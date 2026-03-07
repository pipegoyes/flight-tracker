using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Providers.Amadeus;

/// <summary>
/// Amadeus Self-Service API provider for flight search.
/// Uses OAuth2 client credentials flow for authentication.
/// Free tier: ~2000 requests/month in test environment.
/// </summary>
public class AmadeusProvider : IFlightProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly bool _useProduction;
    private readonly ILogger<AmadeusProvider> _logger;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    // API endpoints
    private string BaseUrl => _useProduction 
        ? "https://api.amadeus.com" 
        : "https://test.api.amadeus.com";

    public AmadeusProvider(
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        bool useProduction,
        ILogger<AmadeusProvider> logger)
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _useProduction = useProduction;
        _logger = logger;
    }

    public async Task<FlightSearchResult> SearchFlightsAsync(
        string originAirportCode,
        string destinationAirportCode,
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Searching flights via Amadeus API: {Origin} -> {Destination}",
                originAirportCode,
                destinationAirportCode);

            // Ensure we have a valid access token
            var token = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                return new FlightSearchResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain Amadeus access token",
                    Origin = originAirportCode,
                    Destination = destinationAirportCode,
                    OutboundDate = outboundDate,
                    ReturnDate = returnDate
                };
            }

            // Build flight search URL
            var outboundDateStr = outboundDate.ToString("yyyy-MM-dd");
            var returnDateStr = returnDate.ToString("yyyy-MM-dd");

            var url = $"{BaseUrl}/v2/shopping/flight-offers?" +
                     $"originLocationCode={originAirportCode}" +
                     $"&destinationLocationCode={destinationAirportCode}" +
                     $"&departureDate={outboundDateStr}" +
                     $"&returnDate={returnDateStr}" +
                     $"&adults=1" +
                     $"&currencyCode=EUR" +
                     $"&max=5"; // Limit results to save quota

            _logger.LogDebug("Calling Amadeus API: {Url}", url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Amadeus API request failed: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);

                // Check for quota exceeded
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    return new FlightSearchResult
                    {
                        Success = false,
                        ErrorMessage = "Amadeus API quota exceeded. Try again later.",
                        Origin = originAirportCode,
                        Destination = destinationAirportCode,
                        OutboundDate = outboundDate,
                        ReturnDate = returnDate
                    };
                }

                return new FlightSearchResult
                {
                    Success = false,
                    ErrorMessage = $"API returned {response.StatusCode}: {errorContent}",
                    Origin = originAirportCode,
                    Destination = destinationAirportCode,
                    OutboundDate = outboundDate,
                    ReturnDate = returnDate
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Amadeus API response length: {Length} bytes", content.Length);

            var apiResponse = JsonSerializer.Deserialize<AmadeusFlightOffersResponse>(content);

            if (apiResponse?.Data == null || !apiResponse.Data.Any())
            {
                _logger.LogWarning("No flights found in Amadeus API response");
                return new FlightSearchResult
                {
                    Success = true,
                    Flights = Enumerable.Empty<FlightOption>(),
                    Origin = originAirportCode,
                    Destination = destinationAirportCode,
                    OutboundDate = outboundDate,
                    ReturnDate = returnDate
                };
            }

            // Convert response to FlightOptions
            var flights = apiResponse.Data.Select(offer => ConvertToFlightOption(offer, apiResponse.Dictionaries)).ToList();

            _logger.LogInformation(
                "Found {Count} flight(s) via Amadeus API. Cheapest: {Price} EUR",
                flights.Count,
                flights.Min(f => f.Price));

            return new FlightSearchResult
            {
                Success = true,
                Flights = flights,
                Origin = originAirportCode,
                Destination = destinationAirportCode,
                OutboundDate = outboundDate,
                ReturnDate = returnDate
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Amadeus API");
            return new FlightSearchResult
            {
                Success = false,
                ErrorMessage = $"HTTP error: {ex.Message}",
                Origin = originAirportCode,
                Destination = destinationAirportCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Amadeus API");
            return new FlightSearchResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Origin = originAirportCode,
                Destination = destinationAirportCode
            };
        }
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Return cached token if still valid (with 1 minute buffer)
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow.AddMinutes(1) < _tokenExpiry)
            {
                return _accessToken;
            }

            _logger.LogInformation("Obtaining new Amadeus access token");

            var tokenUrl = $"{BaseUrl}/v1/security/oauth2/token";

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            });

            var response = await _httpClient.PostAsync(tokenUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to obtain Amadeus access token: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<AmadeusTokenResponse>(responseContent);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogError("Invalid token response from Amadeus");
                return null;
            }

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            _logger.LogInformation("Amadeus access token obtained, expires in {Seconds}s", tokenResponse.ExpiresIn);

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private FlightOption ConvertToFlightOption(FlightOffer offer, Dictionaries? dictionaries)
    {
        var price = decimal.TryParse(offer.Price?.Total, out var p) ? p : 0m;
        var currency = offer.Price?.Currency ?? "EUR";

        // Get first outbound segment for departure/arrival times
        var firstItinerary = offer.Itineraries?.FirstOrDefault();
        var firstSegment = firstItinerary?.Segments?.FirstOrDefault();
        var lastSegment = firstItinerary?.Segments?.LastOrDefault();

        var departureTime = DateTime.TryParse(firstSegment?.Departure?.At, out var dep) 
            ? dep 
            : DateTime.Today;
        var arrivalTime = DateTime.TryParse(lastSegment?.Arrival?.At, out var arr) 
            ? arr 
            : DateTime.Today;

        // Get airline name from dictionaries
        var carrierCode = firstSegment?.CarrierCode ?? "Unknown";
        var airlineName = carrierCode;
        if (dictionaries?.Carriers != null && dictionaries.Carriers.TryGetValue(carrierCode, out var name))
        {
            airlineName = name;
        }

        // Calculate total stops (segments - 1)
        var stops = (firstItinerary?.Segments?.Count ?? 1) - 1;

        return new FlightOption
        {
            Price = price,
            Currency = currency,
            DepartureTime = departureTime,
            ArrivalTime = arrivalTime,
            Airline = airlineName,
            Stops = stops,
            BookingUrl = $"https://www.google.com/travel/flights?q=flights%20from%20{firstSegment?.Departure?.IataCode}%20to%20{lastSegment?.Arrival?.IataCode}"
        };
    }

    #region Response Models

    private class AmadeusTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class AmadeusFlightOffersResponse
    {
        [JsonPropertyName("data")]
        public List<FlightOffer>? Data { get; set; }

        [JsonPropertyName("dictionaries")]
        public Dictionaries? Dictionaries { get; set; }
    }

    private class FlightOffer
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("price")]
        public Price? Price { get; set; }

        [JsonPropertyName("itineraries")]
        public List<Itinerary>? Itineraries { get; set; }
    }

    private class Price
    {
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("total")]
        public string? Total { get; set; }

        [JsonPropertyName("grandTotal")]
        public string? GrandTotal { get; set; }
    }

    private class Itinerary
    {
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        [JsonPropertyName("segments")]
        public List<Segment>? Segments { get; set; }
    }

    private class Segment
    {
        [JsonPropertyName("departure")]
        public FlightEndpoint? Departure { get; set; }

        [JsonPropertyName("arrival")]
        public FlightEndpoint? Arrival { get; set; }

        [JsonPropertyName("carrierCode")]
        public string? CarrierCode { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }
    }

    private class FlightEndpoint
    {
        [JsonPropertyName("iataCode")]
        public string? IataCode { get; set; }

        [JsonPropertyName("at")]
        public string? At { get; set; }
    }

    private class Dictionaries
    {
        [JsonPropertyName("carriers")]
        public Dictionary<string, string>? Carriers { get; set; }
    }

    #endregion
}
