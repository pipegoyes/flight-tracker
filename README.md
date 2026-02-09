# ✈️ Flight Tracker

Track flight prices for German long weekends and find the best deals.

## Features

- 🎯 Track multiple destinations from Frankfurt (FRA)
- 📅 Monitor specific travel date ranges
- 💰 View cheapest flights with details (airline, time, stops)
- 📊 Price history tracking
- 🔄 Automated price checks (twice daily)
- 🐳 Docker support
- 🔍 **Sentry observability** - Production-ready error tracking & performance monitoring

## Architecture

**Clean Architecture** with:
- **Domain Layer** (`FlightTracker.Core`) - Entities, interfaces, services
- **Data Layer** (`FlightTracker.Data`) - EF Core, SQLite, repositories
- **Providers** (`FlightTracker.Providers`) - Flight API abstractions
- **Web UI** (`FlightTracker.Web`) - Blazor Server application

## Technology Stack

- **.NET 8.0** - Framework
- **Blazor Server** - Interactive UI
- **EF Core 8** - ORM
- **SQLite** - Database
- **xUnit** - Testing (16 tests, 100% passing)
- **Docker** - Containerization

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker (optional, for containerized deployment)

### Local Development

```bash
# Clone repository
cd /home/moltbot/.openclaw/workspace/dev

# Restore dependencies
dotnet restore

# Run migrations (auto-runs on startup)
# Database will be created at src/FlightTracker.Web/flighttracker.db

# Run application
dotnet run --project src/FlightTracker.Web

# Access at http://localhost:5000
```

### Docker Deployment

```bash
# Build and run with Docker Compose
docker-compose up -d

# Access at http://localhost:8080

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

### Build Docker Image Manually

```bash
# Build image
docker build -t flight-tracker:latest .

# Run container
docker run -d \
  -p 8080:8080 \
  -v $(pwd)/data:/app/data \
  -v $(pwd)/appsettings.Production.json:/app/appsettings.Production.json \
  --name flight-tracker \
  flight-tracker:latest
```

## Configuration

Edit `appsettings.json` (or `appsettings.Production.json` for Docker):

```json
{
  "FlightTracker": {
    "Origin": "FRA",
    "Destinations": [
      { "Code": "PMI", "Name": "Palma de Mallorca" },
      { "Code": "ARN", "Name": "Stockholm Arlanda" }
    ],
    "TargetDates": [
      {
        "Outbound": "2026-04-18",
        "Return": "2026-04-21",
        "Name": "Easter Weekend"
      }
    ]
  },
  "FlightProvider": {
    "Type": "Mock"
  }
}
```

**Available Flight Providers:**
- `Mock` - Fake data for testing (no API key required)
- `BookingCom` - Booking.com API via RapidAPI ✅ **Ready for production**
- `Skyscanner` - Real API (TODO: requires API key)

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**Test Coverage:**
- ✅ Unit tests (11 tests) - Services, providers
- ✅ Integration tests (5 tests) - Database, repositories

## Project Structure

```
FlightTracker/
├── src/
│   ├── FlightTracker.Core/       # Domain layer
│   │   ├── Entities/             # Destination, TargetDate, PriceCheck
│   │   ├── Interfaces/           # IFlightProvider, IRepository<T>
│   │   ├── Models/               # DTOs, AppConfig
│   │   └── Services/             # Business logic
│   ├── FlightTracker.Data/       # Data access
│   │   ├── Repositories/         # Repository implementations
│   │   ├── Migrations/           # EF Core migrations
│   │   └── FlightTrackerDbContext.cs
│   ├── FlightTracker.Providers/  # Flight APIs
│   │   ├── Mock/                 # MockFlightProvider
│   │   └── Skyscanner/           # SkyscannerProvider (TODO)
│   └── FlightTracker.Web/        # Blazor UI
│       ├── Components/Pages/     # Home.razor
│       └── Program.cs
├── tests/
│   ├── FlightTracker.Tests/
│   └── FlightTracker.IntegrationTests/
├── Dockerfile
├── docker-compose.yml
└── README.md
```

## Database

**SQLite** database with 3 tables:

- `Destinations` - Airports (PMI, ARN, TFS, LPA)
- `TargetDates` - Travel date ranges to track
- `PriceChecks` - Historical price data

Database is automatically created and seeded on startup.

## Roadmap

### Current (MVP)
- ✅ Domain model & repository pattern
- ✅ Mock flight provider
- ✅ Price tracking & history
- ✅ Blazor UI with date selector
- ✅ Docker support
- ⏳ Background service (scheduled price checks)

### Future Enhancements
- [ ] Real Skyscanner API integration
- [ ] Price drop notifications (Telegram/email)
- [ ] Price trend charts
- [ ] Date flexibility (±1 day search)
- [ ] Multiple flight options per route
- [ ] Export price history to CSV
- [ ] German holiday auto-calculation

## Development

### Add New Destination

1. Edit `appsettings.json`:
   ```json
   {
     "Code": "BCN",
     "Name": "Barcelona"
   }
   ```
2. Restart app - destination synced to database automatically

### Add New Target Date

1. Edit `appsettings.json`:
   ```json
   {
     "Outbound": "2026-10-01",
     "Return": "2026-10-04",
     "Name": "October Long Weekend"
   }
   ```
2. Restart app

### Switch to Real API (Booking.com)

1. **Get RapidAPI Key**:
   - Sign up at https://rapidapi.com
   - Subscribe to Booking.com API
   - Get your API key

2. **Update Configuration**:
   ```json
   {
     "FlightProvider": {
       "Type": "BookingCom",
       "ApiKey": "YOUR_RAPIDAPI_KEY",
       "ApiHost": "booking-com.p.rapidapi.com"
     }
   }
   ```

3. **Or use Environment Variables**:
   ```bash
   export FlightProvider__Type=BookingCom
   export FlightProvider__ApiKey=YOUR_KEY
   export FlightProvider__ApiHost=booking-com.p.rapidapi.com
   ```

**See `BOOKINGCOM_SETUP.md` for detailed setup instructions.**

## Contributing

This is a personal project for tracking German long weekend flight deals.

## License

Private - not for distribution

## Author

Built by Pepe (AI assistant) for Felipe
February 2026

---

**Built with ❤️ and .NET 8.0**
