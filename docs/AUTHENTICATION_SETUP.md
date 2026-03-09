# Authentication Setup Guide

This guide explains how to enable Azure Easy Auth with Microsoft Account for your Flight Tracker app.

## Overview

Azure Easy Auth (App Service Authentication) provides built-in authentication without code changes. Users will need to sign in with a Microsoft account (personal or work) before accessing the app.

## Benefits

- ✅ **No code changes** - Authentication handled by Azure infrastructure
- ✅ **Free** - Included with App Service
- ✅ **Secure** - Industry-standard OAuth 2.0 / OpenID Connect
- ✅ **Simple** - Works with any Microsoft account (Outlook.com, Hotmail, Azure AD, Office 365)
- ✅ **Session management** - Automatic 8-hour sessions with token refresh

## Prerequisites

- Azure subscription with the app already deployed
- Terraform installed (or use Azure CLI)
- App Service running (Basic tier or higher recommended)

## Setup Methods

### Option 1: Terraform (Recommended - Infrastructure as Code)

#### Step 1: Update Terraform Configuration

Edit `terraform/terraform.tfvars` (or create from example):

```hcl
# Enable authentication
enable_authentication = true
```

#### Step 2: Apply Terraform Changes

```bash
cd terraform
terraform plan
terraform apply
```

#### Step 3: Complete Microsoft Identity Setup

After Terraform applies, you need to finalize the Microsoft provider configuration:

```bash
# Get the app's URL
APP_URL=$(az webapp show --name flighttracker-6ebcf3aa --resource-group flight-tracker-rg --query defaultHostName -o tsv)

# Enable Microsoft authentication (this creates the app registration automatically)
az webapp auth microsoft update \
  --name flighttracker-6ebcf3aa \
  --resource-group flight-tracker-rg \
  --enable true \
  --tenant-id common \
  --allowed-audiences "https://${APP_URL}/.auth/login/aad/callback"
```

**Note:** The `common` tenant allows both personal Microsoft accounts and Azure AD accounts.

#### Step 4: Verify

Visit your app URL - you should be redirected to Microsoft login!

```bash
# Open in browser
echo "https://${APP_URL}"
```

---

### Option 2: Azure CLI (Quick Setup)

If you prefer not to use Terraform:

```bash
# 1. Enable authentication
az webapp auth update \
  --name flighttracker-6ebcf3aa \
  --resource-group flight-tracker-rg \
  --enabled true \
  --action RedirectToLoginPage \
  --runtime-version 2

# 2. Configure Microsoft provider
az webapp auth microsoft update \
  --name flighttracker-6ebcf3aa \
  --resource-group flight-tracker-rg \
  --enable true \
  --tenant-id common
```

---

### Option 3: Azure Portal (Point-and-Click)

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your App Service: **flighttracker-6ebcf3aa**
3. In the left menu, click **Authentication**
4. Click **Add identity provider**
5. Select **Microsoft**
6. Choose **"Any Microsoft account"** (recommended)
7. Click **Add**

Done! The portal handles everything automatically.

---

## Configuration Options

### Tenant Types

When configuring Microsoft authentication, you can choose:

- **`common`** (Recommended) - Allows both personal MSA (Outlook, Hotmail) and Azure AD accounts
- **`consumers`** - Personal Microsoft accounts only (Outlook, Hotmail, Xbox, etc.)
- **`organizations`** - Work/school Azure AD accounts only
- **`<tenant-id>`** - Specific organization only

For personal use with flexibility, use **`common`**.

### Session Duration

The default session cookie expires after **8 hours**. To change this, edit `terraform/main.tf`:

```hcl
cookie_expiration {
  cookie_expiration_convention = "FixedTime"
  cookie_expiration_time       = "08:00:00"  # Change to desired duration (HH:MM:SS)
}
```

### Allow Anonymous Access to Specific Paths

If you want some endpoints public (e.g., health check), you can configure exclusion paths in the Azure Portal under Authentication → Settings → Unauthenticated requests.

---

## Testing

### 1. Test Login Flow

```bash
# Open the app
curl -I https://flighttracker-6ebcf3aa.azurewebsites.net/

# Should return 302 redirect to Microsoft login
# HTTP/1.1 302 Found
# Location: https://login.microsoftonline.com/...
```

### 2. Test Authenticated Access

- Open the app in a browser
- Sign in with your Microsoft account
- You should see the Flight Tracker dashboard
- Session cookie valid for 8 hours

### 3. Test Logout

Visit: `https://flighttracker-6ebcf3aa.azurewebsites.net/.auth/logout`

---

## Troubleshooting

### Issue: "Login Failed" or Redirect Loop

**Solution:** Check that the app URL is configured correctly:

```bash
# Verify app URL
az webapp show --name flighttracker-6ebcf3aa --resource-group flight-tracker-rg \
  --query "{Name:name, URL:defaultHostName, AuthEnabled:siteConfig.authSettings.enabled}"
```

### Issue: Can't Access with Work Account

**Solution:** If you only want personal accounts, change tenant to `consumers`:

```bash
az webapp auth microsoft update \
  --name flighttracker-6ebcf3aa \
  --resource-group flight-tracker-rg \
  --tenant-id consumers
```

### Issue: Session Expires Too Quickly

**Solution:** Increase cookie expiration time in Terraform (see Configuration Options above).

### Issue: Health Check Fails

If you have a `/health` endpoint for monitoring, you may need to allow anonymous access:

1. Azure Portal → App Service → Authentication
2. Click on the Microsoft provider
3. Under "Unauthenticated requests", add `/health` to exclusion paths

---

## Disabling Authentication

### Via Terraform

```hcl
# terraform/terraform.tfvars
enable_authentication = false
```

Then:
```bash
terraform apply
```

### Via Azure CLI

```bash
az webapp auth update \
  --name flighttracker-6ebcf3aa \
  --resource-group flight-tracker-rg \
  --enabled false
```

---

## Security Best Practices

1. ✅ **Always use HTTPS** - Authentication requires HTTPS (already enabled)
2. ✅ **Review allowed accounts** - Use `common` for flexibility, or restrict to specific tenant
3. ✅ **Monitor access logs** - Check Azure Application Insights for auth events
4. ✅ **Rotate secrets** - If you use custom client secrets (advanced setup), rotate them regularly
5. ✅ **Enable logging** - Authentication events are logged to Application Insights

---

## Advanced: Custom App Registration (Optional)

For more control (custom branding, specific permissions), you can create a custom Azure AD app registration:

1. Azure Portal → Azure Active Directory → App registrations → New registration
2. Set redirect URI: `https://flighttracker-6ebcf3aa.azurewebsites.net/.auth/login/aad/callback`
3. Create a client secret under Certificates & secrets
4. Use the client ID and secret in Terraform or Azure CLI

For most personal use cases, the **automatic app registration** (default) is sufficient.

---

## Cost

**Free!** Authentication is included with Azure App Service at no additional charge.

---

## Support

- [Azure App Service Authentication docs](https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization)
- [Terraform azurerm_linux_web_app docs](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/linux_web_app)
- [Microsoft identity platform docs](https://learn.microsoft.com/en-us/azure/active-directory/develop/)

---

## Next Steps

After enabling authentication:

1. ✅ Test the login flow
2. ✅ Verify your session persists for 8 hours
3. ✅ Share the app URL with others (they'll need a Microsoft account)
4. ✅ Monitor auth events in Application Insights
5. Consider: Custom domain with HTTPS certificate (optional)
