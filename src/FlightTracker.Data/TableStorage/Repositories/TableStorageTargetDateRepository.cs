using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using FlightTracker.Data.TableStorage.Entities;

namespace FlightTracker.Data.TableStorage.Repositories;

public class TableStorageTargetDateRepository : ITargetDateRepository
{
    private readonly TableClient _tableClient;
    private readonly TableStorageDestinationRepository _destinationRepo;
    private int _nextId = 1;

    public TableStorageTargetDateRepository(
        TableStorageContext context,
        TableStorageDestinationRepository destinationRepo)
    {
        _tableClient = context.GetTargetDatesTable();
        _destinationRepo = destinationRepo;
        InitializeNextId();
    }

    private void InitializeNextId()
    {
        try
        {
            var entities = _tableClient.Query<TargetDateEntity>();
            foreach (var entity in entities)
            {
                if (int.TryParse(entity.RowKey, out var id) && id >= _nextId)
                    _nextId = id + 1;
            }
        }
        catch { /* Table might not exist yet */ }
    }

    public async Task<TargetDate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TargetDateEntity>("targetdate", id.ToString(), cancellationToken: cancellationToken);
            return await LoadDestinations(response.Value, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<TargetDate?> GetByIdWithDestinationsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<TargetDate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<TargetDate>();
        await foreach (var entity in _tableClient.QueryAsync<TargetDateEntity>(cancellationToken: cancellationToken))
        {
            if (!entity.IsDeleted)
                results.Add(await LoadDestinations(entity, cancellationToken));
        }
        return results;
    }

    public async Task<IEnumerable<TargetDate>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<TargetDate>();
        await foreach (var entity in _tableClient.QueryAsync<TargetDateEntity>(cancellationToken: cancellationToken))
        {
            results.Add(await LoadDestinations(entity, cancellationToken));
        }
        return results;
    }

    public async Task<IEnumerable<TargetDate>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<TargetDate>();
        await foreach (var entity in _tableClient.QueryAsync<TargetDateEntity>(cancellationToken: cancellationToken))
        {
            if (entity.IsDeleted)
                results.Add(await LoadDestinations(entity, cancellationToken));
        }
        return results;
    }

    public async Task<IEnumerable<TargetDate>> GetUpcomingAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(t => t.OutboundDate >= DateTime.Today).OrderBy(t => t.OutboundDate);
    }

    public async Task<IEnumerable<TargetDate>> FindAsync(Expression<Func<TargetDate, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(predicate.Compile());
    }

    public async Task<TargetDate?> GetByDatesAsync(DateTime outboundDate, DateTime returnDate, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(t => t.OutboundDate.Date == outboundDate.Date && t.ReturnDate.Date == returnDate.Date);
    }

    public async Task<TargetDate> AddAsync(TargetDate targetDate, CancellationToken cancellationToken = default)
    {
        targetDate.Id = _nextId++;
        targetDate.CreatedAt = DateTime.UtcNow;
        
        var destCodes = targetDate.TargetDateDestinations?.Select(tdd => tdd.Destination?.AirportCode).Where(c => c != null).Cast<string>()
                        ?? Enumerable.Empty<string>();
        var entity = TargetDateEntity.FromDomain(targetDate, destCodes);
        
        await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        return targetDate;
    }

    public async Task<TargetDate> CreateTargetDateAsync(TargetDate targetDate, CancellationToken cancellationToken = default)
    {
        return await AddAsync(targetDate, cancellationToken);
    }

    public async Task UpdateAsync(TargetDate targetDate, CancellationToken cancellationToken = default)
    {
        targetDate.UpdatedAt = DateTime.UtcNow;
        var destCodes = targetDate.TargetDateDestinations?.Select(tdd => tdd.Destination?.AirportCode).Where(c => c != null).Cast<string>()
                        ?? Enumerable.Empty<string>();
        var entity = TargetDateEntity.FromDomain(targetDate, destCodes);
        await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateTargetDateAsync(TargetDate targetDate, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(targetDate, cancellationToken);
        return true;
    }

    public async Task DeleteAsync(TargetDate targetDate, CancellationToken cancellationToken = default)
    {
        await _tableClient.DeleteEntityAsync("targetdate", targetDate.Id.ToString(), cancellationToken: cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var targetDate = await GetByIdAsync(id, cancellationToken);
        if (targetDate == null) return false;
        
        targetDate.IsDeleted = true;
        targetDate.DeletedAt = DateTime.UtcNow;
        await UpdateAsync(targetDate, cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TargetDateEntity>("targetdate", id.ToString(), cancellationToken: cancellationToken);
            var entity = response.Value;
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.UpdatedAt = DateTime.UtcNow;
            await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
            return true;
        }
        catch (RequestFailedException)
        {
            return false;
        }
    }

    public async Task<IEnumerable<Destination>> GetDestinationsAsync(int targetDateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TargetDateEntity>("targetdate", targetDateId.ToString(), cancellationToken: cancellationToken);
            var destCodes = response.Value.GetDestinationCodes();
            var results = new List<Destination>();
            foreach (var code in destCodes)
            {
                var dest = await _destinationRepo.GetByCodeAsync(code, cancellationToken);
                if (dest != null) results.Add(dest);
            }
            return results;
        }
        catch (RequestFailedException)
        {
            return Enumerable.Empty<Destination>();
        }
    }

    public async Task UpdateDestinationsAsync(int targetDateId, IEnumerable<int> destinationIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TargetDateEntity>("targetdate", targetDateId.ToString(), cancellationToken: cancellationToken);
            var entity = response.Value;
            
            var codes = new List<string>();
            foreach (var destId in destinationIds)
            {
                var dest = await _destinationRepo.GetByIdAsync(destId, cancellationToken);
                if (dest != null) codes.Add(dest.AirportCode);
            }
            
            entity.DestinationCodes = string.Join(",", codes);
            entity.UpdatedAt = DateTime.UtcNow;
            await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException) { /* Ignore if not found */ }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0); // Table Storage commits immediately
    }

    private async Task<TargetDate> LoadDestinations(TargetDateEntity entity, CancellationToken cancellationToken)
    {
        var domain = entity.ToDomain();
        var destCodes = entity.GetDestinationCodes();
        foreach (var code in destCodes)
        {
            var dest = await _destinationRepo.GetByCodeAsync(code, cancellationToken);
            if (dest != null)
            {
                domain.TargetDateDestinations.Add(new TargetDateDestination
                {
                    TargetDateId = domain.Id,
                    DestinationId = dest.Id,
                    Destination = dest
                });
            }
        }
        return domain;
    }
}
