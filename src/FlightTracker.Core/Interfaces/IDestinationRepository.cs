using FlightTracker.Core.Entities;

namespace FlightTracker.Core.Interfaces;

/// <summary>
/// Repository interface for Destination entities.
/// </summary>
public interface IDestinationRepository : IRepository<Destination>
{
    /// <summary>
    /// Get destination by airport code.
    /// </summary>
    Task<Destination?> GetByAirportCodeAsync(
        string airportCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a destination with the given airport code exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string airportCode,
        CancellationToken cancellationToken = default);
}
