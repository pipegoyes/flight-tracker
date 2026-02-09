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
    /// Navigation property to price checks for this date range.
    /// </summary>
    public ICollection<PriceCheck> PriceChecks { get; set; } = new List<PriceCheck>();
}
