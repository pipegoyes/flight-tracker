# 🔍 Sentry Observability Setup

Production-ready error tracking and performance monitoring for Flight Tracker using Sentry.

## Why Sentry?

- **Real-time error tracking** - Know when something breaks immediately
- **Performance monitoring** - Track slow API calls, database queries
- **Release tracking** - See which deployments introduced issues
- **Alerts** - Email/Slack notifications for critical errors
- **Free tier** - 5,000 events/month, 1 user (perfect for this app)
- **Excellent .NET support** - Built-in ASP.NET Core integration

## What Gets Tracked

### ✅ Automatically Captured

- **Unhandled exceptions** - Any crashes or errors
- **HTTP requests** - Response times, status codes
- **Database queries** - EF Core performance
- **API calls** - Flight provider performance (Booking.com, etc.)
- **Background jobs** - Price check execution
- **User context** - Request path, user agent (no PII)

### 📊 Performance Metrics

- Request duration
- Database query performance
- External API latency
- Memory usage
- Response codes distribution

## Step 1: Create Sentry Account

1. Go to https://sentry.io
2. Sign up for free (GitHub/Google OAuth available)
3. Create a new project:
   - Platform: **ASP.NET Core**
   - Project name: **flight-tracker**
   - Team: Default or create new

## Step 2: Get Your DSN

After creating the project, Sentry will show you a **DSN** (Data Source Name):

```
https://abc123def456...@o123456.ingest.us.sentry.io/7654321
```

Copy this - you'll need it for configuration.

## Step 3: Configure Application

### Option A: appsettings.Production.json (Simple)

```json
{
  "Sentry": {
    "Dsn": "https://YOUR_DSN_HERE@o123456.ingest.us.sentry.io/7654321"
  }
}
```

### Option B: Environment Variables (Recommended for Production)

```bash
export Sentry__Dsn=https://YOUR_DSN_HERE@o123456.ingest.us.sentry.io/7654321
```

### Option C: Docker Environment Variable

```yaml
# docker-compose.yml
services:
  flighttracker:
    environment:
      - Sentry__Dsn=https://YOUR_DSN_HERE@o123456.ingest.us.sentry.io/7654321
```

Or via command line:

```bash
docker run -d \
  -p 8080:8080 \
  -e Sentry__Dsn=https://YOUR_DSN@sentry.io/PROJECT_ID \
  flight-tracker:latest
```

## Step 4: Verify Installation

### Test Error Tracking

1. Start the application
2. Trigger an error (navigate to invalid route, or add test endpoint)
3. Check Sentry dashboard - error should appear within seconds

### Check Logs

Look for Sentry initialization:

```bash
docker logs flight-tracker | grep -i sentry
# Should see: "Sentry initialized successfully"
```

## Configuration Options

The application is configured with these Sentry settings:

```csharp
options.Environment = "Production" or "Development"
options.TracesSampleRate = 1.0  // 100% of transactions tracked
options.Debug = false           // Only true in Development
options.AttachStacktrace = true // Full stack traces
options.SendDefaultPii = false  // No personal data
options.MaxBreadcrumbs = 50     // Context trail
options.EnableTracing = true    // Performance monitoring
```

### What This Means:

- **TracesSampleRate = 1.0**: All HTTP requests tracked (reduce to 0.2 for 20% sampling if needed)
- **SendDefaultPii = false**: No IP addresses, user IDs, or personal data sent
- **Breadcrumbs**: Last 50 actions before error (logs, queries, etc.)

## What You'll See in Sentry

### Errors Tab
- Exception type and message
- Stack trace
- Request URL and method
- Timestamp and frequency
- Affected users (anonymous)

### Performance Tab
- Slowest endpoints
- Database query performance
- External API latency (Booking.com, weather)
- Transaction trends

### Releases Tab
- Which version has issues
- When deployed
- Error rate by version

## Common Scenarios

### 1. Flight Provider API Failure

```
BookingComProvider: HTTP 429 Too Many Requests
/search FRA→PMI 2026-04-17
Rate limit exceeded on Booking.com API
```

**Sentry captures:**
- Exception details
- Request parameters
- API response
- Frequency (how often this happens)

### 2. Database Performance Issue

```
Slow Query Detected: 2.4s
SELECT * FROM PriceChecks WHERE TargetDateId = 1
```

**Sentry shows:**
- Query text
- Duration
- Frequency
- When it started happening

### 3. Background Job Failure

```
PriceCheckBackgroundService: Connection timeout
Failed to check flights at 06:00 CET
```

**Sentry provides:**
- Full context
- Job parameters
- Retry count
- Alert sent to you

## Alerts Configuration

### Recommended Alert Rules:

1. **Critical Errors** - Any unhandled exception
   - Frequency: Immediately
   - Delivery: Email + Slack

2. **High Error Rate** - >10 errors in 5 minutes
   - Frequency: Immediately
   - Delivery: Email

3. **Performance Degradation** - Avg response time >3s
   - Frequency: Once per hour
   - Delivery: Email

**Setup in Sentry:**
- Go to Alerts → Create Alert Rule
- Choose "Issues" or "Performance"
- Set conditions and notification channels

## Free Tier Limits

**Sentry Free Plan:**
- 5,000 events/month
- 1 user
- 10,000 performance units/month
- 30 days retention
- Unlimited projects

**Flight Tracker Usage Estimate:**
- ~500 requests/day = 15,000/month (within limit with sampling)
- Errors: Hopefully <100/month 😊
- Background jobs: 60 checks/month

**Optimization if needed:**
- Reduce `TracesSampleRate` to 0.2 (20% sampling)
- Filter out noisy errors
- Set rate limits per error type

## Security Best Practices

### ✅ Safe (What Sentry Gets):

- Exception messages
- Stack traces
- Request URLs (paths only)
- Response codes
- Performance metrics
- Anonymous breadcrumbs

### ❌ Never Sent (PII Protection):

- User passwords
- API keys (scrubbed automatically)
- Personal user data
- IP addresses (SendDefaultPii = false)
- Email addresses
- Session tokens

### Additional Security:

**1. Scrub Sensitive Data**

Sentry automatically scrubs common patterns, but you can add custom rules:

```csharp
options.BeforeSend = @event =>
{
    // Additional scrubbing if needed
    return @event;
};
```

**2. Environment Variables**

Never commit DSN to Git:

```gitignore
appsettings.Production.json
.env
```

**3. Rotate DSN**

If DSN is compromised, generate a new one in Sentry project settings.

## Cost Monitoring

**Track your usage:**
1. Go to Sentry Dashboard
2. Settings → Usage & Billing
3. See events consumed
4. Set up usage alerts

**If you exceed free tier:**
- Increase sampling rate (reduce from 100%)
- Filter out non-critical errors
- Upgrade to $26/month Team plan (50k events)

## Troubleshooting

### Sentry Not Receiving Events

**Check 1: DSN Configured?**
```bash
docker logs flight-tracker | grep "Sentry"
# Should see initialization message
```

**Check 2: Test Error**
Add temporary test endpoint:
```csharp
app.MapGet("/test-error", () => throw new Exception("Test Sentry"));
```

**Check 3: Firewall**
Ensure outbound HTTPS to `*.sentry.io` is allowed

### Performance Data Not Appearing

- Check `EnableTracing = true`
- Verify `TracesSampleRate > 0`
- Wait a few minutes (can be delayed)

### Too Many Events

- Reduce `TracesSampleRate` to 0.1 (10%)
- Add error filters in Sentry dashboard
- Set rate limits per issue

## Integration with CI/CD

### Track Releases

Add to your deployment script:

```bash
# After deployment
export SENTRY_AUTH_TOKEN=your_token
export SENTRY_ORG=your_org
export SENTRY_PROJECT=flight-tracker

sentry-cli releases new $VERSION
sentry-cli releases set-commits $VERSION --auto
sentry-cli releases finalize $VERSION
```

This lets Sentry show which version introduced errors.

## Useful Sentry Features

### 1. Release Health
Track crash-free sessions per version

### 2. Custom Tags
Add context:
```csharp
SentrySdk.ConfigureScope(scope =>
{
    scope.SetTag("flight_provider", "BookingCom");
    scope.SetTag("destination", "PMI");
});
```

### 3. Breadcrumbs
Already auto-captured, but you can add custom ones:
```csharp
SentrySdk.AddBreadcrumb("Searching flights", "info");
```

### 4. Performance Transactions
Manually track operations:
```csharp
var transaction = SentrySdk.StartTransaction("price-check", "task");
try
{
    // Your code
    transaction.Finish(SpanStatus.Ok);
}
catch
{
    transaction.Finish(SpanStatus.InternalError);
    throw;
}
```

## Alternative: Application Insights

If you prefer Microsoft stack:

**Pros:**
- Deep Azure integration
- Similar features
- Free tier: 5GB/month

**Cons:**
- Azure-centric
- More complex setup
- Better UI than Sentry? (subjective)

Sentry was chosen for simplicity + great .NET support + generous free tier.

## Next Steps

1. ✅ **SDK Installed** - Sentry.AspNetCore added
2. ✅ **Code Configured** - Program.cs updated
3. ⏳ **Get DSN** - Sign up at sentry.io
4. ⏳ **Add DSN** - Update appsettings or env var
5. ⏳ **Test** - Deploy and trigger an error
6. ⏳ **Set Alerts** - Configure email notifications
7. ⏳ **Monitor** - Check dashboard regularly

## Resources

- **Sentry Docs**: https://docs.sentry.io/platforms/dotnet/
- **Dashboard**: https://sentry.io/organizations/YOUR_ORG/issues/
- **Status Page**: https://status.sentry.io/

---

**Status:** ✅ SDK Installed & Configured  
**Cost:** $0/month (Free tier)  
**Setup Time:** ~10 minutes  
**Value:** 🚀 Production-grade observability

**Ready for production monitoring!**
