# Amadeus Flight Provider

The Amadeus Self-Service API provides real flight data from the industry's leading GDS (Global Distribution System).

## Free Tier

- **Test Environment**: ~2,000 requests/month FREE
- **Production Environment**: Pay-per-use after free quota

## Setup

### 1. Create Amadeus Account

1. Go to [developers.amadeus.com](https://developers.amadeus.com)
2. Click "Register" to create a free account
3. Verify your email

### 2. Create an Application

1. Go to [My Apps](https://developers.amadeus.com/my-apps)
2. Click "Create new app"
3. Name your application (e.g., "FlightTracker")
4. Copy your **API Key** (Client ID) and **API Secret** (Client Secret)

### 3. Configure FlightTracker

#### Option A: Local Development (appsettings.json)

```json
{
  "FlightProvider": {
    "Type": "Amadeus",
    "ApiKey": "YOUR_CLIENT_ID",
    "ApiSecret": "YOUR_CLIENT_SECRET",
    "UseProduction": false
  }
}
```

#### Option B: Azure Deployment (Environment Variables)

Set these in Azure App Service Configuration or via Terraform:

```
FlightProvider__Type = Amadeus
FlightProvider__ApiKey = YOUR_CLIENT_ID
FlightProvider__ApiSecret = YOUR_CLIENT_SECRET
FlightProvider__UseProduction = false
```

#### Option C: GitHub Actions (Secrets)

Add these secrets to your GitHub repository (Settings → Secrets → Actions):

- `FLIGHT_PROVIDER_TYPE`: `Amadeus`
- `FLIGHT_PROVIDER_API_KEY`: Your Client ID
- `FLIGHT_PROVIDER_API_SECRET`: Your Client Secret
- `FLIGHT_PROVIDER_USE_PRODUCTION`: `false`

## Environments

| Environment | Base URL | Free Quota | Data |
|-------------|----------|------------|------|
| Test | test.api.amadeus.com | ~2,000/month | Sample data |
| Production | api.amadeus.com | Pay-per-use | Real-time |

## API Response

The Amadeus Flight Offers Search API returns detailed flight information:

- **Price**: Total price including taxes
- **Airlines**: Carrier codes with airline names
- **Stops**: Number of connections
- **Times**: Departure and arrival times
- **Duration**: Total flight duration

## Rate Limits

- Test: 10 requests/second
- Production: Higher limits based on plan

## Troubleshooting

### "Invalid client credentials"
- Double-check your API Key and Secret
- Ensure no extra whitespace

### "Quota exceeded"
- Wait for monthly quota reset
- Consider upgrading to production

### No flights found
- Check airport codes are valid IATA codes (e.g., FRA, MAD)
- Verify dates are in the future
- Try different routes

## Resources

- [Amadeus Developer Portal](https://developers.amadeus.com)
- [Flight Offers Search API Docs](https://developers.amadeus.com/self-service/category/flights/api-doc/flight-offers-search)
- [Pricing](https://developers.amadeus.com/pricing)
