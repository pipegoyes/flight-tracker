using FlightTracker.Core.Entities;

namespace FlightTracker.Core.Interfaces;

/// <summary>
/// Repository interface for TargetDate entities.
/// </summary>
public interface ITargetDateRepository : IRepository<TargetDate>
{
    /// <summary>
    /// Get target date by outbound and return dates.
    /// </summary>
    Task<TargetDate?> GetByDatesAsync(
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all upcoming target dates (outbound date >= today).
    /// </summary>
    Task<IEnumerable<TargetDate>> GetUpcomingAsync(
        CancellationToken cancellationToken = default);
}
