# ☁️ Azure Deployment Guide

Complete guide for deploying Flight Tracker to Azure App Service with Terraform and GitHub Actions.

## 🎯 Overview

This deployment uses:
- **Terraform** - Infrastructure as Code
- **Azure App Service** - Managed hosting (B1 tier, free for 12 months)
- **Azure Container Registry** - Docker image storage
- **GitHub Actions** - Automated CI/CD pipeline
- **Application Insights** - Monitoring and logging

---

## 📋 Prerequisites

### 1. Azure Account
- Active Azure subscription
- Free tier available (check in Azure Portal)

### 2. Tools Installed
```bash
# Azure CLI
az --version

# Terraform
terraform --version

# Docker (optional, for local testing)
docker --version
```

### 3. GitHub Repository
- Fork or clone the flight-tracker repository
- Admin access to configure secrets

---

## 🚀 Deployment Steps

### Step 1: Authenticate with Azure

```bash
# Login to Azure
az login

# Select subscription (if you have multiple)
az account list --output table
az account set --subscription "Your Subscription Name"

# Verify
az account show
```

### Step 2: Deploy Infrastructure with Terraform

```bash
cd terraform

# Copy and configure variables
cp terraform.tfvars.example terraform.tfvars
nano terraform.tfvars
```

**Edit `terraform.tfvars`:**
```hcl
# Required: Your Sentry DSN (already in example)
sentry_dsn = "https://1bd573b8c99620f98e60715ad367d054@o4510860611092480.ingest.de.sentry.io/4510860612862032"

# Optional: Use default values or customize
location = "westeurope"
app_service_sku = "B1" # Free tier eligible
```

**Initialize and deploy:**
```bash
# Initialize Terraform
terraform init

# Review plan
terraform plan

# Deploy (takes 3-5 minutes)
terraform apply
```

**Save outputs:**
```bash
# Display all outputs
terraform output

# Save to file for reference
terraform output > ../terraform-outputs.txt
```

### Step 3: Configure GitHub Secrets

Get values from Terraform outputs:

```bash
#!/bin/bash
# Run this script to display all values needed for GitHub secrets

echo "=== GitHub Secrets Configuration ==="
echo ""
echo "1. ACR_LOGIN_SERVER:"
terraform output -raw container_registry_login_server
echo ""
echo ""
echo "2. ACR_USERNAME:"
terraform output -raw container_registry_admin_username
echo ""
echo ""
echo "3. ACR_PASSWORD:"
terraform output -raw container_registry_admin_password
echo ""
echo ""
echo "4. AZURE_WEBAPP_NAME:"
terraform output -raw app_service_name
echo ""
echo ""
echo "5. AZURE_RESOURCE_GROUP:"
terraform output -raw resource_group_name
echo ""
```

**Create Azure Service Principal:**
```bash
# Get subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Get resource group name
RESOURCE_GROUP=$(cd terraform && terraform output -raw resource_group_name)

# Create service principal
az ad sp create-for-rbac \
  --name "github-actions-flight-tracker" \
  --role Contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

**Copy the JSON output** - this is your `AZURE_CREDENTIALS` secret!

**Add secrets to GitHub:**

1. Go to: `https://github.com/YOUR_USERNAME/flight-tracker/settings/secrets/actions`
2. Click "New repository secret"
3. Add each secret:

| Secret Name | Value |
|-------------|-------|
| ACR_LOGIN_SERVER | `flighttrackeracr*.azurecr.io` |
| ACR_USERNAME | `flighttrackeracr*` |
| ACR_PASSWORD | `password from terraform output` |
| AZURE_CREDENTIALS | `{ "clientId": "...", "clientSecret": "..." }` |
| AZURE_WEBAPP_NAME | `flighttracker-*` |
| AZURE_RESOURCE_GROUP | `flight-tracker-rg` |

### Step 4: Trigger Deployment

**Option A: Push to main branch**
```bash
git add .
git commit -m "feat: deploy to Azure"
git push origin main
```

**Option B: Manual trigger**
1. Go to Actions tab in GitHub
2. Select "Deploy to Azure App Service" workflow
3. Click "Run workflow"

### Step 5: Monitor Deployment

Watch the workflow:
1. GitHub → Actions tab
2. Click on the running workflow
3. Monitor each job:
   - ✅ Build and Test
   - ✅ Build and Push Docker
   - ✅ Deploy to Azure

**Expected duration:** 5-8 minutes

### Step 6: Verify Deployment

```bash
# Get app URL
APP_NAME=$(cd terraform && terraform output -raw app_service_name)
echo "App URL: https://${APP_NAME}.azurewebsites.net"

# Check app status
az webapp show \
  --name $APP_NAME \
  --resource-group flight-tracker-rg \
  --query "state"

# View live logs
az webapp log tail \
  --name $APP_NAME \
  --resource-group flight-tracker-rg
```

**Test in browser:**
```
https://flighttracker-*.azurewebsites.net
```

---

## 🔍 SignalR Configuration

The deployment is pre-configured for Blazor Server with SignalR:

### What's Configured

✅ **WebSockets enabled** - Required for SignalR  
✅ **ARR Affinity enabled** - Sticky sessions for SignalR connections  
✅ **Always On enabled** - Keeps app loaded (B1 tier and above)  
✅ **HTTP/2 enabled** - Better performance  

### Verify SignalR

```bash
# Check WebSockets enabled
az webapp config show \
  --name $APP_NAME \
  --resource-group flight-tracker-rg \
  --query "webSocketsEnabled"

# Should return: true
```

### Test SignalR Connection

1. Open browser developer tools (F12)
2. Go to your app URL
3. Check Console tab for:
   ```
   [HubConnectionBuilder] Starting HubConnection.
   [HubConnectionBuilder] Using HubProtocol 'blazorpack'.
   ```

4. Check Network tab for WebSocket connection:
   ```
   Request URL: wss://flighttracker-*.azurewebsites.net/_blazor
   Status: 101 Switching Protocols
   ```

---

## 📊 Monitoring

### Application Insights

Access logs and metrics:
```bash
# Get connection string
cd terraform
terraform output application_insights_connection_string
```

**View in Azure Portal:**
1. Go to: https://portal.azure.com
2. Search for your App Insights resource
3. View:
   - Live Metrics
   - Failures
   - Performance
   - Logs

### Log Streaming

**Via Azure CLI:**
```bash
az webapp log tail \
  --name $APP_NAME \
  --resource-group flight-tracker-rg
```

**Via Portal:**
1. Azure Portal → App Service
2. Monitoring → Log stream

### Sentry Integration

Errors are automatically sent to Sentry:
- DSN configured in Terraform
- Check: https://sentry.io

---

## 🔄 Updates and Redeployments

### Automatic Deployment

Just push to main:
```bash
git add .
git commit -m "feat: new feature"
git push origin main
```

GitHub Actions handles:
1. Building new Docker image
2. Pushing to ACR
3. Deploying to App Service
4. Restarting the app

### Manual Deployment

```bash
# Build locally
docker build -t flight-tracker:latest .

# Get ACR credentials
ACR_SERVER=$(cd terraform && terraform output -raw container_registry_login_server)
ACR_PASSWORD=$(cd terraform && terraform output -raw container_registry_admin_password)

# Login to ACR
echo $ACR_PASSWORD | docker login $ACR_SERVER -u $ACR_NAME --password-stdin

# Tag and push
docker tag flight-tracker:latest $ACR_SERVER/flight-tracker:latest
docker push $ACR_SERVER/flight-tracker:latest

# Restart App Service
az webapp restart \
  --name $APP_NAME \
  --resource-group flight-tracker-rg
```

### Update Configuration

**Via Terraform:**
```bash
cd terraform
nano terraform.tfvars # Edit values
terraform apply
```

**Via Azure CLI:**
```bash
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group flight-tracker-rg \
  --settings "FlightProvider__Type=BookingCom"
```

---

## 💰 Cost Management

### Monitor Costs

```bash
# View cost analysis
az consumption usage list \
  --start-date $(date -d '30 days ago' +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d)
```

**Azure Portal:**
1. Cost Management + Billing
2. Cost analysis
3. Filter by Resource Group

### Expected Costs (First 12 Months)

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| App Service Plan | B1 | **$0** (free tier) |
| Container Registry | Basic | **$5** |
| Application Insights | Pay-as-you-go | **$0** (within 5 GB) |
| **Total** | | **~$5/month** |

### After Free Tier (Months 13+)

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| App Service Plan | B1 | **$13** |
| Container Registry | Basic | **$5** |
| Application Insights | Pay-as-you-go | **$0** |
| **Total** | | **~$18/month** |

### Cost Optimization

**Reduce ACR costs:**
```bash
# Delete old images
az acr repository show-tags \
  --name $ACR_NAME \
  --repository flight-tracker \
  --output table

az acr repository delete \
  --name $ACR_NAME \
  --image flight-tracker:old-tag
```

**Stop App Service (if not using):**
```bash
az webapp stop --name $APP_NAME --resource-group flight-tracker-rg
```

---

## 🧹 Cleanup

### Delete Everything

```bash
cd terraform
terraform destroy
```

Type `yes` when prompted.

This deletes:
- App Service
- App Service Plan
- Container Registry (and all images)
- Application Insights
- Resource Group

**⚠️ Warning:** This is permanent! Backup data first.

### Partial Cleanup

**Keep infrastructure, remove images:**
```bash
az acr repository delete \
  --name $ACR_NAME \
  --repository flight-tracker \
  --yes
```

**Scale down to save costs:**
```bash
az webapp stop --name $APP_NAME --resource-group flight-tracker-rg
```

---

## 🐛 Troubleshooting

### Common Issues

#### 1. App Won't Start

**Symptoms:** HTTP 503, app doesn't respond

**Diagnosis:**
```bash
# Check logs
az webapp log tail --name $APP_NAME --resource-group flight-tracker-rg

# Check container logs
az webapp log download --name $APP_NAME --resource-group flight-tracker-rg
```

**Common causes:**
- Docker image not found in ACR
- Port mismatch (ensure PORT=8080)
- Missing environment variables
- Application crash on startup

#### 2. SignalR Not Working

**Symptoms:** Blazor interactive components not responding

**Check:**
```bash
# Verify WebSockets enabled
az webapp config show --name $APP_NAME --resource-group flight-tracker-rg \
  --query "webSocketsEnabled"

# Should return: true
```

**Fix:**
```bash
# Enable WebSockets
az webapp config set \
  --name $APP_NAME \
  --resource-group flight-tracker-rg \
  --web-sockets-enabled true
```

#### 3. Deployment Failed

**Check GitHub Actions logs:**
1. GitHub → Actions
2. Click failed workflow
3. Expand failed step

**Common causes:**
- Invalid Azure credentials
- ACR credentials incorrect
- Service principal permissions missing

#### 4. Terraform Errors

**State locked:**
```bash
# Force unlock (use with caution)
terraform force-unlock <lock-id>
```

**Resource already exists:**
```bash
# Import existing resource
terraform import azurerm_linux_web_app.flight_tracker /subscriptions/.../sites/...
```

---

## 📚 Additional Resources

- [Terraform README](terraform/README.md) - Infrastructure details
- [GitHub Actions README](.github/workflows/README.md) - CI/CD setup
- [Azure App Service Docs](https://docs.microsoft.com/azure/app-service/)
- [Blazor SignalR Hosting](https://docs.microsoft.com/aspnet/core/blazor/hosting-models)

---

## ✅ Quick Checklist

- [ ] Azure CLI installed and authenticated
- [ ] Terraform installed (>= 1.0)
- [ ] Azure subscription with free tier available
- [ ] GitHub repository forked/cloned
- [ ] terraform.tfvars configured
- [ ] Infrastructure deployed (`terraform apply`)
- [ ] GitHub secrets configured (6 secrets)
- [ ] Azure service principal created
- [ ] First deployment successful
- [ ] App accessible via HTTPS
- [ ] SignalR working (interactive components)
- [ ] Logs streaming in Application Insights
- [ ] Sentry receiving errors

---

**🎉 Congratulations! Your Flight Tracker is now running on Azure!**

Access your app at: `https://flighttracker-*.azurewebsites.net`

**Generated by Pepe 🐸**  
**Last Updated:** 2026-02-10
