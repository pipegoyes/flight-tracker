using Azure;
using Azure.Data.Tables;

namespace FlightTracker.Data.TableStorage.Entities;

public class TargetDateEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "targetdate";
    public string RowKey { get; set; } = string.Empty; // Id as string
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime OutboundDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Comma-separated list of destination airport codes
    public string DestinationCodes { get; set; } = string.Empty;

    public static TargetDateEntity FromDomain(Core.Entities.TargetDate targetDate, IEnumerable<string>? destinationCodes = null)
    {
        return new TargetDateEntity
        {
            RowKey = targetDate.Id.ToString(),
            Name = targetDate.Name,
            OutboundDate = DateTime.SpecifyKind(targetDate.OutboundDate, DateTimeKind.Utc),
            ReturnDate = DateTime.SpecifyKind(targetDate.ReturnDate, DateTimeKind.Utc),
            IsDeleted = targetDate.IsDeleted,
            CreatedAt = DateTime.SpecifyKind(targetDate.CreatedAt, DateTimeKind.Utc),
            UpdatedAt = targetDate.UpdatedAt.HasValue ? DateTime.SpecifyKind(targetDate.UpdatedAt.Value, DateTimeKind.Utc) : null,
            DeletedAt = targetDate.DeletedAt.HasValue ? DateTime.SpecifyKind(targetDate.DeletedAt.Value, DateTimeKind.Utc) : null,
            DestinationCodes = destinationCodes != null ? string.Join(",", destinationCodes) : string.Empty
        };
    }

    public Core.Entities.TargetDate ToDomain()
    {
        return new Core.Entities.TargetDate
        {
            Id = int.TryParse(RowKey, out var id) ? id : 0,
            Name = Name,
            OutboundDate = OutboundDate,
            ReturnDate = ReturnDate,
            IsDeleted = IsDeleted,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DeletedAt = DeletedAt
        };
    }

    public List<string> GetDestinationCodes()
    {
        if (string.IsNullOrEmpty(DestinationCodes))
            return new List<string>();
        return DestinationCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
