using System.Net;
using System.Text.Json;
using FlightTracker.Providers.Amadeus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FlightTracker.Tests.Providers;

public class AmadeusProviderTests
{
    private readonly Mock<ILogger<AmadeusProvider>> _loggerMock;
    private const string TestClientId = "test-client-id";
    private const string TestClientSecret = "test-client-secret";

    public AmadeusProviderTests()
    {
        _loggerMock = new Mock<ILogger<AmadeusProvider>>();
    }

    private AmadeusProvider CreateProvider(HttpClient httpClient, bool useProduction = false)
    {
        return new AmadeusProvider(
            httpClient,
            TestClientId,
            TestClientSecret,
            useProduction,
            _loggerMock.Object);
    }

    private static HttpClient CreateMockHttpClient(
        HttpStatusCode tokenStatusCode,
        string tokenResponse,
        HttpStatusCode flightStatusCode,
        string flightResponse)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        // Setup token endpoint
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.PathAndQuery.Contains("/v1/security/oauth2/token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = tokenStatusCode,
                Content = new StringContent(tokenResponse)
            });

        // Setup flight search endpoint
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.PathAndQuery.Contains("/v2/shopping/flight-offers")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = flightStatusCode,
                Content = new StringContent(flightResponse)
            });

        return new HttpClient(handlerMock.Object);
    }

    #region Token Tests

    [Fact]
    public async Task SearchFlightsAsync_ObtainsAccessToken_BeforeCallingApi()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("test-token", 1800);
        var flightResponse = CreateFlightResponse(new[] { CreateFlightOffer("150.00", "LH") });

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK, tokenResponse,
            HttpStatusCode.OK, flightResponse);

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "MAD", 
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SearchFlightsAsync_FailsGracefully_WhenTokenRequestFails()
    {
        // Arrange
        var errorResponse = @"{""error"": ""invalid_client"", ""error_description"": ""Invalid credentials""}";

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.Unauthorized, errorResponse,
            HttpStatusCode.OK, "{}");

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "MAD",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("access token");
    }

    #endregion

    #region Flight Search Tests

    [Fact]
    public async Task SearchFlightsAsync_ReturnsSuccessfulResult_WithValidResponse()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("test-token", 1800);
        var flightResponse = CreateFlightResponse(new[]
        {
            CreateFlightOffer("150.00", "LH", "Lufthansa"),
            CreateFlightOffer("180.00", "IB", "Iberia")
        });

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK, tokenResponse,
            HttpStatusCode.OK, flightResponse);

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "MAD",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Origin.Should().Be("FRA");
        result.Destination.Should().Be("MAD");
        result.Flights.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchFlightsAsync_ParsesPrice_Correctly()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("test-token", 1800);
        var flightResponse = CreateFlightResponse(new[]
        {
            CreateFlightOffer("199.99", "LH")
        });

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK, tokenResponse,
            HttpStatusCode.OK, flightResponse);

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "BCN",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeTrue();
        var flight = result.Flights.First();
        flight.Price.Should().Be(199.99m);
        flight.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task SearchFlightsAsync_ParsesAirline_FromDictionaries()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("test-token", 1800);
        var flightResponse = CreateFlightResponse(
            new[] { CreateFlightOffer("150.00", "LH") },
            new Dictionary<string, string> { ["LH"] = "Lufthansa" });

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK, tokenResponse,
            HttpStatusCode.OK, flightResponse);

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "MAD",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeTrue();
        var flight = result.Flights.First();
        flight.Airline.Should().Be("Lufthansa");
    }

    [Fact]
    public async Task SearchFlightsAsync_CalculatesStops_FromSegments()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("test-token", 1800);
        var flightResponse = CreateFlightResponseWithStops("150.00", 2); // 2 segments = 1 stop

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK, tokenResponse,
            HttpStatusCode.OK, flightResponse);

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "JFK",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeTrue();
        var flight = result.Flights.First();
        flight.Stops.Should().Be(1); // 2 segments means 1 stop
    }

    [Fact]
    public async Task SearchFlightsAsync_ReturnsEmptyFlights_WhenNoResults()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("test-token", 1800);
        var emptyResponse = @"{""data"": []}";

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK, tokenResponse,
            HttpStatusCode.OK, emptyResponse);

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "XYZ",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeTrue();
        result.Flights.Should().BeEmpty();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SearchFlightsAsync_HandlesQuotaExceeded_Gracefully()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("test-token", 1800);
        var errorResponse = @"{""errors"": [{""detail"": ""Quota exceeded""}]}";

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK, tokenResponse,
            HttpStatusCode.TooManyRequests, errorResponse);

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "MAD",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("quota");
    }

    [Fact]
    public async Task SearchFlightsAsync_HandlesApiError_Gracefully()
    {
        // Arrange
        var tokenResponse = CreateTokenResponse("test-token", 1800);
        var errorResponse = @"{""errors"": [{""detail"": ""Invalid airport code""}]}";

        var httpClient = CreateMockHttpClient(
            HttpStatusCode.OK, tokenResponse,
            HttpStatusCode.BadRequest, errorResponse);

        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "INVALID",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("BadRequest");
    }

    [Fact]
    public async Task SearchFlightsAsync_HandlesNetworkError_Gracefully()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = CreateProvider(httpClient);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "MAD",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("HTTP error");
    }

    #endregion

    #region Environment Tests

    [Fact]
    public async Task SearchFlightsAsync_UsesTestEnvironment_WhenUseProductionIsFalse()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        string? capturedUrl = null;

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.Host)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(CreateTokenResponse("token", 1800))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = CreateProvider(httpClient, useProduction: false);

        // Act
        await provider.SearchFlightsAsync("FRA", "MAD",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        capturedUrl.Should().Be("test.api.amadeus.com");
    }

    [Fact]
    public async Task SearchFlightsAsync_UsesProductionEnvironment_WhenUseProductionIsTrue()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        string? capturedUrl = null;

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedUrl = req.RequestUri?.Host)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(CreateTokenResponse("token", 1800))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = CreateProvider(httpClient, useProduction: true);

        // Act
        await provider.SearchFlightsAsync("FRA", "MAD",
            DateTime.Today.AddDays(30), DateTime.Today.AddDays(34));

        // Assert
        capturedUrl.Should().Be("api.amadeus.com");
    }

    #endregion

    #region Helper Methods

    private static string CreateTokenResponse(string token, int expiresIn)
    {
        return JsonSerializer.Serialize(new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = expiresIn
        });
    }

    private static string CreateFlightOffer(string price, string carrierCode, string? airlineName = null)
    {
        return JsonSerializer.Serialize(new
        {
            id = "1",
            price = new { currency = "EUR", total = price },
            itineraries = new[]
            {
                new
                {
                    duration = "PT2H30M",
                    segments = new[]
                    {
                        new
                        {
                            departure = new { iataCode = "FRA", at = DateTime.Today.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ss") },
                            arrival = new { iataCode = "MAD", at = DateTime.Today.AddDays(30).AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss") },
                            carrierCode = carrierCode,
                            number = "123"
                        }
                    }
                }
            }
        });
    }

    private static string CreateFlightResponse(
        string[] offers, 
        Dictionary<string, string>? carriers = null)
    {
        var data = offers.Select((o, i) => JsonSerializer.Deserialize<object>(o)).ToList();
        
        var response = new Dictionary<string, object>
        {
            ["data"] = data
        };

        if (carriers != null)
        {
            response["dictionaries"] = new { carriers };
        }

        return JsonSerializer.Serialize(response);
    }

    private static string CreateFlightResponseWithStops(string price, int segmentCount)
    {
        var segments = Enumerable.Range(0, segmentCount).Select(i => new
        {
            departure = new { iataCode = i == 0 ? "FRA" : $"STOP{i}", at = DateTime.Today.AddDays(30).AddHours(i * 3).ToString("yyyy-MM-ddTHH:mm:ss") },
            arrival = new { iataCode = i == segmentCount - 1 ? "JFK" : $"STOP{i + 1}", at = DateTime.Today.AddDays(30).AddHours(i * 3 + 2).ToString("yyyy-MM-ddTHH:mm:ss") },
            carrierCode = "LH",
            number = $"10{i}"
        }).ToArray();

        var response = new
        {
            data = new[]
            {
                new
                {
                    id = "1",
                    price = new { currency = "EUR", total = price },
                    itineraries = new[]
                    {
                        new { duration = "PT8H", segments }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(response);
    }

    #endregion
}
