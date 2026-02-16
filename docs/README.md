# Flight Tracker Documentation

## Structure

```
docs/
├── architecture/          # System design & implementation details
│   ├── BOOKINGCOM_*.md   # Booking.com API integration
│   ├── SENTRY_*.md       # Error tracking setup
│   └── flight-tracker-*  # Core architecture docs
│
├── deployment/           # Deployment & infrastructure
│   ├── AZURE_*.md        # Azure App Service setup
│   ├── GITHUB_*.md       # CI/CD pipeline
│   ├── HEALTH_CHECKS.md  # Health monitoring
│   └── SECRETS_TO_COPY.md # Required secrets
│
├── testing/              # Testing documentation
│   ├── TESTING.md        # Testing strategy
│   └── PLAYWRIGHT_*.md   # Browser automation tests
│
└── FEATURE_*.md          # Feature documentation
```

## Configuration

### Environment Variables (Production)

All production configuration is done via environment variables, NOT config files.

| Variable | Description | Set By |
|----------|-------------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Dockerfile |
| `Sentry__Dsn` | Sentry error tracking DSN | Azure App Service |
| `FlightProvider__Type` | `BookingCom` or `Mock` | Azure App Service |
| `FlightProvider__ApiKey` | RapidAPI key | Azure App Service |
| `FlightProvider__ApiHost` | API host | Azure App Service |
| `ConnectionStrings__FlightTracker` | SQLite path | Dockerfile |

### Why Environment Variables?

1. **Security**: Secrets not in Git
2. **Flexibility**: Same Docker image works in any environment
3. **12-Factor App**: Best practice for cloud-native apps

See [deployment/SECRETS_TO_COPY.md](deployment/SECRETS_TO_COPY.md) for required secrets.
