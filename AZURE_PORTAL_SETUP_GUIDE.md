# Azure Portal Setup Guide - VideoMania

Complete step-by-step guide to set up Azure resources for the VideoMania serverless video streaming platform.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Create an Azure Account](#create-an-azure-account)
3. [Access Azure Portal](#access-azure-portal)
4. [Create Resource Group](#create-resource-group)
5. [Set Up Azure App Service](#set-up-azure-app-service)
6. [Set Up Azure Blob Storage](#set-up-azure-blob-storage)
7. [Set Up Azure Cosmos DB](#set-up-azure-cosmos-db)
8. [Configure Connection Strings](#configure-connection-strings)
9. [Deploy Application](#deploy-application)
10. [Verify Deployment](#verify-deployment)
11. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before starting, ensure you have:

- A valid email address (for Azure account)
- A valid credit/debit card (for billing)
- Azure CLI installed on your machine
- .NET 8 SDK installed
- Git installed
- The VideoMania application source code

**Install Azure CLI:**

```powershell
# Download and install Azure CLI for Windows
# Visit: https://aka.ms/installazurecliwindows

# Or using PowerShell (as Administrator):
$ProgressPreference = 'SilentlyContinue';
Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile AzureCLI.msi;
Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'

# Verify installation
az --version
```

---

## Create an Azure Account

### Step 1: Sign Up for Azure Free Trial

1. Visit [https://azure.microsoft.com/en-us/free/](https://azure.microsoft.com/en-us/free/)
2. Click **Start free**
3. Sign in with your Microsoft account or create a new one
4. Fill in your profile information:
   - Name
   - Email
   - Phone number
   - Country/region
5. Add your credit/debit card (no charges for free tier)
6. Review and accept the agreement
7. Click **Sign up**

### Step 2: Verify Your Identity

- Microsoft will send a verification code to your phone
- Enter the code when prompted
- Complete identity verification

### Step 3: Complete Setup

- Set your subscription name
- Select language and currency
- Review free services (12 months free)
- Click **Next**

You now have a **$200 free credit** for 30 days and **12 months free services**.

---

## Access Azure Portal

### Step 1: Sign In to Azure Portal

1. Go to [https://portal.azure.com](https://portal.azure.com)
2. Sign in with your Azure account
3. You'll see the Azure Portal dashboard

### Step 2: Familiarize with the Interface

- **Top Search Bar**: Search for services and resources
- **Left Sidebar**: Navigation menu with common services
- **Home Dashboard**: Overview of your resources and subscriptions
- **Notifications**: Bell icon shows alerts and messages
- **Account Icon**: Your profile, settings, and sign out

---

## Create Resource Group

A Resource Group is a container for all related Azure resources.

### Step 1: Create New Resource Group

1. In the Azure Portal, click **Resource groups** in the left menu
   - Or search "Resource groups" in the search bar
2. Click **+ Create**
3. Fill in the details:

| Field               | Value                                                                              |
| ------------------- | ---------------------------------------------------------------------------------- |
| Subscription        | Select your subscription                                                           |
| Resource group name | `VideoMania-RG`                                                                    |
| Region              | Choose closest to your location (e.g., `East US`, `West Europe`, `Southeast Asia`) |

4. Click **Review + Create**
5. Click **Create**

**⏱️ Wait for deployment (usually 1-2 seconds)**

### Step 2: Verify Creation

- You'll see "Deployment succeeded"
- Click **Go to resource group**
- You're now in your Resource Group

---

## Set Up Azure App Service

Azure App Service hosts your .NET application.

### Step 1: Create App Service Plan

1. In your Resource Group, click **+ Create**
2. Search for "App Service Plan"
3. Click **Create**
4. Fill in the details:

| Field            | Value                       |
| ---------------- | --------------------------- |
| Resource group   | `VideoMania-RG`             |
| Name             | `VideoMania-AppServicePlan` |
| Operating System | Windows                     |
| Region           | Same as Resource Group      |
| Sku and size     | **Free (F1)**               |

5. Click **Review + Create**
6. Click **Create**

**⏱️ Wait for deployment (2-3 minutes)**

### Step 2: Create App Service

1. In your Resource Group, click **+ Create**
2. Search for "App Service"
3. Click **Create**
4. Fill in the details:

| Field            | Value                       |
| ---------------- | --------------------------- |
| Resource group   | `VideoMania-RG`             |
| Name             | `videomania-app-<randomid>` |
| Runtime stack    | .NET 8 (LTS)                |
| Operating System | Windows                     |
| Region           | Same as Resource Group      |
| App Service Plan | `VideoMania-AppServicePlan` |

5. Click **Review + Create**
6. Click **Create**

**⏱️ Wait for deployment (3-5 minutes)**

### Step 3: Get App Service URL

1. Go to your App Service resource
2. Copy the URL from the top (e.g., `https://videomania-app-xxx.azurewebsites.net`)
3. Save this URL - you'll need it later

---

## Set Up Azure Blob Storage

Blob Storage stores your video files.

### Step 1: Create Storage Account

1. In your Resource Group, click **+ Create**
2. Search for "Storage account"
3. Click **Create**
4. Fill in the details:

| Field                | Value                                          |
| -------------------- | ---------------------------------------------- |
| Resource group       | `VideoMania-RG`                                |
| Storage account name | `videomania<randomid>` (lowercase, no hyphens) |
| Region               | Same as Resource Group                         |
| Performance          | Standard                                       |
| Redundancy           | Locally-redundant storage (LRS)                |

5. Click **Review + Create**
6. Click **Create**

**⏱️ Wait for deployment (2-3 minutes)**

### Step 2: Create Blob Container

1. Go to your Storage Account resource
2. In the left menu, click **Containers** (under Data storage)
3. Click **+ Container**
4. Fill in:
   - Name: `videos`
   - Public access level: `Blob (anonymous read access for blobs only)`
5. Click **Create**

### Step 3: Get Connection String

1. In your Storage Account, click **Access keys** (left menu)
2. Under **key1**, click the copy icon next to "Connection string"
3. Save this connection string - you'll need it in configuration

**Connection string format:**

```
DefaultEndpointsProtocol=https;AccountName=xxx;AccountKey=xxx;EndpointSuffix=core.windows.net
```

---

## Set Up Azure Cosmos DB

Cosmos DB stores video metadata and comments.

### Step 1: Create Cosmos DB Account

1. In your Resource Group, click **+ Create**
2. Search for "Azure Cosmos DB"
3. Click **Create** → **Azure Cosmos DB SQL API**
4. Fill in the details:

| Field                    | Value                          |
| ------------------------ | ------------------------------ |
| Resource group           | `VideoMania-RG`                |
| Account Name             | `videomania-cosmos-<randomid>` |
| Location                 | Same as Resource Group         |
| Capacity mode            | Provisioned throughput         |
| Apply Free Tier Discount | Yes (if available)             |

5. Click **Review + Create**
6. Click **Create**

**⏱️ Wait for deployment (5-10 minutes) - This takes longer**

### Step 2: Create Database and Containers

1. Go to your Cosmos DB account
2. Click **Data Explorer** (left menu)
3. Click **New Container** (top menu)
4. Fill in:

| Field         | Value        |
| ------------- | ------------ |
| Database id   | `VideoMania` |
| Container id  | `Videos`     |
| Partition key | `/UserId`    |

5. Click **OK**

**Repeat for Comments container:**

1. Click **New Container**
2. Fill in:
   - Database: `VideoMania` (select existing)
   - Container id: `Comments`
   - Partition key: `/VideoId`
3. Click **OK**

### Step 3: Get Connection String

1. In your Cosmos DB account, click **Keys** (left menu)
2. Copy the **Primary Connection String**
3. Save this connection string - you'll need it in configuration

**Connection string format:**

```
AccountEndpoint=https://xxx.documents.azure.us:443/;AccountKey=xxx;
```

---

## Configure Connection Strings

### Step 1: Update App Service Configuration

1. Go to your App Service resource
2. Click **Configuration** (left menu, under Settings)
3. Click **+ New application setting**

**Add Blob Storage Connection String:**

- Name: `BlobStorageConnection`
- Value: (paste from Blob Storage step above)
- Click **OK**

**Add Cosmos DB Connection String:**

- Name: `CosmosDbConnection`
- Value: (paste from Cosmos DB step above)
- Click **OK**

**Add Cosmos DB Database Name:**

- Name: `CosmosDbDatabaseName`
- Value: `VideoMania`
- Click **OK**

4. Click **Save** at the top
5. Click **Continue** when prompted to restart

### Step 2: Verify Configuration

1. In Configuration, verify all three settings appear under "Application settings"
2. Settings should now appear as:
   ```
   BlobStorageConnection = DefaultEndpointsProtocol=https;...
   CosmosDbConnection = AccountEndpoint=https://...
   CosmosDbDatabaseName = VideoMania
   ```

---

## Deploy Application

### Step 1: Prepare Application for Deployment

```powershell
# Navigate to application directory
cd C:\VideoMania\D224396\ICC.AzureAppService.Demo

# Publish the application
dotnet publish -c Release -o ./bin/Release/publish

# Verify publish folder created
Get-ChildItem .\bin\Release\publish
```

### Step 2: Deploy Using Azure CLI

```powershell
# Login to Azure
az login

# After signing in via browser, run:
# Set subscription (if you have multiple)
az account set --subscription "Your Subscription ID"

# Deploy using ZIP file
cd C:\VideoMania\D224396\ICC.AzureAppService.Demo\bin\Release\publish

# Create ZIP file
Compress-Archive -Path * -DestinationPath app.zip -Force

# Deploy to App Service
az webapp deployment source config-zip --resource-group VideoMania-RG --name videomania-app-xxx --src app.zip

# Replace 'videomania-app-xxx' with your actual App Service name
```

### Step 3: Monitor Deployment

```powershell
# Check deployment status
az webapp deployment list-publishing-profiles --resource-group VideoMania-RG --name videomania-app-xxx

# View application logs
az webapp log tail --resource-group VideoMania-RG --name videomania-app-xxx
```

---

## Alternative: Deploy Using Visual Studio

### Step 1: Publish from Visual Studio

1. Open the solution in Visual Studio
2. Right-click on `ICC.AzureAppService.Demo` project
3. Click **Publish**
4. Select **Azure**
5. Select **Azure App Service (Windows)**
6. Sign in with your Azure account
7. Select your subscription and App Service
8. Click **Finish**
9. Click **Publish**

### Step 2: Monitor Publishing

- The Output window shows progress
- Wait for "Publish succeeded" message
- Your application is now live!

---

## Verify Deployment

### Step 1: Test Application URL

1. Go to your App Service in Azure Portal
2. Copy the Default domain URL
3. Open in browser: `https://videomania-app-xxx.azurewebsites.net`
4. You should see the VideoMania application

### Step 2: Test Key Features

**Homepage:**

- Verify homepage loads without errors
- Check navigation menu appears

**Videos Page:**

- Click "Videos" in navigation
- Page should load (may be empty if no videos uploaded)

**Upload Page:**

- Click "Upload" in navigation
- Select a test video file
- Enter a title
- Click "Upload"
- Should see success message

**Check Blob Storage:**

1. Go to your Storage Account in Azure Portal
2. Click **Containers** → **videos**
3. Verify uploaded video appears in the container

**Check Cosmos DB:**

1. Go to your Cosmos DB account
2. Click **Data Explorer**
3. Expand `VideoMania` → `Videos` → `Items`
4. Verify video metadata appears

### Step 3: Monitor Application Performance

In App Service, you can monitor:

1. **Metrics** (left menu):

   - CPU Time
   - Memory usage
   - HTTP requests
   - Response times

2. **Logs** (left menu):
   - Application logs
   - Web server logs
   - Failed request traces

---

## Cost Estimation

### Free Tier Benefits (12 months)

| Service      | Free Amount             |
| ------------ | ----------------------- |
| App Service  | 1 F1 instance           |
| Blob Storage | 5 GB/month              |
| Cosmos DB    | 1000 RU/s (provisioned) |

### Estimated Monthly Costs (After Free Tier)

| Service          | Pricing        | Estimated Cost   |
| ---------------- | -------------- | ---------------- |
| App Service (B1) | $12.17/month   | $12.17           |
| Blob Storage     | $0.024/GB      | $5-15            |
| Cosmos DB        | $1.25/100 RU/s | $15-50           |
| **Total**        |                | **$32-77/month** |

**Money-Saving Tips:**

- Keep using Free tier for as long as possible
- Monitor usage in Azure Portal
- Set up billing alerts
- Use B1 plan (cheapest paid tier) instead of higher tiers
- Consider spot instances for non-production workloads

---

## Troubleshooting

### Issue 1: "Invalid Base-64 string in Blob Storage"

**Cause:** Connection string is incorrect or improperly formatted

**Solution:**

1. Go to Storage Account → Access keys
2. Copy the connection string again carefully
3. Update in App Service → Configuration
4. Restart App Service

### Issue 2: "Unauthorized - Cosmos DB"

**Cause:** Connection string has incorrect account key

**Solution:**

1. Verify connection string includes full AccountKey
2. Check for copy/paste errors
3. Regenerate key if needed (Storage Account → Access keys → Regenerate)
4. Update configuration and restart

### Issue 3: "Application times out accessing Azure services"

**Cause:** Firewall rules or network connectivity issues

**Solution:**

1. Check App Service → Networking
2. Ensure resources are in same region
3. Check Storage Account → Firewalls and virtual networks
4. Set to "Allow from all networks" for development (restrict later)

### Issue 4: "Videos page shows empty list"

**Cause:** Cosmos DB container not created or videos not uploaded

**Solution:**

1. Verify containers exist in Cosmos DB Data Explorer
2. Check if any items in Videos container
3. Try uploading a test video
4. Check App Service logs for errors

### Issue 5: "Upload fails with 413 error"

**Cause:** File size exceeds limit (default 100 MB)

**Solution:**

1. Go to App Service → Configuration
2. Click **General settings**
3. Increase "Max outbound connection limit"
4. Or upload smaller video files (< 50 MB recommended)

### Issue 6: "Can't connect to Azure Portal"

**Cause:** Browser cache or JavaScript issues

**Solution:**

1. Clear browser cache and cookies
2. Try incognito/private window
3. Try different browser (Chrome, Edge, Firefox)
4. Check internet connection

---

## Security Best Practices

### Step 1: Restrict Access

1. **Blob Storage**: Set container to "Private" for production
2. **Cosmos DB**: Use firewall rules to restrict IP access
3. **App Service**: Enable SSL/TLS only (HTTPS)

### Step 2: Manage Keys and Secrets

1. Never hardcode connection strings in code
2. Use Azure Key Vault for sensitive data
3. Rotate access keys regularly
4. Restrict who can view connection strings

### Step 3: Enable Monitoring

1. Set up alerts for unusual activity
2. Review logs regularly
3. Monitor costs to detect abuse
4. Enable diagnostic logging

---

## Next Steps

### After Deployment

1. **Monitor Performance:**

   - Check metrics in App Service
   - Review logs for errors
   - Monitor storage usage and costs

2. **Scale if Needed:**

   - Increase Cosmos DB RU/s if queries are slow
   - Upgrade App Service plan if CPU usage is high
   - Add CDN for faster video delivery

3. **Enhance Security:**

   - Move connection strings to Key Vault
   - Enable authentication and authorization
   - Use managed identities instead of connection strings
   - Enable encryption at rest and in transit

4. **Set Up CI/CD:**

   - Create GitHub Actions workflow
   - Automate deployments on code push
   - Run tests before deployment

5. **Add Custom Domain:**
   - Register a domain name
   - Configure custom domain in App Service
   - Use SSL certificate (free with Azure)

---

## Useful Azure CLI Commands

```powershell
# List all resource groups
az group list --output table

# List all resources in resource group
az resource list --resource-group VideoMania-RG --output table

# Get App Service properties
az webapp show --resource-group VideoMania-RG --name videomania-app-xxx

# Get Cosmos DB endpoint
az cosmosdb show --resource-group VideoMania-RG --name videomania-cosmos-xxx

# Monitor App Service logs
az webapp log tail --resource-group VideoMania-RG --name videomania-app-xxx

# Restart App Service
az webapp restart --resource-group VideoMania-RG --name videomania-app-xxx

# Delete all resources (careful!)
az group delete --resource-group VideoMania-RG --yes --no-wait
```

---

## Support and Resources

- **Azure Documentation:** https://docs.microsoft.com/azure/
- **Azure CLI Reference:** https://docs.microsoft.com/cli/azure/
- **Azure Portal:** https://portal.azure.com
- **Azure Pricing Calculator:** https://azure.microsoft.com/en-us/pricing/calculator/
- **Azure Support:** https://azure.microsoft.com/en-us/support/

---

**Last Updated:** November 29, 2025
**Application:** VideoMania - Serverless Video Streaming Platform
**Status:** Ready for Production Deployment
