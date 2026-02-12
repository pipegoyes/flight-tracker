# Flight Tracker - Architecture Document

## Overview

**Architecture Style:** Layered monolith with clean separation of concerns  
**Framework:** ASP.NET Core 8 + Blazor Server  
**Database:** SQLite (embedded)  
**Background Processing:** .NET HostedService  
**Deployment:** Docker container

## High-Level Architecture

```
┌─────────────────────────────────────────────────┐
│              Blazor Server UI                   │
│  (Razor Components, SignalR for real-time)      │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│           Application Services                  │
│  - FlightSearchService                          │
│  - PriceHistoryService                          │
│  - ConfigurationService                         │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│          Domain Layer                           │
│  - Flight (entity)                              │
│  - Destination (entity)                         │
│  - TargetDate (entity)                          │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│         Data Access Layer                       │
│  - FlightTrackerDbContext (EF Core)             │
│  - Repositories                                 │
└─────────────────┬───────────────────────────────┘
                  │
         ┌────────┴────────┐
         │                 │
    ┌────▼─────┐    ┌─────▼────────────────────┐
    │  SQLite  │    │  Flight Provider Layer   │
    │ Database │    │  (IFlightProvider)       │
    └──────────┘    │  - SkyscannerProvider    │
                    │  - KiwiProvider (future) │
                    └──────────────────────────┘
```

## Layer Responsibilities

### 1. Presentation Layer (Blazor UI)

**Components:**
- `Pages/Index.razor` - Main flight search view
- `Components/DateSelector.razor` - Dropdown for target dates
- `Components/FlightCard.razor` - Display individual flight details
- `Components/PriceChart.razor` - (Future: price history visualization)

**Responsibilities:**
- User interaction
- Display data from services
- Real-time updates via SignalR when new prices arrive

### 2. Application Services Layer

#### `FlightSearchService`
```csharp
public class FlightSearchService
{
    private readonly IFlightProvider _flightProvider;
    private readonly IFlightRepository _flightRepository;
    
    public async Task<IEnumerable<FlightResult>> GetLatestPrices(
        int targetDateId)
    {
        // Get latest price check for each destination
        // Returns cached data from DB (not live API call)
    }
    
    public async Task<FlightResult> SearchAndSaveFlight(
        string origin, 
        string destination, 
        DateTime outbound, 
        DateTime returnDate)
    {
        // Calls API via IFlightProvider
        // Finds cheapest flight
        // Saves to database
        // Returns result
    }
}
```

#### `PriceHistoryService`
```csharp
public class PriceHistoryService
{
    public async Task<IEnumerable<PricePoint>> GetPriceHistory(
        int targetDateId, 
        int destinationId, 
        int daysBack = 30)
    {
        // Returns historical prices for charting
    }
    
    public async Task<decimal?> GetPriceChange(
        int targetDateId, 
        int destinationId)
    {
        // Returns % change from 24h ago
    }
}
```

#### `ConfigurationService`
```csharp
public class ConfigurationService
{
    private readonly AppConfig _config;
    
    public string OriginAirport => _config.Origin;
    public IEnumerable<Destination> Destinations { get; }
    public IEnumerable<TargetDate> TargetDates { get; }
    
    public void ReloadConfiguration()
    {
        // Re-read config file
        // Sync with database
    }
}
```

### 3. Domain Layer

**Entities:**

```csharp
public class Destination
{
    public int Id { get; set; }
    public string AirportCode { get; set; } // "PMI"
    public string Name { get; set; }        // "Palma de Mallorca"
}

public class TargetDate
{
    public int Id { get; set; }
    public DateTime OutboundDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public string Name { get; set; }        // "Easter Weekend"
}

public class PriceCheck
{
    public int Id { get; set; }
    public int TargetDateId { get; set; }
    public int DestinationId { get; set; }
    public DateTime CheckTimestamp { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; }
    public TimeOnly DepartureTime { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public string Airline { get; set; }
    public int Stops { get; set; }
    public string BookingUrl { get; set; }
    
    // Navigation properties
    public TargetDate TargetDate { get; set; }
    public Destination Destination { get; set; }
}
```

### 4. Data Access Layer

**Entity Framework Core DbContext:**

```csharp
public class FlightTrackerDbContext : DbContext
{
    public DbSet<Destination> Destinations { get; set; }
    public DbSet<TargetDate> TargetDates { get; set; }
    public DbSet<PriceCheck> PriceChecks { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure indexes
        modelBuilder.Entity<PriceCheck>()
            .HasIndex(p => new { p.TargetDateId, p.DestinationId });
        
        modelBuilder.Entity<PriceCheck>()
            .HasIndex(p => p.CheckTimestamp);
            
        // Configure relationships
        modelBuilder.Entity<PriceCheck>()
            .HasOne(p => p.TargetDate)
            .WithMany()
            .HasForeignKey(p => p.TargetDateId);
            
        modelBuilder.Entity<PriceCheck>()
            .HasOne(p => p.Destination)
            .WithMany()
            .HasForeignKey(p => p.DestinationId);
    }
}
```

### 5. Flight Provider Abstraction

**Interface:**
```csharp
public interface IFlightProvider
{
    Task<FlightSearchResult> SearchFlights(
        string originAirportCode,
        string destinationAirportCode,
        DateTime outboundDate,
        DateTime returnDate,
        CancellationToken cancellationToken = default);
}

public record FlightSearchResult
{
    public bool Success { get; init; }
    public string ErrorMessage { get; init; }
    public IEnumerable<FlightOption> Flights { get; init; }
}

public record FlightOption
{
    public decimal Price { get; init; }
    public string Currency { get; init; }
    public DateTime DepartureTime { get; init; }
    public DateTime ArrivalTime { get; init; }
    public string Airline { get; init; }
    public int Stops { get; init; }
    public string BookingUrl { get; init; }
}
```

**Implementations:**
- `SkyscannerProvider` - Primary implementation
- `MockFlightProvider` - For testing without API calls
- `KiwiProvider` - Future alternative

**Provider Selection via Dependency Injection:**
```csharp
// appsettings.json
{
  "FlightProvider": {
    "Type": "Skyscanner",
    "ApiKey": "xxx",
    "BaseUrl": "https://partners.api.skyscanner.net"
  }
}

// Program.cs
services.AddScoped<IFlightProvider, SkyscannerProvider>();
```

## Background Processing

**Scheduled Price Checking:**

```csharp
public class PriceCheckBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PriceCheckBackgroundService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            
            // Check if it's 6:00 or 18:00 CET
            if (now.Hour == 6 || now.Hour == 18)
            {
                if (now.Minute < 5) // 5-minute window
                {
                    await RunPriceCheck(stoppingToken);
                    
                    // Sleep until next hour to avoid duplicate checks
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
            
            // Check every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
    
    private async Task RunPriceCheck(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var flightSearch = scope.ServiceProvider
            .GetRequiredService<FlightSearchService>();
        var config = scope.ServiceProvider
            .GetRequiredService<ConfigurationService>();
        
        _logger.LogInformation("Starting scheduled price check");
        
        foreach (var targetDate in config.TargetDates)
        {
            foreach (var destination in config.Destinations)
            {
                try
                {
                    await flightSearch.SearchAndSaveFlight(
                        config.OriginAirport,
                        destination.AirportCode,
                        targetDate.OutboundDate,
                        targetDate.ReturnDate);
                    
                    _logger.LogInformation(
                        "Checked {Destination} for {Date}", 
                        destination.Name, 
                        targetDate.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to check {Destination} for {Date}", 
                        destination.Name, 
                        targetDate.Name);
                }
                
                // Rate limiting: delay between API calls
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
        
        _logger.LogInformation("Scheduled price check completed");
    }
}
```

## Configuration Management

**appsettings.json:**
```json
{
  "FlightTracker": {
    "Origin": "FRA",
    "Destinations": [
      { "Code": "PMI", "Name": "Palma de Mallorca" },
      { "Code": "ARN", "Name": "Stockholm Arlanda" },
      { "Code": "TFS", "Name": "Tenerife South" },
      { "Code": "LPA", "Name": "Gran Canaria" }
    ],
    "TargetDates": [
      {
        "Outbound": "2026-04-18",
        "Return": "2026-04-21",
        "Name": "Easter Weekend"
      },
      {
        "Outbound": "2026-06-05",
        "Return": "2026-06-09",
        "Name": "Pentecost"
      }
    ]
  },
  "FlightProvider": {
    "Type": "Skyscanner",
    "ApiKey": "YOUR_API_KEY_HERE"
  },
  "ConnectionStrings": {
    "FlightTracker": "Data Source=flighttracker.db"
  }
}
```

## Project Structure

```
FlightTracker/
├── FlightTracker.sln
├── src/
│   ├── FlightTracker.Web/              # Blazor Server app
│   │   ├── Pages/
│   │   │   └── Index.razor
│   │   ├── Components/
│   │   │   ├── DateSelector.razor
│   │   │   └── FlightCard.razor
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── FlightTracker.Web.csproj
│   │
│   ├── FlightTracker.Core/             # Domain + Services
│   │   ├── Entities/
│   │   │   ├── Destination.cs
│   │   │   ├── TargetDate.cs
│   │   │   └── PriceCheck.cs
│   │   ├── Services/
│   │   │   ├── FlightSearchService.cs
│   │   │   ├── PriceHistoryService.cs
│   │   │   └── ConfigurationService.cs
│   │   ├── Interfaces/
│   │   │   └── IFlightProvider.cs
│   │   └── FlightTracker.Core.csproj
│   │
│   ├── FlightTracker.Data/             # Data Access
│   │   ├── FlightTrackerDbContext.cs
│   │   ├── Repositories/
│   │   └── Migrations/
│   │
│   └── FlightTracker.Providers/        # Flight API implementations
│       ├── Skyscanner/
│       │   └── SkyscannerProvider.cs
│       ├── Mock/
│       │   └── MockFlightProvider.cs
│       └── FlightTracker.Providers.csproj
│
├── tests/
│   ├── FlightTracker.Tests/
│   └── FlightTracker.IntegrationTests/
│
├── Dockerfile
└── docker-compose.yml
```

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/FlightTracker.Web/FlightTracker.Web.csproj", "FlightTracker.Web/"]
COPY ["src/FlightTracker.Core/FlightTracker.Core.csproj", "FlightTracker.Core/"]
COPY ["src/FlightTracker.Data/FlightTracker.Data.csproj", "FlightTracker.Data/"]
COPY ["src/FlightTracker.Providers/FlightTracker.Providers.csproj", "FlightTracker.Providers/"]
RUN dotnet restore "FlightTracker.Web/FlightTracker.Web.csproj"

COPY src/ .
WORKDIR "/src/FlightTracker.Web"
RUN dotnet build "FlightTracker.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlightTracker.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create volume for SQLite database
VOLUME ["/app/data"]
ENV ConnectionStrings__FlightTracker="Data Source=/app/data/flighttracker.db"

ENTRYPOINT ["dotnet", "FlightTracker.Web.dll"]
```

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  flighttracker:
    build: .
    ports:
      - "8080:80"
    volumes:
      - ./data:/app/data
      - ./appsettings.Production.json:/app/appsettings.Production.json
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
```

## Error Handling & Resilience

### API Failure Strategy

```csharp
public class ResilientFlightProvider : IFlightProvider
{
    private readonly IFlightProvider _inner;
    private readonly ILogger _logger;
    
    public async Task<FlightSearchResult> SearchFlights(
        string origin, 
        string destination, 
        DateTime outbound, 
        DateTime returnDate,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        const int delaySeconds = 5;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await _inner.SearchFlights(
                    origin, destination, outbound, returnDate, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, 
                    "API call failed (attempt {Attempt}/{Max}), retrying...", 
                    attempt, maxRetries);
                
                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds * attempt), 
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flight search failed");
                
                return new FlightSearchResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Flights = Enumerable.Empty<FlightOption>()
                };
            }
        }
        
        return new FlightSearchResult
        {
            Success = false,
            ErrorMessage = "Max retries exceeded",
            Flights = Enumerable.Empty<FlightOption>()
        };
    }
}
```

## Logging Strategy

**Serilog Configuration:**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/flighttracker-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();
```

**Key logging points:**
- Each scheduled price check start/end
- Each API call (success/failure)
- Database operations (for troubleshooting)
- Configuration reloads
- User actions (page views, searches)

## Testing Strategy

### Unit Tests
- Service logic (business rules)
- Flight provider implementations (mocked HTTP)
- Data access repositories
- Price calculation logic

### Integration Tests
- Database operations (in-memory SQLite)
- End-to-end price check flow
- Configuration loading
- Background service execution

### Test Data
- `MockFlightProvider` returns predictable test data
- Seed database with sample destinations and dates
- Use in-memory SQLite for fast test execution

## Deployment

### Local Development
```bash
# Run migrations
dotnet ef database update

# Run app
dotnet run --project src/FlightTracker.Web

# Access: http://localhost:5000
```

### Docker Deployment
```bash
# Build image
docker build -t flight-tracker:latest .

# Run container
docker run -p 8080:80 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/appsettings.json:/app/appsettings.Production.json \
  flight-tracker:latest
```

### AWS/Azure Deployment (Future)
- Push Docker image to container registry
- Deploy to AWS ECS / Azure Container Apps
- Use managed secrets for API keys
- Configure persistent volume for SQLite database

## Performance Considerations

### Database Optimization
- Indexes on frequently queried columns
- Periodic cleanup of old price history (>90 days)
- Connection pooling (default in EF Core)

### Caching Strategy
- In-memory cache for latest prices (5-minute TTL)
- Configuration cached after initial load
- SignalR for real-time UI updates when new data arrives

### API Rate Limiting
- 2-second delay between consecutive API calls
- Respect provider rate limits (Skyscanner: ~50/min)
- Exponential backoff on failures

## Security Considerations

1. **API Keys:** Store in environment variables, not code
2. **Input Validation:** Airport codes must match IATA format
3. **SQL Injection:** Prevented by EF Core parameterization
4. **HTTPS:** Required for production deployment
5. **No Authentication:** (MVP - internal tool only)

## Monitoring & Observability

**Health Checks:**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FlightTrackerDbContext>()
    .AddCheck<FlightProviderHealthCheck>("flight-api");
```

**Metrics to Track:**
- Price check success rate
- API response times
- Database query performance
- Background job execution times

---

**Document Version:** 1.0  
**Date:** 2026-02-08  
**Author:** Pepe (AI Assistant)  
**Status:** Ready for Implementation
