using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using FlightTracker.Core.Entities;
using FlightTracker.Core.Interfaces;
using FlightTracker.Data.TableStorage.Entities;

namespace FlightTracker.Data.TableStorage.Repositories;

public class TableStorageDestinationRepository : IDestinationRepository
{
    private readonly TableClient _tableClient;
    private readonly Dictionary<string, int> _codeToIdCache = new();
    private int _nextId = 1;

    public TableStorageDestinationRepository(TableStorageContext context)
    {
        _tableClient = context.GetDestinationsTable();
        InitializeCache();
    }

    private void InitializeCache()
    {
        try
        {
            var entities = _tableClient.Query<DestinationEntity>();
            foreach (var entity in entities)
            {
                var id = _nextId++;
                _codeToIdCache[entity.RowKey] = id;
            }
        }
        catch { /* Table might not exist yet */ }
    }

    public async Task<Destination?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var code = _codeToIdCache.FirstOrDefault(x => x.Value == id).Key;
        if (code == null) return null;
        return await GetByCodeAsync(code, cancellationToken);
    }

    public async Task<Destination?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<DestinationEntity>("destination", code, cancellationToken: cancellationToken);
            var domain = response.Value.ToDomain();
            domain.Id = _codeToIdCache.TryGetValue(code, out var id) ? id : code.GetHashCode();
            return domain;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Destination>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<Destination>();
        await foreach (var entity in _tableClient.QueryAsync<DestinationEntity>(cancellationToken: cancellationToken))
        {
            var domain = entity.ToDomain();
            domain.Id = _codeToIdCache.TryGetValue(entity.RowKey, out var id) ? id : entity.RowKey.GetHashCode();
            results.Add(domain);
        }
        return results;
    }

    public async Task<IEnumerable<Destination>> FindAsync(Expression<Func<Destination, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(predicate.Compile());
    }

    public async Task<Destination> AddAsync(Destination destination, CancellationToken cancellationToken = default)
    {
        var entity = DestinationEntity.FromDomain(destination);
        await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
        
        if (!_codeToIdCache.ContainsKey(destination.AirportCode))
        {
            _codeToIdCache[destination.AirportCode] = _nextId++;
        }
        destination.Id = _codeToIdCache[destination.AirportCode];
        return destination;
    }

    public async Task UpdateAsync(Destination destination, CancellationToken cancellationToken = default)
    {
        var entity = DestinationEntity.FromDomain(destination);
        await _tableClient.UpsertEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Destination destination, CancellationToken cancellationToken = default)
    {
        await _tableClient.DeleteEntityAsync("destination", destination.AirportCode, cancellationToken: cancellationToken);
        _codeToIdCache.Remove(destination.AirportCode);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Table Storage commits immediately, no-op
        return Task.FromResult(0);
    }

    public int GetIdForCode(string code)
    {
        return _codeToIdCache.TryGetValue(code, out var id) ? id : code.GetHashCode();
    }

    public async Task<Destination?> GetByAirportCodeAsync(string airportCode, CancellationToken cancellationToken = default)
    {
        return await GetByCodeAsync(airportCode, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string airportCode, CancellationToken cancellationToken = default)
    {
        var dest = await GetByCodeAsync(airportCode, cancellationToken);
        return dest != null;
    }
}
