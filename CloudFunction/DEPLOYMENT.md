# CloudFunction Deployment Guide

Quick reference for deploying the cloud function.

---

## Option 1: Deploy from Command Line

```bash
cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania/CloudFunction

# 1. Login to Azure
az login

# 2. Build Release
dotnet build --configuration Release

# 3. Publish to Function App
func azure functionapp publish VideoProcessingFunction

# 4. Verify deployment
az functionapp show --name VideoProcessingFunction --resource-group <resource-group>
```

---

## Option 2: Deploy via Visual Studio Code

1. Install Azure Functions extension
2. Open CloudFunction folder
3. Right-click on CloudFunction.csproj → Deploy to Function App
4. Select subscription and function app
5. Click Deploy

---

## Option 3: Deploy via Azure CLI (Full Setup)

```bash
# Set variables
RESOURCE_GROUP="your-resource-group"
FUNCTION_APP="VideoProcessingFunction"
STORAGE_ACCOUNT="videomaniadev98e1"
REGION="eastus"

# 1. Create Function App
az functionapp create \
  --resource-group $RESOURCE_GROUP \
  --consumption-plan-location $REGION \
  --runtime dotnet-isolated \
  --runtime-version 8.0 \
  --functions-version 4 \
  --name $FUNCTION_APP \
  --storage-account $STORAGE_ACCOUNT

# 2. Set App Settings
az functionapp config appsettings set \
  --name $FUNCTION_APP \
  --resource-group $RESOURCE_GROUP \
  --settings \
    BLOB_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=videomaniadev98e1;AccountKey=YOUR_KEY;..." \
    COSMOS_DB_CONNECTION_STRING="AccountEndpoint=https://YOUR_ACCOUNT.documents.azure.com:443/;AccountKey=YOUR_KEY;..."

# 3. Deploy Code
cd CloudFunction
dotnet build --configuration Release
func azure functionapp publish $FUNCTION_APP
```

---

## Pre-Deployment Checklist

- [ ] Updated `local.settings.json` with connection strings
- [ ] Created blob containers: `thumbnails`, `processed-videos`
- [ ] Cosmos DB database `videomania` exists
- [ ] Cosmos DB container `Videos` exists
- [ ] Azure Function App created (or will be auto-created)
- [ ] Storage account `videomaniadev98e1` exists
- [ ] Have Azure CLI installed: `az --version`
- [ ] Have Azure Functions Core Tools: `func --version`

---

## Post-Deployment Verification

```bash
# 1. Check function app exists
az functionapp show --name VideoProcessingFunction --resource-group <group>

# 2. Check function is deployed
func list

# 3. Check app settings
az functionapp config appsettings list --name VideoProcessingFunction --resource-group <group>

# 4. Test by uploading video
# Upload video via web app to blob storage and monitor logs

# 5. View logs
func azure functionapp logstream VideoProcessingFunction

# 6. Check Application Insights
# Azure Portal → Function App → Application Insights
```

---

## Rollback

If something goes wrong, you can rollback the deployment:

```bash
# View deployment history
az webapp deployment list --name VideoProcessingFunction --resource-group <group>

# Rollback to previous version
az webapp deployment slot swap \
  --name VideoProcessingFunction \
  --resource-group <group> \
  --slot staging
```

---

## Monitoring After Deployment

### Real-time Logs

```bash
func azure functionapp logstream VideoProcessingFunction
```

### Azure Portal

1. Go to Function App → VideoProcessingFunction
2. Click "Monitor" under VideoTrigger
3. See invocations, duration, and success rate
4. Click "Application Insights" for detailed logs

### Metrics to Watch

- **Invocation count** - How many times function ran
- **Execution time** - How long each video takes to process
- **Failures** - Any errors during processing
- **Duration** - P95 and P99 percentiles
- **Throughput** - Videos processed per hour

---

## Common Deployment Issues

| Issue | Solution |
|-------|----------|
| "Function app doesn't exist" | Create it first: `az functionapp create ...` |
| "Connection string not found" | Run: `az functionapp config appsettings set ...` |
| "Unauthorized" | Check Azure credentials: `az login` |
| "Deployment failed" | Check error message and try again |
| "FFmpeg download fails" | Normal on first run, takes ~1-2 minutes |

---

## Update Deployment

To update the function code after initial deployment:

```bash
cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania/CloudFunction

# Make code changes...

# Build and redeploy
dotnet build --configuration Release
func azure functionapp publish VideoProcessingFunction
```

Changes take effect within a few seconds.

---

## Environment Variables Reference

| Variable | Where to Set | How to Get |
|----------|--------------|-----------|
| `BLOB_STORAGE_CONNECTION_STRING` | App Settings | Portal → Storage Account → Access Keys |
| `COSMOS_DB_CONNECTION_STRING` | App Settings | Portal → Cosmos DB → Keys |
| `AzureWebJobsStorage` | Auto-set | Uses storage account from function app |
| `FUNCTIONS_WORKER_RUNTIME` | host.json | Set to `dotnet-isolated` |

---

## Testing Deployment

After deployment, upload a test video:

1. Use your web app to upload a video
2. Watch logs: `func azure functionapp logstream VideoProcessingFunction`
3. Check blob storage for thumbnail and resized video
4. Check Cosmos DB for processing field in document

If everything works, you're ready for production!

