using Azure;
using Azure.Data.Tables;

namespace FlightTracker.Data.TableStorage.Entities;

public class PriceCheckEntity : ITableEntity
{
    // PartitionKey: TargetDateId (groups all prices for a travel date)
    public string PartitionKey { get; set; } = string.Empty;
    
    // RowKey: Timestamp_DestinationCode (for uniqueness and sorting)
    public string RowKey { get; set; } = string.Empty;
    
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public int TargetDateId { get; set; }
    public string DestinationCode { get; set; } = string.Empty;
    public double Price { get; set; }  // Table Storage doesn't support decimal
    public string Currency { get; set; } = "EUR";
    public string? Airline { get; set; }
    public int Stops { get; set; }
    public string? DepartureTimeStr { get; set; }  // Store TimeOnly as string "HH:mm"
    public string? ArrivalTimeStr { get; set; }    // Store TimeOnly as string "HH:mm"
    public string? BookingUrl { get; set; }
    public DateTime CheckTimestamp { get; set; }

    public static PriceCheckEntity FromDomain(Core.Entities.PriceCheck priceCheck, string destinationCode)
    {
        var checkTime = DateTime.SpecifyKind(priceCheck.CheckTimestamp, DateTimeKind.Utc);
        var timestamp = checkTime.ToString("yyyyMMddHHmmss");
        return new PriceCheckEntity
        {
            PartitionKey = priceCheck.TargetDateId.ToString(),
            RowKey = $"{timestamp}_{destinationCode}",
            TargetDateId = priceCheck.TargetDateId,
            DestinationCode = destinationCode,
            Price = (double)priceCheck.Price,
            Currency = priceCheck.Currency,
            Airline = priceCheck.Airline,
            Stops = priceCheck.Stops,
            DepartureTimeStr = priceCheck.DepartureTime.ToString("HH:mm"),
            ArrivalTimeStr = priceCheck.ArrivalTime.ToString("HH:mm"),
            BookingUrl = priceCheck.BookingUrl,
            CheckTimestamp = checkTime
        };
    }

    public Core.Entities.PriceCheck ToDomain(int destinationId)
    {
        return new Core.Entities.PriceCheck
        {
            Id = RowKey.GetHashCode(),
            TargetDateId = TargetDateId,
            DestinationId = destinationId,
            Price = (decimal)Price,
            Currency = Currency,
            Airline = Airline ?? string.Empty,
            Stops = Stops,
            DepartureTime = TimeOnly.TryParse(DepartureTimeStr, out var dep) ? dep : TimeOnly.MinValue,
            ArrivalTime = TimeOnly.TryParse(ArrivalTimeStr, out var arr) ? arr : TimeOnly.MinValue,
            BookingUrl = BookingUrl,
            CheckTimestamp = CheckTimestamp
        };
    }
}
