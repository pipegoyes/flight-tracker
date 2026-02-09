using FlightTracker.Core.Models;

namespace FlightTracker.Core.Interfaces;

/// <summary>
/// Interface for flight search providers (Skyscanner, Kiwi, etc.).
/// Allows swapping implementations without changing business logic.
/// </summary>
public interface IFlightProvider
{
    /// <summary>
    /// Search for flights between two airports on specific dates.
    /// </summary>
    /// <param name="originAirportCode">Origin airport IATA code (e.g., "FRA").</param>
    /// <param name="destinationAirportCode">Destination airport IATA code (e.g., "PMI").</param>
    /// <param name="outboundDate">Outbound flight date.</param>
    /// <param name="returnDate">Return flight date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Flight search result with available options.</returns>
    Task<FlightSearchResult> SearchFlightsAsync(
        string originAirportCode,
        string destinationAirportCode,
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default);
}
