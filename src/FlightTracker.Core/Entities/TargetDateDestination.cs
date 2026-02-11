namespace FlightTracker.Core.Entities;

/// <summary>
/// Junction entity for many-to-many relationship between TargetDate and Destination.
/// </summary>
public class TargetDateDestination
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to TargetDate.
    /// </summary>
    public int TargetDateId { get; set; }

    /// <summary>
    /// Navigation property to TargetDate.
    /// </summary>
    public TargetDate TargetDate { get; set; } = null!;

    /// <summary>
    /// Foreign key to Destination.
    /// </summary>
    public int DestinationId { get; set; }

    /// <summary>
    /// Navigation property to Destination.
    /// </summary>
    public Destination Destination { get; set; } = null!;

    /// <summary>
    /// When this association was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
