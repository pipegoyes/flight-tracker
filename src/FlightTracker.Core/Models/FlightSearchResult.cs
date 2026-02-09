namespace FlightTracker.Core.Models;

/// <summary>
/// Result of a flight search operation from a provider.
/// </summary>
public record FlightSearchResult
{
    /// <summary>
    /// Indicates whether the search was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if search failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// List of flight options found (empty if unsuccessful).
    /// </summary>
    public IEnumerable<FlightOption> Flights { get; init; } = Enumerable.Empty<FlightOption>();

    /// <summary>
    /// Origin airport code.
    /// </summary>
    public string? Origin { get; init; }

    /// <summary>
    /// Destination airport code.
    /// </summary>
    public string? Destination { get; init; }

    /// <summary>
    /// Outbound date searched.
    /// </summary>
    public DateTime? OutboundDate { get; init; }

    /// <summary>
    /// Return date searched.
    /// </summary>
    public DateTime? ReturnDate { get; init; }
}
