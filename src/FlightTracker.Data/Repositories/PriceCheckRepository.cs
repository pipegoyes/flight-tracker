using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Data.Repositories;

/// <summary>
/// Repository implementation for PriceCheck entities.
/// </summary>
public class PriceCheckRepository : Repository<PriceCheck>, IPriceCheckRepository
{
    public PriceCheckRepository(FlightTrackerDbContext context) : base(context)
    {
    }

    public async Task<PriceCheck?> GetLatestAsync(
        int targetDateId,
        int destinationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.TargetDateId == targetDateId && p.DestinationId == destinationId)
            .OrderByDescending(p => p.CheckTimestamp)
            .Include(p => p.TargetDate)
            .Include(p => p.Destination)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PriceCheck>> GetLatestForTargetDateAsync(
        int targetDateId,
        CancellationToken cancellationToken = default)
    {
        // Get the latest price check for each destination for the given target date
        var latestPriceIds = await _dbSet
            .Where(p => p.TargetDateId == targetDateId)
            .GroupBy(p => p.DestinationId)
            .Select(g => g.OrderByDescending(p => p.CheckTimestamp).First().Id)
            .ToListAsync(cancellationToken);

        var latestPrices = await _dbSet
            .Where(p => latestPriceIds.Contains(p.Id))
            .Include(p => p.TargetDate)
            .Include(p => p.Destination)
            .ToListAsync(cancellationToken);

        return latestPrices;
    }

    public async Task<IEnumerable<PriceCheck>> GetHistoryAsync(
        int targetDateId,
        int destinationId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.TargetDateId == targetDateId &&
                       p.DestinationId == destinationId &&
                       p.CheckTimestamp >= since)
            .OrderBy(p => p.CheckTimestamp)
            .Include(p => p.TargetDate)
            .Include(p => p.Destination)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PriceCheck>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.CheckTimestamp >= startDate && p.CheckTimestamp <= endDate)
            .OrderBy(p => p.CheckTimestamp)
            .Include(p => p.TargetDate)
            .Include(p => p.Destination)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteOlderThanAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default)
    {
        var oldRecords = await _dbSet
            .Where(p => p.CheckTimestamp < cutoffDate)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(oldRecords);
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
