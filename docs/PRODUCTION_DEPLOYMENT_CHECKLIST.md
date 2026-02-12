# Production Deployment Checklist

This checklist covers all steps needed to deploy Flight Tracker to Azure App Service.

## ✅ Completed Implementation

### 1. Feature Flags ✅
- [x] Added `SeedingConfig` class with two flags:
  - `SeedDemoTravelDates` - Controls automatic Rheinland-Pfalz holiday seeding
  - `SeedHistoricalPrices` - Controls fake price data seeding
- [x] Configured `appsettings.json` with seeding **enabled** (development default)
- [x] Configured `appsettings.Production.json` with seeding **disabled**
- [x] Updated `ConfigurationService.InitializeAllAsync()` to check flag before seeding
- [x] Updated `DataSeeder.SeedHistoricalPriceDataAsync()` to check flag before seeding

**Result**: Production will start with clean database - users add their own travel dates via UI.

### 2. GitHub Environment Setup ✅
- [x] Workflow configured to use `production` environment
- [x] Environment variables configured in workflow:
  - `SENTRY_DSN` - Production Sentry DSN (from GitHub environment secret)
  - `FLIGHT_PROVIDER_TYPE` - Provider type (Mock/BookingCom)
  - `FLIGHT_PROVIDER_API_KEY` - RapidAPI key
  - `FLIGHT_PROVIDER_API_HOST` - RapidAPI host
- [x] Created `docs/GITHUB_SETUP.md` with complete setup instructions

**Result**: Separate Sentry tracking for prod, API keys stored in GitHub secrets.

### 3. CI/CD Pipeline ✅
- [x] GitHub Actions workflow exists (`.github/workflows/azure-deploy.yml`)
- [x] Pipeline steps:
  1. Build and test
  2. Build Docker image
  3. Push to Azure Container Registry
  4. Configure App Service environment variables
  5. Deploy to Azure App Service
  6. Restart app
  7. Health check

**Status**: Ready to test (needs GitHub secrets configured first).

## 🔲 TODO: Pre-Deployment Setup

### Step 1: Create Production Sentry Project

1. Go to [sentry.io](https://sentry.io)
2. Create new project:
   - Name: `flight-tracker-production`
   - Platform: ASP.NET Core
3. Copy the production DSN
4. Save for next step

### Step 2: Configure GitHub Secrets

Follow instructions in `docs/GITHUB_SETUP.md`:

#### Repository Secrets (Settings → Secrets and variables → Actions)
- [ ] `ACR_LOGIN_SERVER` - Azure Container Registry URL
- [ ] `ACR_USERNAME` - ACR admin username
- [ ] `ACR_PASSWORD` - ACR admin password
- [ ] `AZURE_CREDENTIALS` - Service principal JSON
- [ ] `AZURE_WEBAPP_NAME` - App Service name
- [ ] `AZURE_RESOURCE_GROUP` - Resource group name

#### Environment Secrets (Settings → Environments → production)
- [ ] `SENTRY_DSN` - Production Sentry DSN (from Step 1)
- [ ] `FLIGHT_PROVIDER_TYPE` - `Mock` or `BookingCom`
- [ ] `FLIGHT_PROVIDER_API_KEY` - RapidAPI key (optional for now)
- [ ] `FLIGHT_PROVIDER_API_HOST` - `booking-com15.p.rapidapi.com` (optional)

**Tip**: Use the quick setup script in `GITHUB_SETUP.md` to get Azure credentials.

### Step 3: Deploy Terraform Infrastructure

```bash
cd terraform

# Initialize Terraform
terraform init

# Review the plan
terraform plan

# Deploy infrastructure
terraform apply

# Save outputs for GitHub secrets
terraform output
```

### Step 4: Configure GitHub Secrets from Terraform Output

After `terraform apply`, run:

```bash
# Get all required values
terraform output -json > outputs.json

# Extract values (manually copy to GitHub)
cat outputs.json | jq -r '.acr_login_server.value'
cat outputs.json | jq -r '.webapp_name.value'
cat outputs.json | jq -r '.resource_group_name.value'

# Get ACR credentials
ACR_NAME=$(terraform output -raw acr_name)
az acr credential show --name $ACR_NAME
```

### Step 5: Test CI/CD Pipeline

1. Push a commit to `main` branch (or trigger manually)
2. Monitor GitHub Actions: https://github.com/pipegoyes/flight-tracker/actions
3. Verify deployment:
   ```bash
   # Check app is running
   curl https://<WEBAPP_NAME>.azurewebsites.net/
   
   # Check health endpoint (if exists)
   curl https://<WEBAPP_NAME>.azurewebsites.net/health
   ```

### Step 6: Verify Production Configuration

After deployment, check Azure App Service Configuration:

```bash
az webapp config appsettings list \
  --name <WEBAPP_NAME> \
  --resource-group <RESOURCE_GROUP> \
  --query "[?name=='Sentry__Dsn' || name=='FlightProvider__Type'].{Name:name, Value:value}" \
  -o table
```

Expected output:
```
Name                     Value
-----------------------  ------------------------------------
Sentry__Dsn              https://...@sentry.io/...
FlightProvider__Type     Mock (or BookingCom)
```

### Step 7: First Production Test

1. Open the app: `https://<WEBAPP_NAME>.azurewebsites.net`
2. Verify:
   - [ ] No demo travel dates appear (clean database)
   - [ ] No price history (clean database)
   - [ ] Can manually add a travel date via "Manage Travel Dates" page
   - [ ] Can select destinations for that date
3. Check Sentry dashboard:
   - [ ] Production project receives events (not dev project)
4. Test a flight search (if provider configured):
   - [ ] Price check works
   - [ ] Data persists to database

## 📊 Cost Estimate

### First 12 Months (Azure Free Tier)
- Azure Container Registry (Basic): ~$5/month
- App Service (B1 Basic): $0/month (Free tier)
- Database (SQLite): $0 (included)
- **Total**: ~$5/month

### After 12 Months
- Azure Container Registry (Basic): ~$5/month
- App Service (B1 Basic): ~$13/month
- **Total**: ~$18/month

**Savings vs EC2**: $30/month = $360/year

## 🔒 Security Considerations

### Implemented ✅
- [x] Secrets stored in GitHub environment (not in code)
- [x] Separate Sentry DSN for prod/dev
- [x] App Service environment variables (not appsettings files)
- [x] Feature flags prevent demo data in production
- [x] Service principal with minimal permissions (Contributor on RG only)

### Recommended
- [ ] Enable Azure App Service authentication (if needed)
- [ ] Configure CORS if using external frontend
- [ ] Set up Azure Key Vault for sensitive secrets (future)
- [ ] Enable Application Insights (included with App Service)

## 📝 Post-Deployment Tasks

### Monitoring
1. Check Application Insights:
   - Live metrics
   - Failed requests
   - Performance
2. Check Sentry dashboard:
   - Errors/exceptions
   - Performance monitoring

### Database Backup (Optional)
```bash
# Download SQLite database for backup
az webapp download \
  --name <WEBAPP_NAME> \
  --resource-group <RESOURCE_GROUP> \
  --log-file download.log
```

### Regular Maintenance
- Monitor API usage (RapidAPI dashboard when enabled)
- Review Sentry errors weekly
- Check Azure costs monthly
- Update Docker base images quarterly

## 🚨 Troubleshooting

### Deployment fails at "Build and push Docker image"
- Check ACR credentials in GitHub secrets
- Verify Dockerfile builds locally: `docker build -t test .`

### Deployment succeeds but app doesn't start
- Check App Service logs: `az webapp log tail --name <NAME> --resource-group <RG>`
- Verify environment variables are set
- Check database file permissions (/app/data/ should be writable)

### Sentry not receiving events
- Verify `SENTRY_DSN` environment variable is set
- Check Sentry project is active
- Look for connection errors in App Service logs

### Database resets on redeploy
- Ensure volume mount configured in App Service
- Check `appsettings.Production.json` connection string points to `/app/data/`
- Verify persistent storage configured in Terraform

## 📚 References

- [GitHub Setup Guide](./GITHUB_SETUP.md)
- [Azure App Service Docs](https://learn.microsoft.com/en-us/azure/app-service/)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Sentry ASP.NET Core](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/)

---

**Ready to deploy?** Start with Step 1: Create Production Sentry Project.
