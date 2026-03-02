using System.Text.Json;
using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Providers.BookingCom;

/// <summary>
/// Booking.com API provider via RapidAPI using getMinPrice endpoint.
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

        // Configure HttpClient headers
        _httpClient.DefaultRequestHeaders.Clear();
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

            // Build API URL - using getMinPrice endpoint
            // Airport codes need .AIRPORT suffix
            var url = $"https://{_apiHost}/api/v1/flights/getMinPrice?" +
                     $"fromId={originAirportCode}.AIRPORT" +
                     $"&toId={destinationAirportCode}.AIRPORT" +
                     $"&departDate={outboundDateStr}" +
                     $"&returnDate={returnDateStr}" +
                     $"&adults=1" +
                     $"&cabinClass=ECONOMY" +
                     $"&currency_code=EUR";

            _logger.LogDebug("Calling Booking.com API: {Url}", url);

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
            _logger.LogDebug("Booking.com API response: {Content}", content);

            var apiResponse = JsonSerializer.Deserialize<BookingComMinPriceResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse?.Status != true || apiResponse.Data == null || !apiResponse.Data.Any())
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

            // Find the price for the exact requested dates (offsetDays = 0)
            // or get the cheapest one
            var exactMatch = apiResponse.Data.FirstOrDefault(d => d.OffsetDays == 0);
            var cheapest = apiResponse.Data.FirstOrDefault(d => d.IsCheapest == true);
            var priceData = exactMatch ?? cheapest ?? apiResponse.Data.First();

            // Calculate price from units and nanos
            var price = priceData.Price?.Units ?? 0m;
            if (priceData.Price?.Nanos > 0)
            {
                price += priceData.Price.Nanos / 1_000_000_000m;
            }

            var flight = new FlightOption
            {
                Price = price,
                Currency = priceData.Price?.CurrencyCode ?? "EUR",
                DepartureTime = DateTime.Parse(priceData.DepartureDate ?? outboundDate.ToString("yyyy-MM-dd")),
                ArrivalTime = DateTime.Parse(priceData.ReturnDate ?? returnDate.ToString("yyyy-MM-dd")),
                Airline = "Various", // getMinPrice doesn't return airline info
                Stops = -1, // Unknown from this endpoint
                BookingUrl = $"https://www.booking.com/flights/search.html?from={originAirportCode}&to={destinationAirportCode}&depart={outboundDateStr}&return={returnDateStr}"
            };

            _logger.LogInformation(
                "Found flight price via Booking.com API: {Price} {Currency}",
                flight.Price,
                flight.Currency);

            return new FlightSearchResult
            {
                Success = true,
                Flights = new[] { flight },
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

    #region Response Models for getMinPrice

    private class BookingComMinPriceResponse
    {
        public bool Status { get; set; }
        public string? Message { get; set; }
        public List<MinPriceData>? Data { get; set; }
    }

    private class MinPriceData
    {
        public string? DepartureDate { get; set; }
        public string? ReturnDate { get; set; }
        public int OffsetDays { get; set; }
        public bool IsCheapest { get; set; }
        public MinPriceValue? Price { get; set; }
    }

    private class MinPriceValue
    {
        public string? CurrencyCode { get; set; }
        public decimal Units { get; set; }
        public long Nanos { get; set; }
    }

    #endregion
}
