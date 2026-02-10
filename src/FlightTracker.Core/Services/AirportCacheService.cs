using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Core.Services;

/// <summary>
/// Caching service for airport/destination data to avoid repeated database queries.
/// </summary>
public class AirportCacheService
{
    private readonly IDestinationRepository _destinationRepository;
    private readonly ILogger<AirportCacheService> _logger;
    
    private List<Destination>? _cachedDestinations;
    private DateTime? _cacheTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

    public AirportCacheService(
        IDestinationRepository destinationRepository,
        ILogger<AirportCacheService> logger)
    {
        _destinationRepository = destinationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all destinations from cache or database.
    /// </summary>
    public async Task<List<Destination>> GetAllDestinationsAsync(CancellationToken cancellationToken = default)
    {
        // Check if cache is valid
        if (_cachedDestinations != null && 
            _cacheTime.HasValue && 
            DateTime.UtcNow - _cacheTime.Value < _cacheExpiration)
        {
            _logger.LogDebug("Returning {Count} destinations from cache", _cachedDestinations.Count);
            return _cachedDestinations;
        }

        // Reload from database
        _logger.LogInformation("Loading destinations from database");
        var destinations = await _destinationRepository.GetAllAsync(cancellationToken);
        _cachedDestinations = destinations.OrderBy(d => d.Name).ToList();
        _cacheTime = DateTime.UtcNow;

        _logger.LogInformation("Cached {Count} destinations", _cachedDestinations.Count);
        return _cachedDestinations;
    }

    /// <summary>
    /// Search destinations by IATA code or name.
    /// </summary>
    public async Task<List<Destination>> SearchDestinationsAsync(
        string query, 
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<Destination>();
        }

        var allDestinations = await GetAllDestinationsAsync(cancellationToken);

        var results = allDestinations
            .Where(d => 
                d.AirportCode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();

        _logger.LogDebug(
            "Search query '{Query}' returned {Count} results", 
            query, 
            results.Count);

        return results;
    }

    /// <summary>
    /// Invalidate the cache (force reload on next request).
    /// </summary>
    public void InvalidateCache()
    {
        _logger.LogInformation("Invalidating destination cache");
        _cachedDestinations = null;
        _cacheTime = null;
    }
}
