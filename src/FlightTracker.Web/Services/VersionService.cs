using FlightTracker.Core.Models;

namespace FlightTracker.Web.Services;

/// <summary>
/// Service for retrieving application version information.
/// </summary>
public class VersionService
{
    private readonly AppVersion _version;

    public VersionService(IConfiguration configuration, IHostEnvironment environment)
    {
        _version = new AppVersion
        {
            Commit = configuration["APP_VERSION"] 
                ?? configuration["AppVersion"] 
                ?? Environment.GetEnvironmentVariable("APP_VERSION") 
                ?? "dev",
            BuildTime = configuration["APP_BUILD_TIME"] 
                ?? configuration["AppBuildTime"] 
                ?? Environment.GetEnvironmentVariable("APP_BUILD_TIME") 
                ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            Environment = environment.EnvironmentName,
            AppName = "FlightTracker"
        };
    }

    /// <summary>
    /// Get the current application version.
    /// </summary>
    public AppVersion GetVersion() => _version;

    /// <summary>
    /// Get the short commit hash for display.
    /// </summary>
    public string GetShortCommit() => _version.ShortCommit;
}
