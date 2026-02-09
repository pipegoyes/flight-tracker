using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Data.Repositories;

/// <summary>
/// Repository implementation for Destination entities.
/// </summary>
public class DestinationRepository : Repository<Destination>, IDestinationRepository
{
    public DestinationRepository(FlightTrackerDbContext context) : base(context)
    {
    }

    public async Task<Destination?> GetByAirportCodeAsync(
        string airportCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(d => d.AirportCode == airportCode, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string airportCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(d => d.AirportCode == airportCode, cancellationToken);
    }
}
