namespace FlightTracker.Core.Entities;

/// <summary>
/// Represents a single price check result for a specific route and date.
/// </summary>
public class PriceCheck
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
    /// Foreign key to Destination.
    /// </summary>
    public int DestinationId { get; set; }

    /// <summary>
    /// Timestamp when this price check was performed.
    /// </summary>
    public DateTime CheckTimestamp { get; set; }

    /// <summary>
    /// Flight price in the specified currency.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code (e.g., "EUR", "USD").
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Departure time (outbound flight).
    /// </summary>
    public TimeOnly DepartureTime { get; set; }

    /// <summary>
    /// Arrival time (outbound flight).
    /// </summary>
    public TimeOnly ArrivalTime { get; set; }

    /// <summary>
    /// Airline operating the flight.
    /// </summary>
    public string Airline { get; set; } = string.Empty;

    /// <summary>
    /// Number of stops (0 = direct flight).
    /// </summary>
    public int Stops { get; set; }

    /// <summary>
    /// URL to book this flight.
    /// </summary>
    public string? BookingUrl { get; set; }

    /// <summary>
    /// Navigation property to TargetDate.
    /// </summary>
    public TargetDate TargetDate { get; set; } = null!;

    /// <summary>
    /// Navigation property to Destination.
    /// </summary>
    public Destination Destination { get; set; } = null!;
}
