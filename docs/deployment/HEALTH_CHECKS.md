# Health Checks

Flight Tracker implements ASP.NET Core health checks for monitoring application and database status.

## Endpoints

### `/health` - Full Health Check
Returns detailed health status including database connectivity.

**Response Example:**
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": null,
      "duration": 12.5
    }
  ],
  "totalDuration": 15.2
}
```

**Status Values:**
- `Healthy` - All checks passed
- `Degraded` - Some checks failed but app is functional
- `Unhealthy` - Critical checks failed

**HTTP Status Codes:**
- `200 OK` - All checks healthy
- `503 Service Unavailable` - One or more checks failed

**Usage:**
```bash
# Check full health
curl https://your-app.azurewebsites.net/health

# With jq for pretty output
curl -s https://your-app.azurewebsites.net/health | jq .

# Check only status
curl -s https://your-app.azurewebsites.net/health | jq -r '.status'
```

### `/health/live` - Liveness Check
Simple check that returns 200 if the app is running. Does NOT check database.

**Response:**
```
Healthy
```

**HTTP Status Codes:**
- `200 OK` - App is running
- No response - App is down

**Usage:**
```bash
# Quick liveness check
curl https://your-app.azurewebsites.net/health/live

# Check HTTP status code
curl -o /dev/null -w "%{http_code}" https://your-app.azurewebsites.net/health/live
```

## Health Checks Performed

### Database Check
- **Name:** `database`
- **Tags:** `db`, `sqlite`
- **What it checks:**
  - SQLite database file exists and is accessible
  - Database connection can be established
  - Basic query can execute (`SELECT 1`)

## Monitoring & Alerting

### Azure App Service
Azure automatically uses `/health/live` for:
- Container health probes
- Auto-restart on failures
- Load balancer health checks

Configure in Azure Portal:
1. App Service → Settings → Health check
2. Path: `/health/live`
3. Interval: 30 seconds (default)

### CI/CD Pipeline
GitHub Actions deployment workflow automatically:
1. Checks `/health/live` after deployment (liveness)
2. Checks `/health` for full health status (database)
3. Fails deployment if health checks fail

### External Monitoring
Health check endpoints can be monitored by:
- **Azure Application Insights** (built-in)
- **UptimeRobot** (free tier available)
- **Pingdom**
- **Datadog**
- **New Relic**

Example UptimeRobot configuration:
- URL: `https://your-app.azurewebsites.net/health/live`
- Type: HTTP(s)
- Interval: 5 minutes
- Expected keyword: `Healthy`

## Troubleshooting

### Health check returns 503
1. Check database file exists: `/app/data/flighttracker.db` (production)
2. Check file permissions: App Service should have read/write access
3. Check Application Insights logs for errors
4. Verify connection string in App Service Configuration

### Database check fails locally
```bash
# Check SQLite file
ls -la flighttracker.db

# Test connection manually
sqlite3 flighttracker.db "SELECT 1;"

# Check app logs
dotnet run --urls "http://localhost:5000"
curl http://localhost:5000/health
```

### Health check timeout
- Default timeout: 30 seconds
- Database query should complete in <100ms
- If slow, check:
  - Database file size (large DB files slow SQLite)
  - Disk I/O on App Service
  - Consider migrating to Azure SQL if >100MB

## Testing Locally

### Docker
```bash
# Build and run
docker-compose up -d

# Wait for startup
sleep 5

# Test health checks
curl http://localhost:8080/health | jq .
curl http://localhost:8080/health/live
```

### Development
```bash
# Run app
dotnet run --project src/FlightTracker.Web

# Test health checks
curl http://localhost:5000/health | jq .
curl http://localhost:5000/health/live
```

## Adding Custom Health Checks

To add custom checks (e.g., external API availability):

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<FlightTrackerDbContext>(
        name: "database",
        tags: new[] { "db", "sqlite" })
    .AddUrlGroup(
        new Uri("https://api.example.com/status"),
        name: "external-api",
        tags: new[] { "api", "external" });
```

Filter by tags:
```csharp
// Only database checks
app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

// Only API checks
app.MapHealthChecks("/health/api", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("api")
});
```

## References

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Azure App Service Health Check](https://learn.microsoft.com/en-us/azure/app-service/monitor-instances-health-check)
- [EF Core Health Check Extension](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore)
