# GitHub Secrets Setup Guide

## Why This Is Needed

The GitHub Actions deployment workflow sets Azure App Service configuration from GitHub secrets.
Without these secrets, deployments will reset your app settings to empty values!

## Steps to Add Secrets

1. **Go to repository secrets page:**
   https://github.com/pipegoyes/flight-tracker/settings/secrets/actions

2. **Click "New repository secret" for each of the following:**

### Required Secrets

| Secret Name | Value |
|-------------|-------|
| `FLIGHT_PROVIDER_TYPE` | `Amadeus` |
| `FLIGHT_PROVIDER_API_KEY` | `A7z86RxpJMkEdUmkdJkeHhk6OuxoFu5T` |
| `FLIGHT_PROVIDER_API_SECRET` | `ujTCvMfmsRfXZq0x` |
| `FLIGHT_PROVIDER_USE_PRODUCTION` | `false` |
| `SENTRY_DSN` | `https://da5ad2d37257a4b8e0ab7186b36cd662@o4510860611092480.ingest.de.sentry.io/4510873140527184` |

### Existing Secrets (should already be there)

- `ACR_USERNAME` - Azure Container Registry username
- `ACR_PASSWORD` - Azure Container Registry password  
- `AZURE_CREDENTIALS` - Azure service principal credentials

## How to Add Each Secret

For each secret:
1. Click **"New repository secret"**
2. Enter the **Name** (exactly as shown above)
3. Copy and paste the **Value**
4. Click **"Add secret"**

## Verification

After adding all secrets, you can trigger a new deployment:
- Push any commit to `main` branch
- Or go to Actions → Deploy to Azure App Service → Run workflow

The deployment should now preserve your Amadeus configuration!

## Current App Settings (Manually Set)

These are currently set via Azure CLI and working:
- ✅ FlightProvider__Type = Amadeus
- ✅ FlightProvider__ApiKey = A7z86RxpJMkEdUmkdJkeHhk6OuxoFu5T
- ✅ FlightProvider__ApiSecret = ujTCvMfmsRfXZq0x
- ✅ FlightProvider__UseProduction = false
- ✅ Sentry__Dsn = (production DSN)

But they will be overwritten on next deployment unless GitHub secrets are added!
