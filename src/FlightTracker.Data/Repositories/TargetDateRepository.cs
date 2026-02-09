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

    public async Task<TargetDate?> GetByDatesAsync(
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
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
            .Where(t => t.OutboundDate >= today)
            .OrderBy(t => t.OutboundDate)
            .ToListAsync(cancellationToken);
    }
}
