# GitHub Setup for CI/CD

This guide covers setting up GitHub environments and secrets for automated deployment to Azure.

## GitHub Environment Setup

### 1. Create Production Environment

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Environments**
3. Click **New environment**
4. Name it: `production`
5. Click **Configure environment**

### 2. Configure Environment Secrets

Add the following secrets to the **production** environment:

#### Sentry Configuration

- **Secret name**: `SENTRY_DSN`
- **Value**: Your production Sentry DSN
- **Example**: `https://abc123@o123456.ingest.sentry.io/456789`

#### Flight Provider Configuration (Optional - use if you want RapidAPI in production)

- **Secret name**: `FLIGHT_PROVIDER_TYPE`
- **Value**: `BookingCom` or `Mock`

- **Secret name**: `FLIGHT_PROVIDER_API_KEY`
- **Value**: Your RapidAPI key

- **Secret name**: `FLIGHT_PROVIDER_API_HOST`
- **Value**: `booking-com15.p.rapidapi.com`

## Repository Secrets Setup

These secrets are required at the repository level (not environment-specific):

### 3. Azure Container Registry Secrets

Navigate to **Settings** → **Secrets and variables** → **Actions** → **Repository secrets**

1. **ACR_LOGIN_SERVER**
   - Value: Your ACR login server (e.g., `flighttracker123.azurecr.io`)
   - Get from Terraform output or Azure Portal

2. **ACR_USERNAME**
   - Value: Your ACR admin username
   - Get from: `az acr credential show --name flighttracker123 --query username -o tsv`

3. **ACR_PASSWORD**
   - Value: Your ACR admin password
   - Get from: `az acr credential show --name flighttracker123 --query "passwords[0].value" -o tsv`

### 4. Azure Deployment Secrets

4. **AZURE_CREDENTIALS**
   - Value: Service principal credentials in JSON format
   ```json
   {
     "clientId": "your-client-id",
     "clientSecret": "your-client-secret",
     "subscriptionId": "your-subscription-id",
     "tenantId": "your-tenant-id"
   }
   ```
   - Get from: `az ad sp create-for-rbac --name "github-actions-sp" --role contributor --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} --sdk-auth`

5. **AZURE_WEBAPP_NAME**
   - Value: Your App Service name (e.g., `flight-tracker-prod`)
   - Get from Terraform output or Azure Portal

6. **AZURE_RESOURCE_GROUP**
   - Value: Your resource group name (e.g., `flight-tracker-rg`)
   - Get from Terraform output

## Quick Setup Script

```bash
# Get ACR credentials
ACR_NAME="flighttracker123"
ACR_SERVER=$(az acr show --name $ACR_NAME --query loginServer -o tsv)
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv)

echo "ACR_LOGIN_SERVER=$ACR_SERVER"
echo "ACR_USERNAME=$ACR_USERNAME"
echo "ACR_PASSWORD=$ACR_PASSWORD"

# Create service principal for GitHub Actions
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RESOURCE_GROUP="flight-tracker-rg"

az ad sp create-for-rbac \
  --name "github-actions-sp" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth

# Get App Service name
WEBAPP_NAME=$(az webapp list --resource-group $RESOURCE_GROUP --query "[0].name" -o tsv)
echo "AZURE_WEBAPP_NAME=$WEBAPP_NAME"
echo "AZURE_RESOURCE_GROUP=$RESOURCE_GROUP"
```

## Sentry Production Project Setup

1. Go to [sentry.io](https://sentry.io)
2. Create a new project:
   - Name: `flight-tracker-production`
   - Platform: ASP.NET Core
3. Copy the DSN from the project settings
4. Add to GitHub environment secret `SENTRY_DSN`

## Testing the Pipeline

1. Push a commit to `main` branch
2. Watch the Actions tab: https://github.com/pipegoyes/flight-tracker/actions
3. The workflow will:
   - ✅ Run tests
   - ✅ Build Docker image
   - ✅ Push to ACR
   - ✅ Configure App Service settings
   - ✅ Deploy to Azure
   - ✅ Restart the app

## Manual Trigger

You can also trigger deployment manually:
1. Go to **Actions** → **Deploy to Azure App Service**
2. Click **Run workflow**
3. Select branch: `main`
4. Click **Run workflow**

## Feature Flags in Production

The following settings are automatically configured for production (via `appsettings.Production.json`):

- ✅ `Seeding.SeedDemoTravelDates = false` - No demo Rheinland-Pfalz holidays
- ✅ `Seeding.SeedHistoricalPrices = false` - No fake price data
- ✅ Separate Sentry DSN via environment variable
- ✅ Flight provider configured via environment variables

Users will need to manually add their own travel dates via the UI.
