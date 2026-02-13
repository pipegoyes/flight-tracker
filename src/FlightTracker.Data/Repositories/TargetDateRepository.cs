using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Data.Repositories;

/// <summary>
/// Repository implementation for TargetDate entities.
/// </summary>
public class TargetDateRepository : Repository<TargetDate>, ITargetDateRepository
{
    public TargetDateRepository(FlightTrackerDbContext context) : base(context)
    {
    }

    // Override GetAllAsync to exclude soft-deleted by default
    public override async Task<IEnumerable<TargetDate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.OutboundDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<TargetDate?> GetByDatesAsync(
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => !t.IsDeleted)
            .FirstOrDefaultAsync(
                t => t.OutboundDate.Date == outboundDate.Date &&
                     t.ReturnDate.Date == returnDate.Date,
                cancellationToken);
    }

    public async Task<IEnumerable<TargetDate>> GetUpcomingAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await _dbSet
            .Where(t => !t.IsDeleted && t.OutboundDate >= today)
            .OrderBy(t => t.OutboundDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TargetDate>> GetAllIncludingDeletedAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(t => t.OutboundDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TargetDate>> GetDeletedAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.IsDeleted)
            .OrderByDescending(t => t.DeletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null || entity.IsDeleted)
            return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        
        if (entity == null || !entity.IsDeleted)
            return false;

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TargetDate> CreateTargetDateAsync(TargetDate targetDate, CancellationToken cancellationToken = default)
    {
        targetDate.CreatedAt = DateTime.UtcNow;
        targetDate.IsDeleted = false;
        targetDate.DeletedAt = null;
        targetDate.UpdatedAt = null;

        await _dbSet.AddAsync(targetDate, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return targetDate;
    }

    public async Task<bool> UpdateTargetDateAsync(TargetDate targetDate, CancellationToken cancellationToken = default)
    {
        var existing = await _dbSet.FindAsync(new object[] { targetDate.Id }, cancellationToken);
        if (existing == null || existing.IsDeleted)
            return false;

        existing.Name = targetDate.Name;
        existing.OutboundDate = targetDate.OutboundDate;
        existing.ReturnDate = targetDate.ReturnDate;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<Destination>> GetDestinationsAsync(int targetDateId, CancellationToken cancellationToken = default)
    {
        return await _context.TargetDateDestinations
            .Where(tdd => tdd.TargetDateId == targetDateId)
            .Include(tdd => tdd.Destination)
            .Select(tdd => tdd.Destination)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateDestinationsAsync(int targetDateId, IEnumerable<int> destinationIds, CancellationToken cancellationToken = default)
    {
        var newDestinationIds = destinationIds.ToHashSet();
        
        // Get existing associations
        var existing = await _context.TargetDateDestinations
            .Where(tdd => tdd.TargetDateId == targetDateId)
            .ToListAsync(cancellationToken);

        var existingDestinationIds = existing.Select(e => e.DestinationId).ToHashSet();

        // Find destinations that are being removed
        var removedDestinationIds = existingDestinationIds.Except(newDestinationIds).ToList();

        // Delete price checks for removed destinations (invalidate old prices)
        if (removedDestinationIds.Any())
        {
            var orphanedPrices = await _context.PriceChecks
                .Where(p => p.TargetDateId == targetDateId && removedDestinationIds.Contains(p.DestinationId))
                .ToListAsync(cancellationToken);

            if (orphanedPrices.Any())
            {
                _context.PriceChecks.RemoveRange(orphanedPrices);
            }
        }

        // Remove existing associations
        _context.TargetDateDestinations.RemoveRange(existing);

        // Add new associations
        var newAssociations = newDestinationIds.Select(destId => new TargetDateDestination
        {
            TargetDateId = targetDateId,
            DestinationId = destId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.TargetDateDestinations.AddRangeAsync(newAssociations, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TargetDate?> GetByIdWithDestinationsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.TargetDateDestinations)
                .ThenInclude(tdd => tdd.Destination)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
