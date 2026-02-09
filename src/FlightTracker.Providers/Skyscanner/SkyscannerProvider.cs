using FlightTracker.Core.Interfaces;
using FlightTracker.Core.Models;

namespace FlightTracker.Providers.Skyscanner;

/// <summary>
/// Skyscanner API flight provider.
/// TODO: Implement real Skyscanner API integration.
/// </summary>
public class SkyscannerProvider : IFlightProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public SkyscannerProvider(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<FlightSearchResult> SearchFlightsAsync(
        string originAirportCode,
        string destinationAirportCode,
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement Skyscanner API call
        // 1. Create search session
        // 2. Poll for results
        // 3. Parse flight options
        // 4. Return cheapest flight

        await Task.Delay(100, cancellationToken); // Placeholder

        throw new NotImplementedException(
            "Skyscanner provider not yet implemented. Use MockFlightProvider for testing.");
    }
}
