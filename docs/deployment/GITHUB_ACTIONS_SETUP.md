# 🚨 GitHub Actions Setup Required

## Issue

The GitHub Personal Access Token used by Pepe doesn't have the `workflow` scope, which is required to create or modify GitHub Actions workflow files.

## Solution Options

### Option 1: Add Files Manually (Easiest)

The workflow files are ready in your local repository but need to be pushed by you:

```bash
cd /path/to/flight-tracker

# Add the workflow files
git add .github/workflows/

# Commit
git commit -m "Add GitHub Actions CI/CD workflow for Azure deployment

- Automated build, test, and deployment pipeline
- Triggers on push to main branch
- Builds Docker image and pushes to ACR
- Deploys to Azure App Service
- Health checks and failure notifications"

# Push
git push origin main
```

### Option 2: Update GitHub Token (For Future)

If you want Pepe to be able to manage workflows in the future:

1. Go to: https://github.com/settings/tokens
2. Click on your token or create a new one
3. **Check the `workflow` scope:**
   - ✅ `workflow` - Update GitHub Action workflows
4. Regenerate/Save the token
5. Update the token in OpenClaw configuration

---

## Files Ready to Push

These files are in your local repository (`.github/workflows/`):

### 1. `azure-deploy.yml`
The main CI/CD pipeline that:
- ✅ Builds and tests .NET solution
- ✅ Builds Docker image
- ✅ Pushes to Azure Container Registry
- ✅ Deploys to Azure App Service
- ✅ Runs health checks

### 2. `README.md`
Complete documentation for:
- ✅ Setting up GitHub secrets
- ✅ Creating Azure service principal
- ✅ Configuring the workflow
- ✅ Troubleshooting common issues

---

## After Adding Workflows

Once the workflow files are pushed, you need to configure GitHub secrets:

### Required Secrets (6 total)

Go to: `https://github.com/YOUR_USERNAME/flight-tracker/settings/secrets/actions`

Add these secrets (get values from Terraform outputs):

| Secret Name | How to Get Value |
|-------------|------------------|
| `ACR_LOGIN_SERVER` | `terraform output -raw container_registry_login_server` |
| `ACR_USERNAME` | `terraform output -raw container_registry_admin_username` |
| `ACR_PASSWORD` | `terraform output -raw container_registry_admin_password` |
| `AZURE_WEBAPP_NAME` | `terraform output -raw app_service_name` |
| `AZURE_RESOURCE_GROUP` | `terraform output -raw resource_group_name` |
| `AZURE_CREDENTIALS` | See section below |

### Creating AZURE_CREDENTIALS

Run this command to create a service principal:

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

Copy the **entire JSON output** and paste it as the `AZURE_CREDENTIALS` secret.

---

## Testing the Workflow

After adding files and secrets:

1. Make any small change to your code
2. Commit and push:
   ```bash
   git add .
   git commit -m "test: trigger deployment"
   git push origin main
   ```

3. Watch the workflow run:
   - Go to: https://github.com/YOUR_USERNAME/flight-tracker/actions
   - Click on the running workflow
   - Monitor progress

---

## Why This Happened

GitHub Personal Access Tokens have different scopes (permissions). The scope used by Pepe:
- ✅ Can read repositories
- ✅ Can push code
- ✅ Can create pull requests
- ❌ Cannot modify workflows (requires `workflow` scope)

This is a security feature to prevent accidentally giving too much access.

---

## Current Status

✅ Terraform infrastructure ready  
✅ Deployment documentation complete  
✅ GitHub Actions workflows created locally  
⚠️ **Workflows need to be pushed manually**  
⏳ GitHub secrets configuration pending  

---

**Next Steps:**
1. Push workflow files (Option 1 above)
2. Configure GitHub secrets
3. Trigger first deployment

Need help? Check `.github/workflows/README.md` for detailed instructions.
