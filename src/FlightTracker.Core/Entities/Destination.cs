namespace FlightTracker.Core.Entities;

/// <summary>
/// Represents a destination airport.
/// </summary>
public class Destination
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// IATA airport code (e.g., "PMI", "ARN", "TFS").
    /// </summary>
    public string AirportCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable destination name (e.g., "Palma de Mallorca").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to price checks for this destination.
    /// </summary>
    public ICollection<PriceCheck> PriceChecks { get; set; } = new List<PriceCheck>();

    /// <summary>
    /// Navigation property to target date associations.
    /// </summary>
    public ICollection<TargetDateDestination> TargetDateDestinations { get; set; } = new List<TargetDateDestination>();
}
