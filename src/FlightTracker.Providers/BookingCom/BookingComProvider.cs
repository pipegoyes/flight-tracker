using System.Net.Http.Headers;
using System.Text.Json;
using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Providers.BookingCom;

/// <summary>
/// Booking.com API provider via RapidAPI.
/// </summary>
public class BookingComProvider : IFlightProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiHost;
    private readonly ILogger<BookingComProvider> _logger;

    public BookingComProvider(
        HttpClient httpClient,
        string apiKey,
        string apiHost,
        ILogger<BookingComProvider> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _apiHost = apiHost;
        _logger = logger;

        // Configure HttpClient
        _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", _apiHost);
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
                "Searching flights via Booking.com API: {Origin} -> {Destination}",
                originAirportCode,
                destinationAirportCode);

            // Format dates as required by Booking.com API (YYYY-MM-DD)
            var outboundDateStr = outboundDate.ToString("yyyy-MM-dd");
            var returnDateStr = returnDate.ToString("yyyy-MM-dd");

            // Build API URL
            // Note: Adjust endpoint based on actual Booking.com API documentation
            var url = $"https://{_apiHost}/v1/flights/search?" +
                     $"fromId={originAirportCode}" +
                     $"&toId={destinationAirportCode}" +
                     $"&departDate={outboundDateStr}" +
                     $"&returnDate={returnDateStr}" +
                     $"&adults=1" +
                     $"&cabinClass=ECONOMY" +
                     $"&currency=EUR";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Booking.com API request failed: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);

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
            var searchResponse = JsonSerializer.Deserialize<BookingComSearchResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (searchResponse?.Data?.Flights == null || !searchResponse.Data.Flights.Any())
            {
                _logger.LogWarning("No flights found in Booking.com API response");
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

            // Parse flights
            var flights = searchResponse.Data.Flights
                .Select(f => ParseFlight(f))
                .Where(f => f != null)
                .OrderBy(f => f!.Price)
                .ToList();

            _logger.LogInformation(
                "Found {Count} flights via Booking.com API",
                flights.Count);

            return new FlightSearchResult
            {
                Success = true,
                Flights = flights!,
                Origin = originAirportCode,
                Destination = destinationAirportCode,
                OutboundDate = outboundDate,
                ReturnDate = returnDate
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Booking.com API");
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
            _logger.LogError(ex, "Unexpected error calling Booking.com API");
            return new FlightSearchResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Origin = originAirportCode,
                Destination = destinationAirportCode
            };
        }
    }

    private FlightOption? ParseFlight(BookingComFlight flight)
    {
        try
        {
            // Extract outbound leg details
            var outboundLeg = flight.Legs?.FirstOrDefault();
            if (outboundLeg == null)
                return null;

            return new FlightOption
            {
                Price = flight.Price?.Total ?? 0m,
                Currency = flight.Price?.Currency ?? "EUR",
                DepartureTime = DateTime.Parse(outboundLeg.DepartureTime ?? DateTime.Now.ToString()),
                ArrivalTime = DateTime.Parse(outboundLeg.ArrivalTime ?? DateTime.Now.ToString()),
                Airline = outboundLeg.Carriers?.FirstOrDefault() ?? "Unknown",
                Stops = (outboundLeg.Stops ?? 0),
                BookingUrl = flight.DeepLink ?? $"https://www.booking.com/flights/"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse flight from Booking.com response");
            return null;
        }
    }

    #region Response Models

    private class BookingComSearchResponse
    {
        public BookingComData? Data { get; set; }
    }

    private class BookingComData
    {
        public List<BookingComFlight>? Flights { get; set; }
    }

    private class BookingComFlight
    {
        public BookingComPrice? Price { get; set; }
        public List<BookingComLeg>? Legs { get; set; }
        public string? DeepLink { get; set; }
    }

    private class BookingComPrice
    {
        public decimal Total { get; set; }
        public string? Currency { get; set; }
    }

    private class BookingComLeg
    {
        public string? DepartureTime { get; set; }
        public string? ArrivalTime { get; set; }
        public List<string>? Carriers { get; set; }
        public int? Stops { get; set; }
    }

    #endregion
}
