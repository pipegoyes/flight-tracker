namespace FlightTracker.Core.Models;

/// <summary>
/// Configuration for flight provider.
/// </summary>
public class FlightProviderConfig
{
    /// <summary>
    /// Provider type: Mock, Skyscanner, BookingCom
    /// </summary>
    public string Type { get; set; } = "Mock";

    /// <summary>
    /// API Key for the provider (RapidAPI, etc.)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API Secret (if required by provider)
    /// </summary>
    public string? ApiSecret { get; set; }

    /// <summary>
    /// API Host (for RapidAPI endpoints)
    /// </summary>
    public string? ApiHost { get; set; }

    /// <summary>
    /// Additional provider-specific settings
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new();
}
