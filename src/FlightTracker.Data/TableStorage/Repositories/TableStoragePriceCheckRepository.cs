using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using FlightTracker.Data.TableStorage.Entities;

namespace FlightTracker.Data.TableStorage.Repositories;

public class TableStoragePriceCheckRepository : IPriceCheckRepository
{
    private readonly TableClient _tableClient;
    private readonly TableStorageDestinationRepository _destinationRepo;

    public TableStoragePriceCheckRepository(
        TableStorageContext context,
        TableStorageDestinationRepository destinationRepo)
    {
        _tableClient = context.GetPriceChecksTable();
        _destinationRepo = destinationRepo;
    }

    public async Task<PriceCheck?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await foreach (var entity in _tableClient.QueryAsync<PriceCheckEntity>(cancellationToken: cancellationToken))
        {
            if (entity.RowKey.GetHashCode() == id)
            {
                var destId = _destinationRepo.GetIdForCode(entity.DestinationCode);
                return entity.ToDomain(destId);
            }
        }
        return null;
    }

    public async Task<IEnumerable<PriceCheck>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<PriceCheck>();
        await foreach (var entity in _tableClient.QueryAsync<PriceCheckEntity>(cancellationToken: cancellationToken))
        {
            var dest = await _destinationRepo.GetByCodeAsync(entity.DestinationCode, cancellationToken);
            if (dest != null)
            {
                var priceCheck = entity.ToDomain(dest.Id);
                priceCheck.Destination = dest;
                results.Add(priceCheck);
            }
        }
        return results;
    }

    public async Task<IEnumerable<PriceCheck>> FindAsync(Expression<Func<PriceCheck, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(predicate.Compile());
    }

    public async Task<PriceCheck> AddAsync(PriceCheck priceCheck, CancellationToken cancellationToken = default)
    {
        var dest = await _destinationRepo.GetByIdAsync(priceCheck.DestinationId, cancellationToken);
        var destCode = dest?.AirportCode ?? priceCheck.DestinationId.ToString();
        
        var entity = PriceCheckEntity.FromDomain(priceCheck, destCode);
        await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        
        priceCheck.Id = entity.RowKey.GetHashCode();
        return priceCheck;
    }

    public async Task UpdateAsync(PriceCheck priceCheck, CancellationToken cancellationToken = default)
    {
        var dest = await _destinationRepo.GetByIdAsync(priceCheck.DestinationId, cancellationToken);
        var destCode = dest?.AirportCode ?? priceCheck.DestinationId.ToString();
        
        var entity = PriceCheckEntity.FromDomain(priceCheck, destCode);
        await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(PriceCheck priceCheck, CancellationToken cancellationToken = default)
    {
        var dest = await _destinationRepo.GetByIdAsync(priceCheck.DestinationId, cancellationToken);
        var destCode = dest?.AirportCode ?? priceCheck.DestinationId.ToString();
        var timestamp = priceCheck.CheckTimestamp.ToString("yyyyMMddHHmmss");
        var rowKey = $"{timestamp}_{destCode}";
        
        try
        {
            await _tableClient.DeleteEntityAsync(priceCheck.TargetDateId.ToString(), rowKey, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException) { /* Ignore if not found */ }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0); // Table Storage commits immediately
    }

    public async Task<PriceCheck?> GetLatestAsync(int targetDateId, int destinationId, CancellationToken cancellationToken = default)
    {
        var dest = await _destinationRepo.GetByIdAsync(destinationId, cancellationToken);
        if (dest == null) return null;

        var filter = $"PartitionKey eq '{targetDateId}' and DestinationCode eq '{dest.AirportCode}'";
        
        PriceCheckEntity? latest = null;
        await foreach (var entity in _tableClient.QueryAsync<PriceCheckEntity>(filter, cancellationToken: cancellationToken))
        {
            if (latest == null || entity.CheckTimestamp > latest.CheckTimestamp)
                latest = entity;
        }

        if (latest == null) return null;
        
        var priceCheck = latest.ToDomain(destinationId);
        priceCheck.Destination = dest;
        return priceCheck;
    }

    public async Task<IEnumerable<PriceCheck>> GetLatestForTargetDateAsync(int targetDateId, CancellationToken cancellationToken = default)
    {
        var filter = $"PartitionKey eq '{targetDateId}'";
        
        var byDestination = new Dictionary<string, PriceCheckEntity>();
        await foreach (var entity in _tableClient.QueryAsync<PriceCheckEntity>(filter, cancellationToken: cancellationToken))
        {
            if (!byDestination.TryGetValue(entity.DestinationCode, out var existing) ||
                entity.CheckTimestamp > existing.CheckTimestamp)
            {
                byDestination[entity.DestinationCode] = entity;
            }
        }

        var results = new List<PriceCheck>();
        foreach (var kvp in byDestination)
        {
            var dest = await _destinationRepo.GetByCodeAsync(kvp.Key, cancellationToken);
            if (dest != null)
            {
                var priceCheck = kvp.Value.ToDomain(dest.Id);
                priceCheck.Destination = dest;
                results.Add(priceCheck);
            }
        }
        return results;
    }

    public async Task<IEnumerable<PriceCheck>> GetHistoryAsync(int targetDateId, int destinationId, DateTime since, CancellationToken cancellationToken = default)
    {
        var dest = await _destinationRepo.GetByIdAsync(destinationId, cancellationToken);
        if (dest == null) return Enumerable.Empty<PriceCheck>();

        var filter = $"PartitionKey eq '{targetDateId}' and DestinationCode eq '{dest.AirportCode}'";
        
        var results = new List<PriceCheck>();
        await foreach (var entity in _tableClient.QueryAsync<PriceCheckEntity>(filter, cancellationToken: cancellationToken))
        {
            if (entity.CheckTimestamp >= since)
            {
                var priceCheck = entity.ToDomain(destinationId);
                priceCheck.Destination = dest;
                results.Add(priceCheck);
            }
        }

        return results.OrderByDescending(p => p.CheckTimestamp);
    }

    public async Task<IEnumerable<PriceCheck>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var results = new List<PriceCheck>();
        await foreach (var entity in _tableClient.QueryAsync<PriceCheckEntity>(cancellationToken: cancellationToken))
        {
            if (entity.CheckTimestamp >= startDate && entity.CheckTimestamp <= endDate)
            {
                var dest = await _destinationRepo.GetByCodeAsync(entity.DestinationCode, cancellationToken);
                if (dest != null)
                {
                    var priceCheck = entity.ToDomain(dest.Id);
                    priceCheck.Destination = dest;
                    results.Add(priceCheck);
                }
            }
        }
        return results;
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var count = 0;
        var toDelete = new List<(string partitionKey, string rowKey)>();
        
        await foreach (var entity in _tableClient.QueryAsync<PriceCheckEntity>(cancellationToken: cancellationToken))
        {
            if (entity.CheckTimestamp < cutoffDate)
                toDelete.Add((entity.PartitionKey, entity.RowKey));
        }

        foreach (var (pk, rk) in toDelete)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(pk, rk, cancellationToken: cancellationToken);
                count++;
            }
            catch { /* Ignore errors */ }
        }
        
        return count;
    }

    public async Task<PriceCheck?> GetRecentPriceAsync(int targetDateId, int destinationId, int maxAgeHours, CancellationToken cancellationToken = default)
    {
        var latest = await GetLatestAsync(targetDateId, destinationId, cancellationToken);
        if (latest == null) return null;

        if (DateTime.UtcNow - latest.CheckTimestamp <= TimeSpan.FromHours(maxAgeHours))
            return latest;
        
        return null;
    }

    public async Task<int> DeleteOrphanedPriceChecksAsync(int targetDateId, IEnumerable<int> validDestinationIds, CancellationToken cancellationToken = default)
    {
        var validCodes = new HashSet<string>();
        foreach (var destId in validDestinationIds)
        {
            var dest = await _destinationRepo.GetByIdAsync(destId, cancellationToken);
            if (dest != null) validCodes.Add(dest.AirportCode);
        }

        var filter = $"PartitionKey eq '{targetDateId}'";
        var toDelete = new List<(string partitionKey, string rowKey)>();
        
        await foreach (var entity in _tableClient.QueryAsync<PriceCheckEntity>(filter, cancellationToken: cancellationToken))
        {
            if (!validCodes.Contains(entity.DestinationCode))
                toDelete.Add((entity.PartitionKey, entity.RowKey));
        }

        var count = 0;
        foreach (var (pk, rk) in toDelete)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(pk, rk, cancellationToken: cancellationToken);
                count++;
            }
            catch { /* Ignore errors */ }
        }
        
        return count;
    }
}
