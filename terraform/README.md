# Flight Tracker - Terraform Infrastructure

This directory contains Terraform configuration for deploying Flight Tracker to Azure App Service with full Blazor Server and SignalR support.

## 🏗️ Infrastructure Components

- **Azure Resource Group** - Container for all resources
- **Azure App Service Plan (B1)** - Free tier eligible for 12 months
- **Azure App Service (Linux)** - Hosts the Blazor Server app with Docker
- **Azure Container Registry** - Stores Docker images
- **Application Insights** - Monitoring and logging

## 🎯 Features Configured

✅ **Blazor Server optimized:**
- WebSockets enabled for SignalR
- Sticky sessions (ARR Affinity) enabled
- Always On enabled (B1 tier and above)
- HTTP/2 support

✅ **Security:**
- HTTPS only
- TLS 1.2 minimum
- Managed identity enabled
- Sensitive values marked as sensitive

✅ **Monitoring:**
- Application Insights integration
- HTTP logs (7-day retention)
- Application logs (file system level)

✅ **Deployment:**
- Azure Container Registry integration
- Docker image pull from ACR
- Health check endpoint configured

---

## 📋 Prerequisites

1. **Azure CLI** installed and authenticated:
   ```bash
   az login
   az account set --subscription "Your Subscription Name"
   ```

2. **Terraform** installed (version >= 1.0):
   ```bash
   terraform --version
   ```

3. **Azure Subscription** with free tier available:
   - Check: https://portal.azure.com → Subscriptions → Free services

---

## 🚀 Quick Start

### Step 1: Configure Variables

```bash
# Copy the example file
cp terraform.tfvars.example terraform.tfvars

# Edit with your values
nano terraform.tfvars
```

**Required variables:**
- `sentry_dsn` - Your Sentry DSN (already configured in example)
- `location` - Azure region (default: westeurope)
- `app_service_sku` - Use "B1" for free tier

### Step 2: Initialize Terraform

```bash
cd terraform
terraform init
```

### Step 3: Review Plan

```bash
terraform plan
```

This shows what resources will be created.

### Step 4: Apply Configuration

```bash
terraform apply
```

Type `yes` when prompted. Takes ~3-5 minutes.

### Step 5: Get Outputs

```bash
terraform output
```

Save these values - you'll need them for GitHub Actions!

---

## 📦 Deploy Docker Image

After Terraform creates the infrastructure:

### Option 1: Manual Push

```bash
# Get ACR credentials
ACR_NAME=$(terraform output -raw container_registry_name)
ACR_SERVER=$(terraform output -raw container_registry_login_server)
ACR_PASSWORD=$(terraform output -raw container_registry_admin_password)

# Login to ACR
echo $ACR_PASSWORD | docker login $ACR_SERVER -u $ACR_NAME --password-stdin

# Tag and push
docker tag flight-tracker:latest $ACR_SERVER/flight-tracker:latest
docker push $ACR_SERVER/flight-tracker:latest

# Restart App Service to pull new image
az webapp restart --name $(terraform output -raw app_service_name) \
                  --resource-group $(terraform output -raw resource_group_name)
```

### Option 2: Use GitHub Actions (Recommended)

See `.github/workflows/azure-deploy.yml` - automatically builds and deploys on push to main.

---

## 🔧 Configuration

### App Service Settings

All app settings are configured via Terraform in `main.tf`:

```hcl
app_settings = {
  Sentry__Dsn = var.sentry_dsn
  FlightTracker__Origin = var.flight_tracker_origin
  FlightProvider__Type = var.flight_provider_type
  # ... and more
}
```

### SignalR Configuration

SignalR settings are pre-configured:

- **WebSockets:** Enabled (`websockets_enabled = true`)
- **Sticky Sessions:** Enabled (`WEBSITE_DISABLE_ARR_AFFINITY = "false"`)
- **Always On:** Enabled for B1+ tiers
- **HTTP/2:** Enabled for better performance

### Environment Variables

Environment variables can be set via:

1. **Terraform variables** (recommended):
   ```hcl
   variable "sentry_dsn" {
     type      = string
     sensitive = true
   }
   ```

2. **Azure Portal** (for quick changes):
   App Service → Configuration → Application settings

3. **Azure CLI**:
   ```bash
   az webapp config appsettings set \
     --name <app-name> \
     --resource-group <rg-name> \
     --settings "Key=Value"
   ```

---

## 📊 Monitoring

### Application Insights

Access logs and metrics:
```bash
# Get Application Insights connection string
terraform output application_insights_connection_string

# View in portal
az monitor app-insights component show \
  --app $(terraform output app_service_name)-insights-* \
  --resource-group $(terraform output resource_group_name)
```

### Log Stream

Watch live logs:
```bash
az webapp log tail \
  --name $(terraform output app_service_name) \
  --resource-group $(terraform output resource_group_name)
```

Or via portal:
Portal → App Service → Log stream

---

## 🔄 Updates

### Update App Configuration

```bash
# Modify terraform.tfvars or variables
nano terraform.tfvars

# Apply changes
terraform apply
```

### Scale Up/Down

Change `app_service_sku` in `terraform.tfvars`:
```hcl
app_service_sku = "B2" # Scale to 2 vCPU, 3.5 GB RAM
```

Then apply:
```bash
terraform apply
```

### Deploy New Image

Push new image to ACR (via GitHub Actions or manually), then:
```bash
az webapp restart --name $(terraform output -raw app_service_name) \
                  --resource-group $(terraform output -raw resource_group_name)
```

---

## 🧹 Cleanup

### Destroy All Resources

```bash
terraform destroy
```

Type `yes` when prompted.

⚠️ **Warning:** This deletes everything, including:
- App Service and data
- Container Registry and images
- Application Insights logs

### Selective Destroy

Remove specific resource:
```bash
terraform destroy -target=azurerm_linux_web_app.flight_tracker
```

---

## 💰 Cost Breakdown

### Free Tier (First 12 Months)

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| App Service Plan | B1 | **$0** (free tier) |
| Container Registry | Basic | **$5** |
| Application Insights | Pay-as-you-go | **$0** (5 GB free) |
| **Total** | | **~$5/month** |

### After Free Tier

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| App Service Plan | B1 | **$13** |
| Container Registry | Basic | **$5** |
| Application Insights | Pay-as-you-go | **$0** (5 GB free) |
| **Total** | | **~$18/month** |

---

## 🔒 Security Best Practices

### Secrets Management

1. **Never commit `terraform.tfvars`** to Git
   - Already in `.gitignore`
   - Use `terraform.tfvars.example` as template

2. **Use Azure Key Vault** for production:
   ```hcl
   data "azurerm_key_vault_secret" "sentry_dsn" {
     name         = "sentry-dsn"
     key_vault_id = var.key_vault_id
   }
   ```

3. **Use environment variables**:
   ```bash
   export TF_VAR_sentry_dsn="your-dsn-here"
   terraform apply
   ```

### Managed Identity

Managed identity is enabled for the App Service. Use it to access Azure resources without storing credentials:

```csharp
// Access Azure Storage, Key Vault, etc. without passwords
var credential = new DefaultAzureCredential();
```

---

## 🐛 Troubleshooting

### App Service Won't Start

1. **Check logs:**
   ```bash
   az webapp log tail -n <app-name> -g <rg-name>
   ```

2. **Verify Docker image:**
   ```bash
   az acr repository show -n <acr-name> --repository flight-tracker
   ```

3. **Check configuration:**
   ```bash
   az webapp config appsettings list -n <app-name> -g <rg-name>
   ```

### SignalR Connection Issues

1. **Verify WebSockets enabled:**
   ```bash
   az webapp config show -n <app-name> -g <rg-name> \
     --query "webSocketsEnabled"
   ```

2. **Check ARR Affinity:**
   ```bash
   az webapp config show -n <app-name> -g <rg-name> \
     --query "clientAffinityEnabled"
   ```

3. **Enable detailed logging** in `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Microsoft.AspNetCore.SignalR": "Debug"
       }
     }
   }
   ```

### Terraform State Issues

If state gets corrupted:
```bash
# Backup state
cp terraform.tfstate terraform.tfstate.backup

# Import existing resource
terraform import azurerm_linux_web_app.flight_tracker /subscriptions/.../resourceGroups/.../providers/Microsoft.Web/sites/...
```

---

## 📚 Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure Container Registry](https://docs.microsoft.com/azure/container-registry/)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Blazor SignalR Hosting](https://docs.microsoft.com/aspnet/core/blazor/hosting-models)
- [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

## 🆘 Support

For issues:
1. Check logs: `az webapp log tail`
2. Review Application Insights
3. Check Sentry for errors
4. Review Terraform state: `terraform show`

---

**Generated by Pepe 🐸**  
**Last Updated:** 2026-02-10
