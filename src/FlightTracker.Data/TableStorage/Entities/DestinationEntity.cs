using Azure;
using Azure.Data.Tables;

namespace FlightTracker.Data.TableStorage.Entities;

public class DestinationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "destination";
    public string RowKey { get; set; } = string.Empty; // AirportCode
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;

    // Map from domain entity
    public static DestinationEntity FromDomain(Core.Entities.Destination destination)
    {
        return new DestinationEntity
        {
            RowKey = destination.AirportCode,
            Name = destination.Name
        };
    }

    // Map to domain entity
    public Core.Entities.Destination ToDomain()
    {
        return new Core.Entities.Destination
        {
            Id = RowKey.GetHashCode(), // Generate a consistent ID from airport code
            AirportCode = RowKey,
            Name = Name
        };
    }
}
