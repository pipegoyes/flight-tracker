using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Core.Services;

/// <summary>
/// Caching service for airport/destination data to avoid repeated database queries.
/// Uses IServiceScopeFactory to resolve scoped dependencies safely from singleton.
/// </summary>
public class AirportCacheService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AirportCacheService> _logger;
    
    private List<Destination>? _cachedDestinations;
    private DateTime? _cacheTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public AirportCacheService(
        IServiceScopeFactory scopeFactory,
        ILogger<AirportCacheService> logger)
    {
        _scopeFactory = scopeFactory;
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

        // Acquire lock to prevent multiple simultaneous loads
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedDestinations != null && 
                _cacheTime.HasValue && 
                DateTime.UtcNow - _cacheTime.Value < _cacheExpiration)
            {
                return _cachedDestinations;
            }

            // Reload from database using scoped repository
            _logger.LogInformation("Loading destinations from database");
            
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDestinationRepository>();
            var destinations = await repository.GetAllAsync(cancellationToken);
            
            _cachedDestinations = destinations.OrderBy(d => d.Name).ToList();
            _cacheTime = DateTime.UtcNow;

            _logger.LogInformation("Cached {Count} destinations", _cachedDestinations.Count);
            return _cachedDestinations;
        }
        finally
        {
            _cacheLock.Release();
        }
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
