# 🔑 Booking.com Provider Setup

## Overview

The Flight Tracker supports real flight price data via the **Booking.com API on RapidAPI**.

This guide walks you through getting API credentials and configuring the provider.

---

## 1. Get RapidAPI Credentials

### Sign Up for RapidAPI

1. Go to https://rapidapi.com
2. Sign up / Log in
3. Navigate to: https://rapidapi.com/marketplace
4. Search for **"Booking.com"**
5. Subscribe to the API (check free tier limits)
6. Copy your **RapidAPI Key** and **API Host**

---

## 2. Configure the Application

### Option A: Configuration File (Recommended for Local Development)

1. Copy the example configuration:
   ```bash
   cp appsettings.BookingCom.example.json appsettings.BookingCom.json
   ```

2. Edit `appsettings.BookingCom.json`:
   ```json
   {
     "FlightProvider": {
       "Type": "BookingCom",
       "ApiKey": "YOUR_RAPIDAPI_KEY_HERE",
       "ApiHost": "booking-com.p.rapidapi.com"
     }
   }
   ```

3. **IMPORTANT**: Never commit `appsettings.BookingCom.json` to Git!
   - It's already in `.gitignore`
   - Only commit the `.example` file

4. Update `appsettings.json` to use BookingCom:
   ```json
   {
     "FlightProvider": {
       "Type": "BookingCom"
     }
   }
   ```

5. Run the application:
   ```bash
   dotnet run --project src/FlightTracker.Web
   ```

### Option B: Environment Variables (Recommended for Docker/Production)

Set environment variables:

```bash
export FlightProvider__Type=BookingCom
export FlightProvider__ApiKey=YOUR_RAPIDAPI_KEY
export FlightProvider__ApiHost=booking-com.p.rapidapi.com
```

Or in Docker Compose:

```yaml
services:
  flighttracker:
    environment:
      - FlightProvider__Type=BookingCom
      - FlightProvider__ApiKey=${RAPIDAPI_KEY}
      - FlightProvider__ApiHost=booking-com.p.rapidapi.com
```

Then create a `.env` file (also in `.gitignore`):

```env
RAPIDAPI_KEY=your_key_here
```

---

## 3. Verify Setup

### Check Logs on Startup

```bash
dotnet run --project src/FlightTracker.Web
```

Look for:
```
✅ info: Program[0]
      Flight provider configured: BookingCom
```

### Test API Call

Visit the UI and trigger a search:
- Go to http://localhost:5000
- Select a date
- Click "Search Flights"
- Check logs for API calls

### Expected Log Output

**Success:**
```
info: FlightTracker.Providers.BookingCom.BookingComProvider[0]
      Searching flights: FRA -> PMI (2026-04-18 to 2026-04-21)
info: FlightTracker.Providers.BookingCom.BookingComProvider[0]
      Found 12 flights, cheapest: €89
```

**API Error:**
```
warn: FlightTracker.Providers.BookingCom.BookingComProvider[0]
      Booking.com API error: 401 Unauthorized
      Check your API key and subscription status
```

---

## 4. Rate Limits & Costs

### Free Tier (RapidAPI)
- **Requests/month**: Check your plan (usually 100-500)
- **Cost per extra request**: Varies by plan

### Flight Tracker Usage
- **Manual searches**: 1 API call per route
- **Background job** (2x daily): 
  - 4 destinations × 6 dates = 24 calls
  - 24 × 2 = **48 calls/day**
  - **~1,440 calls/month**

⚠️ **With free tier, you'll hit limits!** Consider:
- Reducing destinations/dates
- Disabling background service
- Upgrading RapidAPI plan

---

## 5. Troubleshooting

### "401 Unauthorized"
- ✅ Check API key is correct
- ✅ Verify you're subscribed to the API on RapidAPI
- ✅ Check API key hasn't expired

### "429 Too Many Requests"
- ⏳ You've hit rate limits
- Wait for quota reset (usually monthly)
- Upgrade RapidAPI plan

### "No flights found"
- ℹ️ API might not have data for all routes
- Try popular routes first (FRA → PMI works well)
- Check date range isn't too far in future

### "Connection timeout"
- 🌐 RapidAPI might be experiencing issues
- Check https://status.rapidapi.com
- Try again later

---

## 6. Switch Back to Mock Provider

If you want to go back to fake data:

```json
{
  "FlightProvider": {
    "Type": "Mock"
  }
}
```

No API key needed for Mock provider.

---

## 7. Security Best Practices

✅ **DO:**
- Store API keys in environment variables in production
- Use `.env` files locally (never commit them!)
- Use `appsettings.BookingCom.json` locally (in `.gitignore`)
- Rotate API keys periodically

❌ **DON'T:**
- Commit API keys to Git
- Share API keys publicly
- Hardcode keys in source code
- Use production keys in development

---

## 8. Future: Add More Providers

The provider system is extensible. To add Skyscanner or another API:

1. Implement `IFlightProvider` interface
2. Add to `FlightProvider` enum
3. Update `Program.cs` dependency injection
4. Document setup process

---

**Questions?** Check the main README.md or open an issue.
