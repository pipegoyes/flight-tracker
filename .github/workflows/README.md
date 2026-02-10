# GitHub Actions - Azure Deployment

This workflow automatically builds, tests, and deploys the Flight Tracker application to Azure App Service whenever you push to the `main` branch.

## 🔄 Workflow Overview

The deployment pipeline consists of 3 jobs:

1. **Build and Test** - Compiles .NET code and runs tests
2. **Build and Push Docker** - Creates Docker image and pushes to Azure Container Registry
3. **Deploy to Azure** - Deploys the image to Azure App Service

## 📋 Prerequisites

Before the workflow can run, you need to:

1. Deploy infrastructure with Terraform
2. Configure GitHub secrets
3. Set up Azure service principal

---

## 🚀 Setup Instructions

### Step 1: Deploy Infrastructure with Terraform

```bash
cd terraform
terraform init
terraform apply
```

Save the following outputs - you'll need them for GitHub secrets:
```bash
terraform output container_registry_name
terraform output container_registry_login_server
terraform output container_registry_admin_username
terraform output container_registry_admin_password
terraform output app_service_name
terraform output resource_group_name
```

### Step 2: Create Azure Service Principal

The GitHub Action needs permission to deploy to Azure. Create a service principal:

```bash
# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Get resource group name from Terraform
RESOURCE_GROUP=$(terraform output -raw resource_group_name)

# Create service principal with Contributor role scoped to resource group
az ad sp create-for-rbac \
  --name "github-actions-flight-tracker" \
  --role Contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

This command outputs JSON - **SAVE THIS ENTIRE JSON OUTPUT** for the next step!

Example output:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  ...
}
```

### Step 3: Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions → New repository secret

Add the following secrets:

| Secret Name | Value | Where to Get It |
|-------------|-------|-----------------|
| `ACR_LOGIN_SERVER` | `yourregistry.azurecr.io` | `terraform output container_registry_login_server` |
| `ACR_USERNAME` | `yourregistry` | `terraform output container_registry_admin_username` |
| `ACR_PASSWORD` | `password123...` | `terraform output container_registry_admin_password` |
| `AZURE_CREDENTIALS` | `{ "clientId": "...", ... }` | Output from service principal creation (Step 2) |
| `AZURE_WEBAPP_NAME` | `flighttracker-abc123` | `terraform output app_service_name` |
| `AZURE_RESOURCE_GROUP` | `flight-tracker-rg` | `terraform output resource_group_name` |

#### Quick Script to Get Values:

```bash
#!/bin/bash

echo "📋 GitHub Secrets Configuration"
echo "================================"
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
echo ""
echo "⚠️  Create AZURE_CREDENTIALS by running:"
echo "az ad sp create-for-rbac --name github-actions-flight-tracker --role Contributor --scopes /subscriptions/\$SUBSCRIPTION_ID/resourceGroups/$(terraform output -raw resource_group_name) --sdk-auth"
```

Save as `get-secrets.sh`, make executable (`chmod +x get-secrets.sh`), and run.

### Step 4: Test the Workflow

Push a commit to `main` branch:

```bash
git add .
git commit -m "test: trigger deployment"
git push origin main
```

Watch the workflow run:
- Go to your repo → Actions tab
- Click on the running workflow
- Monitor each job

---

## 🔍 Workflow Details

### Triggers

The workflow runs on:
- **Push to `main` branch** (excluding changes to markdown/docs/terraform)
- **Manual trigger** via `workflow_dispatch` (Actions tab → Run workflow)

### Build and Test Job

```yaml
steps:
  - Setup .NET 8.0
  - Restore NuGet packages
  - Build in Release mode
  - Run all tests
```

**Purpose:** Ensure code quality before deployment

### Build and Push Docker Job

```yaml
steps:
  - Checkout code
  - Setup Docker Buildx
  - Login to ACR
  - Build multi-platform image
  - Push to ACR with tags (latest, branch, SHA)
```

**Tags created:**
- `latest` - Always points to latest main branch
- `main-abc1234` - Git commit SHA
- `main` - Branch name

**Caching:** Uses registry cache for faster builds

### Deploy to Azure Job

```yaml
steps:
  - Login to Azure with service principal
  - Deploy Docker image to App Service
  - Restart App Service
  - Health check
  - Logout from Azure
```

**Environment:** Marked as `production` with deployment URL

---

## 🛠️ Customization

### Change Deployment Branch

Edit `.github/workflows/azure-deploy.yml`:

```yaml
on:
  push:
    branches:
      - develop  # Change from 'main' to your branch
```

### Add Environment Variables

In `deploy-to-azure` job, add step before deploy:

```yaml
- name: Update App Settings
  run: |
    az webapp config appsettings set \
      --name ${{ secrets.AZURE_WEBAPP_NAME }} \
      --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
      --settings "NewSetting=Value"
```

### Deploy to Staging First

Create `.github/workflows/azure-deploy-staging.yml`:

```yaml
deploy-to-staging:
  environment:
    name: staging
    url: https://flighttracker-staging.azurewebsites.net
  # ... deploy steps

deploy-to-production:
  needs: deploy-to-staging
  environment:
    name: production
  # ... deploy steps with manual approval
```

Then configure environment protection rules in GitHub.

### Add Notifications

**Slack notification:**

```yaml
- name: Notify Slack
  uses: slackapi/slack-github-action@v1
  with:
    webhook-url: ${{ secrets.SLACK_WEBHOOK }}
    payload: |
      {
        "text": "✅ Deployed to production!"
      }
```

**Discord notification:**

```yaml
- name: Notify Discord
  run: |
    curl -X POST ${{ secrets.DISCORD_WEBHOOK }} \
      -H "Content-Type: application/json" \
      -d '{"content":"✅ Flight Tracker deployed!"}'
```

---

## 🐛 Troubleshooting

### Authentication Failed

**Error:** `Error: Login failed with Error: Unable to authenticate`

**Fix:**
1. Verify `AZURE_CREDENTIALS` secret is correct JSON
2. Ensure service principal has Contributor role
3. Check service principal hasn't expired:
   ```bash
   az ad sp show --id <clientId>
   ```

### Docker Push Failed

**Error:** `Error: failed to push image`

**Fix:**
1. Verify ACR credentials are correct
2. Check ACR admin user is enabled:
   ```bash
   az acr update --name <registry> --admin-enabled true
   ```
3. Ensure ACR exists and is accessible

### Deployment Timeout

**Error:** `Error: Deployment timed out`

**Fix:**
1. Increase timeout in workflow (default is 30 minutes)
2. Check App Service logs for startup errors:
   ```bash
   az webapp log tail --name <app-name> --resource-group <rg-name>
   ```
3. Verify Docker image runs locally:
   ```bash
   docker run -p 8080:8080 <acr>/<image>:latest
   ```

### Health Check Failed

**Error:** `⚠️ Health check failed`

**Fix:**
1. Add `/health` endpoint to your app
2. Or remove health check from workflow
3. Check Application Insights for errors

---

## 📊 Monitoring Deployments

### View Deployment History

```bash
# List deployments
az webapp deployment list \
  --name <app-name> \
  --resource-group <rg-name>

# Show specific deployment
az webapp deployment show \
  --name <app-name> \
  --resource-group <rg-name> \
  --deployment-id <id>
```

### View Deployment Logs

```bash
# Live logs
az webapp log tail \
  --name <app-name> \
  --resource-group <rg-name>

# Download logs
az webapp log download \
  --name <app-name> \
  --resource-group <rg-name> \
  --log-file deployment-logs.zip
```

### GitHub Actions Logs

- Repository → Actions tab
- Click on workflow run
- Click on specific job
- Expand steps to see detailed logs

---

## 🔒 Security Best Practices

### Secrets Management

✅ **DO:**
- Store all credentials as GitHub secrets
- Use service principal with minimal permissions
- Rotate secrets regularly
- Enable secret scanning in GitHub settings

❌ **DON'T:**
- Commit secrets to code
- Share secrets in issues/PRs
- Use personal Azure credentials
- Give service principal Owner role

### Service Principal Permissions

**Recommended:** `Contributor` role scoped to resource group

**Minimal permissions needed:**
- `Microsoft.Web/sites/write` - Deploy to App Service
- `Microsoft.Web/sites/config/write` - Update configuration
- `Microsoft.ContainerRegistry/registries/pull/read` - Pull images

Create custom role if needed:
```bash
az role definition create --role-definition custom-role.json
```

### Audit Deployments

Enable audit logs:
```bash
az monitor activity-log list \
  --resource-group <rg-name> \
  --max-events 50 \
  --query "[?contains(operationName.value, 'Microsoft.Web')]"
```

---

## 📚 Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure App Service Deploy Action](https://github.com/Azure/webapps-deploy)
- [Azure Login Action](https://github.com/Azure/login)
- [Docker Build Push Action](https://github.com/docker/build-push-action)
- [Azure CLI Reference](https://docs.microsoft.com/cli/azure/)

---

## 🆘 Need Help?

1. Check workflow logs in GitHub Actions tab
2. Review Azure App Service logs
3. Check Application Insights for errors
4. Verify all secrets are configured correctly
5. Test Docker image locally first

---

**Generated by Pepe 🐸**  
**Last Updated:** 2026-02-10
