namespace FlightTracker.Core.Entities;

/// <summary>
/// Represents a target travel date range to track.
/// </summary>
public class TargetDate
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Outbound flight date.
    /// </summary>
    public DateTime OutboundDate { get; set; }

    /// <summary>
    /// Return flight date.
    /// </summary>
    public DateTime ReturnDate { get; set; }

    /// <summary>
    /// Human-readable name for this date range (e.g., "Easter Weekend").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Soft delete flag - if true, this date is hidden from active tracking.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp when this date was soft-deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Timestamp when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to price checks for this date range.
    /// </summary>
    public ICollection<PriceCheck> PriceChecks { get; set; } = new List<PriceCheck>();

    /// <summary>
    /// Navigation property to destination associations.
    /// </summary>
    public ICollection<TargetDateDestination> TargetDateDestinations { get; set; } = new List<TargetDateDestination>();
}
