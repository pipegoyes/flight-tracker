using Azure.Data.Tables;

namespace FlightTracker.Data.TableStorage;

public class TableStorageContext
{
    private readonly TableServiceClient _serviceClient;

    public TableStorageContext(string connectionString)
    {
        _serviceClient = new TableServiceClient(connectionString);
    }

    public TableClient GetDestinationsTable() => _serviceClient.GetTableClient("Destinations");
    public TableClient GetTargetDatesTable() => _serviceClient.GetTableClient("TargetDates");
    public TableClient GetPriceChecksTable() => _serviceClient.GetTableClient("PriceChecks");

    public async Task EnsureTablesExistAsync()
    {
        await GetDestinationsTable().CreateIfNotExistsAsync();
        await GetTargetDatesTable().CreateIfNotExistsAsync();
        await GetPriceChecksTable().CreateIfNotExistsAsync();
    }
}
