# 🚀 Ready to Deploy to Azure!

All infrastructure code is ready and your Azure service principal is configured!

## ✅ What's Already Done

- ✅ Service principal created: `moltbot-sp`
- ✅ Terraform configuration ready
- ✅ Authentication script prepared
- ✅ terraform.tfvars configured with your settings

---

## 📋 Quick Deployment (15 minutes)

### Step 1: Install Terraform (if not installed)

```bash
cd /tmp
wget https://releases.hashicorp.com/terraform/1.7.3/terraform_1.7.3_linux_amd64.zip
sudo apt-get update && sudo apt-get install -y unzip
unzip terraform_1.7.3_linux_amd64.zip
sudo mv terraform /usr/local/bin/
terraform --version
```

### Step 2: Configure Azure Authentication

```bash
cd ~/workspace/flight-tracker/terraform

# Set up Azure credentials (already configured in auth.sh)
source ./auth.sh
```

You should see:
```
✅ Azure credentials configured for Terraform

Service Principal: moltbot-sp
Tenant ID: 79900399-969f-4784-81b0-f0f92f9859ba
Subscription ID: 7acf9440-8dbc-4642-9e37-da6b6fcd12b1
```

### Step 3: Initialize Terraform

```bash
terraform init
```

This downloads the Azure provider and prepares Terraform.

### Step 4: Review the Plan

```bash
terraform plan
```

This shows you what will be created:
- Resource Group: `flight-tracker-rg`
- App Service Plan: B1 (FREE for 12 months!)
- App Service: Linux with Docker
- Container Registry: For Docker images
- Application Insights: Monitoring

### Step 5: Deploy Infrastructure

```bash
terraform apply
```

Type `yes` when prompted.

**This takes 3-5 minutes.**

### Step 6: Save Outputs

```bash
# Display all outputs
terraform output

# Save to file
terraform output > ../terraform-outputs.txt

# Or get specific values for GitHub secrets
echo "=== GitHub Secrets Configuration ==="
echo ""
echo "ACR_LOGIN_SERVER:"
terraform output -raw container_registry_login_server
echo ""
echo ""
echo "ACR_USERNAME:"
terraform output -raw container_registry_admin_username
echo ""
echo ""
echo "ACR_PASSWORD:"
terraform output -raw container_registry_admin_password
echo ""
echo ""
echo "AZURE_WEBAPP_NAME:"
terraform output -raw app_service_name
echo ""
echo ""
echo "AZURE_RESOURCE_GROUP:"
terraform output -raw resource_group_name
echo ""
```

---

## 🔐 Configure GitHub Secrets (5 minutes)

### Step 1: Create Service Principal for GitHub Actions

```bash
# Get subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Get resource group name from Terraform
RESOURCE_GROUP=$(terraform output -raw resource_group_name)

# Create service principal for GitHub Actions
az ad sp create-for-rbac \
  --name "github-actions-flight-tracker" \
  --role Contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

**IMPORTANT:** Copy the entire JSON output! You'll need it for GitHub secrets.

### Step 2: Add Secrets to GitHub

Go to: https://github.com/pipegoyes/flight-tracker/settings/secrets/actions

Click "New repository secret" and add each:

| Secret Name | How to Get Value |
|-------------|------------------|
| `ACR_LOGIN_SERVER` | `terraform output -raw container_registry_login_server` |
| `ACR_USERNAME` | `terraform output -raw container_registry_admin_username` |
| `ACR_PASSWORD` | `terraform output -raw container_registry_admin_password` |
| `AZURE_CREDENTIALS` | JSON output from service principal creation above |
| `AZURE_WEBAPP_NAME` | `terraform output -raw app_service_name` |
| `AZURE_RESOURCE_GROUP` | `terraform output -raw resource_group_name` |

---

## 🚀 Deploy the App (2 minutes)

Once GitHub secrets are configured:

**Option A: Manual trigger**
1. Go to: https://github.com/pipegoyes/flight-tracker/actions
2. Click "Deploy to Azure App Service"
3. Click "Run workflow" → "Run workflow"

**Option B: Push to trigger**
```bash
cd ~/workspace/flight-tracker
git pull origin main
echo "# Deployed $(date)" >> README.md
git add README.md
git commit -m "docs: trigger Azure deployment"
git push origin main
```

---

## ✅ Verify Deployment

After workflow completes (~7 minutes):

```bash
# Get your app URL
APP_NAME=$(cd terraform && terraform output -raw app_service_name)
echo "🌐 Your app: https://${APP_NAME}.azurewebsites.net"

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

**Open in browser:**
```
https://flighttracker-*.azurewebsites.net
```

---

## 📊 Expected Results

After successful deployment:

✅ **Infrastructure created:**
- Resource Group in West Europe
- App Service Plan (B1 - FREE tier)
- App Service running Docker container
- Container Registry
- Application Insights

✅ **Cost:**
- First 12 months: ~$5/month (only ACR)
- After: ~$18/month

✅ **App features:**
- Flight price tracking
- SignalR/Blazor Server working
- Background jobs (2x daily)
- Sentry error tracking
- Application Insights monitoring

---

## 🐛 Troubleshooting

### Terraform Errors

**Error: "Provider registry.terraform.io/hashicorp/azurerm could not be found"**
```bash
terraform init
```

**Error: "Error: building account: could not acquire access token"**
```bash
# Re-run authentication
source ./auth.sh

# Verify
az account show
```

### GitHub Actions Errors

**Error: "Authentication failed"**
- Verify all 6 secrets are configured
- Check `AZURE_CREDENTIALS` is valid JSON (from service principal creation)
- Ensure service principal has Contributor role

**Error: "Failed to push image"**
- Verify ACR credentials in GitHub secrets
- Check ACR admin is enabled:
  ```bash
  az acr update --name $(terraform output -raw container_registry_name) --admin-enabled true
  ```

### App Service Issues

**App won't start**
```bash
# Check logs
az webapp log tail --name $APP_NAME --resource-group flight-tracker-rg

# Check if container is running
az webapp show --name $APP_NAME --resource-group flight-tracker-rg \
  --query "siteConfig.linuxFxVersion"
```

---

## 💰 Cost Monitoring

### Check Current Costs

```bash
# View cost analysis
az consumption usage list \
  --start-date $(date -d '30 days ago' +%Y-%m-%d) \
  --end-date $(date +%Y-%m-%d) \
  --query "[].{Date:usageEnd, Cost:pretaxCost}" \
  --output table
```

**Or in Azure Portal:**
1. Go to Cost Management + Billing
2. Click Cost analysis
3. Filter by Resource Group: `flight-tracker-rg`

---

## 🎉 Success Checklist

- [ ] Terraform installed
- [ ] Azure authentication working (`source ./auth.sh`)
- [ ] `terraform init` completed
- [ ] `terraform plan` shows resources to create
- [ ] `terraform apply` successful
- [ ] Outputs saved
- [ ] GitHub Actions service principal created
- [ ] 6 GitHub secrets configured
- [ ] First deployment triggered
- [ ] App accessible via URL
- [ ] SignalR working (Blazor interactive components)
- [ ] Application Insights receiving data
- [ ] Background jobs scheduled

---

## 📚 Documentation

- **AZURE_DEPLOYMENT.md** - Complete deployment guide
- **terraform/README.md** - Infrastructure details
- **.github/workflows/README.md** - CI/CD setup
- **DEPLOYMENT_NEXT_STEPS.md** - Step-by-step guide

---

## 🆘 Need Help?

1. Check logs: `az webapp log tail`
2. Review Application Insights in Azure Portal
3. Check Sentry for errors
4. Review GitHub Actions workflow logs

---

**Ready? Start with Step 1!** 🚀

Your service principal is already configured, so you just need to:
1. Install Terraform
2. Run `terraform apply`
3. Configure GitHub secrets
4. Deploy!

**Estimated total time:** 15-20 minutes from start to deployed app! ⚡

---

**Generated by Pepe 🐸**  
**Date:** 2026-02-10
