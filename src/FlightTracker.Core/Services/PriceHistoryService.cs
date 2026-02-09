using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Core.Services;

/// <summary>
/// Service for retrieving and analyzing price history.
/// </summary>
public class PriceHistoryService
{
    private readonly IPriceCheckRepository _priceCheckRepository;
    private readonly ILogger<PriceHistoryService> _logger;

    public PriceHistoryService(
        IPriceCheckRepository priceCheckRepository,
        ILogger<PriceHistoryService> logger)
    {
        _priceCheckRepository = priceCheckRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get price history for a specific route over a time period.
    /// </summary>
    public async Task<IEnumerable<PriceCheck>> GetPriceHistoryAsync(
        int targetDateId,
        int destinationId,
        int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-daysBack);
        
        return await _priceCheckRepository.GetHistoryAsync(
            targetDateId,
            destinationId,
            since,
            cancellationToken);
    }

    /// <summary>
    /// Calculate price change percentage from 24 hours ago.
    /// </summary>
    public async Task<decimal?> GetPriceChangeAsync(
        int targetDateId,
        int destinationId,
        CancellationToken cancellationToken = default)
    {
        var history = await GetPriceHistoryAsync(
            targetDateId,
            destinationId,
            daysBack: 7,
            cancellationToken);

        var priceChecks = history.OrderByDescending(p => p.CheckTimestamp).ToList();

        if (!priceChecks.Any())
            return null;

        var latestPrice = priceChecks.First().Price;

        // Find price from ~24 hours ago
        var yesterday = DateTime.UtcNow.AddHours(-24);
        var oldPrice = priceChecks
            .Where(p => p.CheckTimestamp <= yesterday)
            .OrderByDescending(p => p.CheckTimestamp)
            .FirstOrDefault()?.Price;

        if (oldPrice == null || oldPrice == 0)
            return null;

        // Calculate percentage change
        var percentageChange = ((latestPrice - oldPrice.Value) / oldPrice.Value) * 100;
        
        return percentageChange;
    }

    /// <summary>
    /// Get lowest price seen in history for a route.
    /// </summary>
    public async Task<PriceCheck?> GetLowestPriceAsync(
        int targetDateId,
        int destinationId,
        int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        var history = await GetPriceHistoryAsync(
            targetDateId,
            destinationId,
            daysBack,
            cancellationToken);

        return history.OrderBy(p => p.Price).FirstOrDefault();
    }

    /// <summary>
    /// Get average price over time period.
    /// </summary>
    public async Task<decimal?> GetAveragePriceAsync(
        int targetDateId,
        int destinationId,
        int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        var history = await GetPriceHistoryAsync(
            targetDateId,
            destinationId,
            daysBack,
            cancellationToken);

        if (!history.Any())
            return null;

        return history.Average(p => p.Price);
    }

    /// <summary>
    /// Delete old price check records (cleanup).
    /// </summary>
    public async Task<int> CleanupOldRecordsAsync(
        int daysToKeep = 90,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        _logger.LogInformation(
            "Cleaning up price checks older than {Date}",
            cutoffDate.ToShortDateString());

        var deletedCount = await _priceCheckRepository.DeleteOlderThanAsync(
            cutoffDate,
            cancellationToken);

        _logger.LogInformation(
            "Deleted {Count} old price check records",
            deletedCount);

        return deletedCount;
    }
}
