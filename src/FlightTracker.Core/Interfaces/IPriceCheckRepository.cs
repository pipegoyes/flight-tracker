using FlightTracker.Core.Entities;

namespace FlightTracker.Core.Interfaces;

/// <summary>
/// Repository interface for PriceCheck entities.
/// </summary>
public interface IPriceCheckRepository : IRepository<PriceCheck>
{
    /// <summary>
    /// Get the latest price check for a specific target date and destination.
    /// </summary>
    Task<PriceCheck?> GetLatestAsync(
        int targetDateId,
        int destinationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all latest price checks for a specific target date (one per destination).
    /// </summary>
    Task<IEnumerable<PriceCheck>> GetLatestForTargetDateAsync(
        int targetDateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get price history for a specific route over a time period.
    /// </summary>
    Task<IEnumerable<PriceCheck>> GetHistoryAsync(
        int targetDateId,
        int destinationId,
        DateTime since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all price checks within a date range.
    /// </summary>
    Task<IEnumerable<PriceCheck>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete old price checks (cleanup for records older than specified date).
    /// </summary>
    Task<int> DeleteOlderThanAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if we have a recent price check for a specific target date and destination.
    /// "Recent" is defined by the maxAgeHours parameter.
    /// </summary>
    Task<PriceCheck?> GetRecentPriceAsync(
        int targetDateId,
        int destinationId,
        int maxAgeHours,
        CancellationToken cancellationToken = default);
}
