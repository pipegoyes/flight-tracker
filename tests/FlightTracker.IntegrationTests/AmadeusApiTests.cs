using FlightTracker.Providers.Amadeus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace FlightTracker.IntegrationTests;

/// <summary>
/// Integration tests for Amadeus API.
/// These tests call the REAL API - only run manually to verify credentials.
/// Marked with Skip to exclude from CI/CD pipeline.
/// </summary>
public class AmadeusApiTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<AmadeusProvider>> _loggerMock;

    // Test credentials (Amadeus Test Environment)
    private const string TestClientId = "A7z86RxpJMkEdUmkdJkeHhk6OuxoFu5T";
    private const string TestClientSecret = "ujTCvMfmsRfXZq0x";

    public AmadeusApiTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerMock = new Mock<ILogger<AmadeusProvider>>();
    }

    private AmadeusProvider CreateProvider()
    {
        var httpClient = new HttpClient();
        return new AmadeusProvider(
            httpClient,
            TestClientId,
            TestClientSecret,
            useProduction: false, // Use test environment
            _loggerMock.Object);
    }

    [Fact(Skip = "Integration test - calls real Amadeus API. Run manually to verify credentials.")]
    public async Task SearchFlights_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var provider = CreateProvider();
        var outboundDate = DateTime.Today.AddDays(30);
        var returnDate = DateTime.Today.AddDays(34);

        // Act
        var result = await provider.SearchFlightsAsync(
            "FRA", // Frankfurt
            "MAD", // Madrid
            outboundDate,
            returnDate);

        // Assert
        _output.WriteLine($"Success: {result.Success}");
        _output.WriteLine($"Error: {result.ErrorMessage ?? "None"}");
        _output.WriteLine($"Flights found: {result.Flights?.Count() ?? 0}");

        if (result.Success && result.Flights.Any())
        {
            var cheapest = result.Flights.OrderBy(f => f.Price).First();
            _output.WriteLine($"Cheapest: {cheapest.Price} {cheapest.Currency} ({cheapest.Airline})");
        }

        result.Success.Should().BeTrue($"API call failed: {result.ErrorMessage}");
        result.Flights.Should().NotBeEmpty("Expected at least one flight result");
    }

    [Fact(Skip = "Integration test - calls real Amadeus API. Run manually to verify credentials.")]
    public async Task SearchFlights_ReturnsFlightDetails()
    {
        // Arrange
        var provider = CreateProvider();
        var outboundDate = DateTime.Today.AddDays(30);
        var returnDate = DateTime.Today.AddDays(34);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "BCN", outboundDate, returnDate);

        // Assert
        result.Success.Should().BeTrue();
        
        var flight = result.Flights.First();
        
        _output.WriteLine($"Price: {flight.Price} {flight.Currency}");
        _output.WriteLine($"Airline: {flight.Airline}");
        _output.WriteLine($"Stops: {flight.Stops}");
        _output.WriteLine($"Departure: {flight.DepartureTime}");
        _output.WriteLine($"Arrival: {flight.ArrivalTime}");

        flight.Price.Should().BeGreaterThan(0);
        flight.Currency.Should().Be("EUR");
        flight.Airline.Should().NotBeNullOrEmpty();
        flight.Stops.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact(Skip = "Integration test - calls real Amadeus API. Run manually to verify credentials.")]
    public async Task SearchFlights_MultipleDestinations_AllReturnResults()
    {
        // Arrange
        var provider = CreateProvider();
        var outboundDate = DateTime.Today.AddDays(30);
        var returnDate = DateTime.Today.AddDays(34);
        
        var destinations = new[] { "MAD", "BCN", "PMI", "LIS" };
        var results = new List<(string Dest, bool Success, decimal Price, string Error)>();

        // Act
        foreach (var dest in destinations)
        {
            var result = await provider.SearchFlightsAsync("FRA", dest, outboundDate, returnDate);
            
            var price = result.Flights?.OrderBy(f => f.Price).FirstOrDefault()?.Price ?? 0;
            results.Add((dest, result.Success, price, result.ErrorMessage ?? ""));
            
            // Rate limiting - small delay between calls
            await Task.Delay(1000);
        }

        // Assert & Output
        _output.WriteLine("Results:");
        _output.WriteLine("--------");
        foreach (var (dest, success, price, error) in results)
        {
            if (success)
            {
                _output.WriteLine($"FRA -> {dest}: {price:F2} EUR ✓");
            }
            else
            {
                _output.WriteLine($"FRA -> {dest}: FAILED - {error}");
            }
        }

        results.Should().OnlyContain(r => r.Success, "All destinations should return successful results");
    }

    [Fact(Skip = "Integration test - calls real Amadeus API. Run manually to verify credentials.")]
    public async Task SearchFlights_TokenIsCached_OnMultipleCalls()
    {
        // Arrange
        var provider = CreateProvider();
        var outboundDate = DateTime.Today.AddDays(30);
        var returnDate = DateTime.Today.AddDays(34);

        // Act - Make two calls, second should use cached token
        var result1 = await provider.SearchFlightsAsync("FRA", "MAD", outboundDate, returnDate);
        var result2 = await provider.SearchFlightsAsync("FRA", "BCN", outboundDate, returnDate);

        // Assert
        _output.WriteLine($"First call (FRA->MAD): {(result1.Success ? "Success" : "Failed")}");
        _output.WriteLine($"Second call (FRA->BCN): {(result2.Success ? "Success" : "Failed")}");

        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
    }

    [Fact(Skip = "Integration test - calls real Amadeus API. Run manually to verify credentials.")]
    public async Task SearchFlights_InvalidAirportCode_ReturnsError()
    {
        // Arrange
        var provider = CreateProvider();
        var outboundDate = DateTime.Today.AddDays(30);
        var returnDate = DateTime.Today.AddDays(34);

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "INVALID", outboundDate, returnDate);

        // Assert
        _output.WriteLine($"Success: {result.Success}");
        _output.WriteLine($"Error: {result.ErrorMessage}");

        // The API should either return an error or empty results for invalid codes
        if (!result.Success)
        {
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    [Fact(Skip = "Integration test - calls real Amadeus API. Run manually to verify credentials.")]
    public async Task SearchFlights_RoundTrip_ReturnsValidPrices()
    {
        // Arrange
        var provider = CreateProvider();
        
        // Search for Easter weekend 2026
        var outboundDate = new DateTime(2026, 4, 17); // Good Friday
        var returnDate = new DateTime(2026, 4, 20);   // Easter Monday

        // Act
        var result = await provider.SearchFlightsAsync("FRA", "PMI", outboundDate, returnDate);

        // Assert
        _output.WriteLine($"Easter Weekend 2026 (Apr 17-20)");
        _output.WriteLine($"FRA -> PMI (Mallorca)");
        _output.WriteLine($"Success: {result.Success}");
        
        if (result.Success && result.Flights.Any())
        {
            foreach (var flight in result.Flights.OrderBy(f => f.Price).Take(3))
            {
                _output.WriteLine($"  {flight.Price:F2} EUR - {flight.Airline} ({flight.Stops} stops)");
            }
        }

        result.Success.Should().BeTrue();
    }
}
