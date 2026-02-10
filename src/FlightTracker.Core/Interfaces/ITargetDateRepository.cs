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

    /// <summary>
    /// Get all target dates including soft-deleted ones.
    /// </summary>
    Task<IEnumerable<TargetDate>> GetAllIncludingDeletedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get only soft-deleted target dates.
    /// </summary>
    Task<IEnumerable<TargetDate>> GetDeletedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete a target date by ID.
    /// </summary>
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore a soft-deleted target date by ID.
    /// </summary>
    Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new target date.
    /// </summary>
    Task<TargetDate> CreateTargetDateAsync(TargetDate targetDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing target date.
    /// </summary>
    Task<bool> UpdateTargetDateAsync(TargetDate targetDate, CancellationToken cancellationToken = default);
}
