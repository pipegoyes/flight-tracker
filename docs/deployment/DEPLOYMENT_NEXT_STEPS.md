# 🚀 Flight Tracker - Deployment Next Steps

✅ **All infrastructure code is ready and pushed to GitHub!**

Repository: https://github.com/pipegoyes/flight-tracker

---

## 📋 What's Complete

✅ Terraform infrastructure configuration  
✅ GitHub Actions CI/CD workflow  
✅ SignalR/Blazor Server optimization  
✅ Documentation (Terraform, Azure, CI/CD)  
✅ Cost analysis and free tier info  
✅ All files pushed to GitHub  

---

## 🎯 Next Steps to Deploy

### Step 1: Deploy Azure Infrastructure (10 minutes)

```bash
cd terraform

# Copy and edit configuration
cp terraform.tfvars.example terraform.tfvars
nano terraform.tfvars

# Important: Set your Sentry DSN (already in example):
# sentry_dsn = "https://1bd573b8c99620f98e60715ad367d054@o4510860611092480.ingest.de.sentry.io/4510860612862032"

# Initialize Terraform
terraform init

# Review what will be created
terraform plan

# Deploy (takes 3-5 minutes)
terraform apply
```

**What gets created:**
- Resource Group: `flight-tracker-rg`
- App Service Plan: B1 (FREE for 12 months!)
- App Service: Linux with Docker
- Container Registry: For Docker images
- Application Insights: Monitoring

**Estimated cost:**
- First 12 months: ~$5/month (only ACR)
- After: ~$18/month

---

### Step 2: Get Terraform Outputs (2 minutes)

Save these values - you'll need them for GitHub secrets:

```bash
cd terraform

# Display all outputs
terraform output

# Or save to file
terraform output > ../terraform-outputs.txt

# Get specific values
echo "ACR_LOGIN_SERVER:"
terraform output -raw container_registry_login_server
echo ""
echo "ACR_USERNAME:"
terraform output -raw container_registry_admin_username
echo ""
echo "ACR_PASSWORD:"
terraform output -raw container_registry_admin_password
echo ""
echo "AZURE_WEBAPP_NAME:"
terraform output -raw app_service_name
echo ""
echo "AZURE_RESOURCE_GROUP:"
terraform output -raw resource_group_name
```

---

### Step 3: Create Azure Service Principal (5 minutes)

The GitHub Action needs permission to deploy. Create a service principal:

```bash
# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Get resource group name from Terraform
RESOURCE_GROUP=$(cd terraform && terraform output -raw resource_group_name)

# Create service principal
az ad sp create-for-rbac \
  --name "github-actions-flight-tracker" \
  --role Contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

**Important:** Copy the ENTIRE JSON output! You'll need it for GitHub secrets.

Expected output:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  ...
}
```

---

### Step 4: Configure GitHub Secrets (5 minutes)

Go to: https://github.com/pipegoyes/flight-tracker/settings/secrets/actions

Click **"New repository secret"** and add each of these:

| Secret Name | Value | Where to Get It |
|-------------|-------|-----------------|
| **ACR_LOGIN_SERVER** | `flighttrackeracr*.azurecr.io` | `terraform output -raw container_registry_login_server` |
| **ACR_USERNAME** | `flighttrackeracr*` | `terraform output -raw container_registry_admin_username` |
| **ACR_PASSWORD** | `long-password-string` | `terraform output -raw container_registry_admin_password` |
| **AZURE_CREDENTIALS** | `{ "clientId": "...", ... }` | Full JSON from service principal creation (Step 3) |
| **AZURE_WEBAPP_NAME** | `flighttracker-*` | `terraform output -raw app_service_name` |
| **AZURE_RESOURCE_GROUP** | `flight-tracker-rg` | `terraform output -raw resource_group_name` |

**Total: 6 secrets**

---

### Step 5: Trigger First Deployment (2 minutes)

Once secrets are configured:

**Option A: Manual trigger**
1. Go to: https://github.com/pipegoyes/flight-tracker/actions
2. Click "Deploy to Azure App Service" workflow
3. Click "Run workflow" → "Run workflow"

**Option B: Push a commit**
```bash
# Make any small change
echo "# Deployed to Azure" >> README.md
git add README.md
git commit -m "docs: trigger Azure deployment"
git push origin main
```

The workflow will:
1. ✅ Build and test .NET solution (~2 min)
2. ✅ Build Docker image and push to ACR (~3 min)
3. ✅ Deploy to Azure App Service (~2 min)
4. ✅ Run health checks

**Total time:** ~7-10 minutes

---

### Step 6: Verify Deployment (2 minutes)

After the workflow completes:

```bash
# Get your app URL
APP_NAME=$(cd terraform && terraform output -raw app_service_name)
echo "Your app: https://${APP_NAME}.azurewebsites.net"

# Check app status
az webapp show \
  --name $APP_NAME \
  --resource-group flight-tracker-rg \
  --query "state"

# View logs
az webapp log tail \
  --name $APP_NAME \
  --resource-group flight-tracker-rg
```

**Test in browser:**
Open: `https://flighttracker-*.azurewebsites.net`

You should see the Flight Tracker app running!

---

## 🔍 Monitoring

### Application Insights

Access in Azure Portal:
1. Go to: https://portal.azure.com
2. Search for your App Insights resource
3. View Live Metrics, Logs, Failures

### Sentry

Errors are sent to Sentry:
- Check: https://sentry.io
- View real-time errors and performance

### GitHub Actions

Monitor deployments:
- Actions tab: https://github.com/pipegoyes/flight-tracker/actions
- Each run shows:
  - Build logs
  - Test results
  - Deployment status

---

## 📚 Documentation Reference

| Document | Purpose |
|----------|---------|
| **AZURE_DEPLOYMENT.md** | Complete deployment guide |
| **terraform/README.md** | Terraform infrastructure details |
| **.github/workflows/README.md** | CI/CD setup and troubleshooting |
| **GITHUB_ACTIONS_SETUP.md** | GitHub secrets configuration |

---

## 🎯 Quick Start Checklist

- [ ] Azure CLI installed and authenticated (`az login`)
- [ ] Terraform installed (`terraform --version`)
- [ ] Run `terraform init` in terraform/ directory
- [ ] Edit `terraform.tfvars` with your settings
- [ ] Run `terraform apply` to create infrastructure
- [ ] Create Azure service principal
- [ ] Configure 6 GitHub secrets
- [ ] Trigger deployment (manual or push)
- [ ] Verify app is running
- [ ] Check Application Insights
- [ ] Test SignalR/Blazor interactivity

---

## 💰 Expected Costs

### First 12 Months (Free Tier)
- App Service Plan B1: **$0** (free tier) ✨
- Container Registry Basic: **$5**
- Application Insights: **$0** (within 5 GB free tier)
- **Total: ~$5/month**

### After 12 Months
- App Service Plan B1: **$13**
- Container Registry Basic: **$5**
- Application Insights: **$0**
- **Total: ~$18/month**

**Savings vs current EC2:** $17/month = $204/year 💰

---

## 🐛 Troubleshooting

### Terraform Issues

**Error: "Resource group already exists"**
```bash
# Import existing resource
terraform import azurerm_resource_group.flight_tracker /subscriptions/.../resourceGroups/flight-tracker-rg
```

**Error: "Backend initialization required"**
```bash
cd terraform
rm -rf .terraform
terraform init
```

### GitHub Actions Issues

**Error: "Authentication failed"**
- Verify `AZURE_CREDENTIALS` secret is valid JSON
- Check service principal hasn't expired
- Ensure all 6 secrets are configured

**Error: "Failed to push Docker image"**
- Verify ACR credentials in GitHub secrets
- Check ACR admin user is enabled:
  ```bash
  az acr update --name <acr-name> --admin-enabled true
  ```

### App Service Issues

**App won't start**
```bash
# View logs
az webapp log tail --name <app-name> --resource-group flight-tracker-rg

# Check container logs
az webapp log download --name <app-name> --resource-group flight-tracker-rg
```

**SignalR not working**
```bash
# Verify WebSockets enabled
az webapp config show --name <app-name> --resource-group flight-tracker-rg \
  --query "webSocketsEnabled"
```

---

## 🆘 Need Help?

1. **Check logs:**
   - GitHub Actions logs (in Actions tab)
   - Azure App Service logs (`az webapp log tail`)
   - Application Insights (Azure Portal)

2. **Review documentation:**
   - AZURE_DEPLOYMENT.md
   - terraform/README.md
   - .github/workflows/README.md

3. **Common issues:**
   - Verify all secrets are configured
   - Check Terraform state is clean
   - Ensure service principal has permissions
   - Verify Docker image builds locally

---

## ✅ Success Indicators

You'll know it's working when:
- ✅ Terraform apply completes without errors
- ✅ GitHub Actions workflow runs successfully
- ✅ App Service shows "Running" state
- ✅ URL opens and shows Flight Tracker UI
- ✅ Interactive Blazor components work (SignalR)
- ✅ Background jobs run (check logs)
- ✅ Application Insights receiving data
- ✅ Sentry receiving error reports

---

## 🎉 After Successful Deployment

You'll have:
- ✅ App running on Azure App Service
- ✅ Automatic deployments on every push to main
- ✅ Monitoring with Application Insights and Sentry
- ✅ $0 hosting cost for 12 months (free tier)
- ✅ Professional CI/CD pipeline
- ✅ Infrastructure as Code with Terraform

**Your app will be live at:**
`https://flighttracker-<random>.azurewebsites.net`

---

**Ready to deploy? Start with Step 1!** 🚀

**Generated by Pepe 🐸**  
**Date:** 2026-02-10
