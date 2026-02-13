namespace FlightTracker.Core.Models;

/// <summary>
/// Application version information.
/// </summary>
public class AppVersion
{
    /// <summary>
    /// Full git commit hash.
    /// </summary>
    public string Commit { get; set; } = "unknown";

    /// <summary>
    /// Short git commit hash (first 7 characters).
    /// </summary>
    public string ShortCommit => Commit.Length >= 7 ? Commit[..7] : Commit;

    /// <summary>
    /// Build timestamp (UTC).
    /// </summary>
    public string BuildTime { get; set; } = "unknown";

    /// <summary>
    /// Environment name (Development, Production, etc.)
    /// </summary>
    public string Environment { get; set; } = "unknown";

    /// <summary>
    /// Application name.
    /// </summary>
    public string AppName { get; set; } = "FlightTracker";
}
