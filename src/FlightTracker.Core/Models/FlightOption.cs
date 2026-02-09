namespace FlightTracker.Core.Models;

/// <summary>
/// Represents a single flight option returned from a flight provider.
/// </summary>
public record FlightOption
{
    /// <summary>
    /// Flight price.
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Currency code (e.g., "EUR", "USD").
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Departure time.
    /// </summary>
    public required DateTime DepartureTime { get; init; }

    /// <summary>
    /// Arrival time.
    /// </summary>
    public required DateTime ArrivalTime { get; init; }

    /// <summary>
    /// Airline name.
    /// </summary>
    public required string Airline { get; init; }

    /// <summary>
    /// Number of stops (0 = direct).
    /// </summary>
    public required int Stops { get; init; }

    /// <summary>
    /// URL to book this flight.
    /// </summary>
    public string? BookingUrl { get; init; }
}
