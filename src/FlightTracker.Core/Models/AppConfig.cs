namespace FlightTracker.Core.Models;

/// <summary>
/// Application configuration model.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Origin airport code (e.g., "FRA").
    /// </summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>
    /// List of destination configurations.
    /// </summary>
    public List<DestinationConfig> Destinations { get; set; } = new();

    /// <summary>
    /// List of target date ranges to track.
    /// </summary>
    public List<TargetDateConfig> TargetDates { get; set; } = new();
}

/// <summary>
/// Configuration for a destination airport.
/// </summary>
public class DestinationConfig
{
    /// <summary>
    /// IATA airport code (e.g., "PMI").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name (e.g., "Palma de Mallorca").
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for a target date range.
/// </summary>
public class TargetDateConfig
{
    /// <summary>
    /// Outbound flight date (ISO 8601 format: "2026-04-18").
    /// </summary>
    public string Outbound { get; set; } = string.Empty;

    /// <summary>
    /// Return flight date (ISO 8601 format: "2026-04-21").
    /// </summary>
    public string Return { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name (e.g., "Easter Weekend").
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
