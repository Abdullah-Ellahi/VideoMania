# Cloud Function Setup Checklist

Complete these steps in order to get your cloud function fully operational.

---

## **Phase 1: Local Development Setup** ✓

- [ ] **Read** `CLOUD_FUNCTION_INTEGRATION_GUIDE.md` for full context
- [ ] **Read** `HOW_WEBAPP_GETS_RESULTS.md` to understand the flow

### **Update Configuration**

- [ ] **Update `local.settings.json`** with your Azure credentials:
  - [ ] Get **Storage Connection String** from Azure Portal → Storage Account → Access Keys
  - [ ] Get **Cosmos DB Connection String** from Azure Portal → Cosmos DB → Keys
  - [ ] Update both values in `local.settings.json`

### **Create Azure Resources**

- [ ] **Create Blob Containers** (Azure Portal → Storage Account → Containers):
  - [ ] `videos` (for original uploads)
  - [ ] `thumbnails` (for generated thumbnails)
  - [ ] `processed-videos` (for resized videos)

### **Install Dependencies**

- [ ] **Install Azure Functions Core Tools:**
  ```bash
  brew tap azure/formulae
  brew install azure-functions-core-tools@4
  ```

- [ ] **Add NuGet Package** (if not present):
  ```bash
  cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania
  dotnet add package Azure.Cosmos
  ```

- [ ] **Build Project:**
  ```bash
  dotnet build
  ```

### **Test Locally**

- [ ] **Start cloud function:**
  ```bash
  func start
  ```
  Should see: `Listening on http://localhost:7071`

- [ ] **Upload test video** through your web app

- [ ] **Monitor logs** in the terminal running `func start`

- [ ] **Verify in Azure Portal:**
  - [ ] Blob appears in `videos` container
  - [ ] Cosmos DB has video document
  - [ ] Thumbnail appears in `thumbnails` container
  - [ ] Resized video appears in `processed-videos` container
  - [ ] Cosmos DB document updated with `processing` field

---

## **Phase 2: Azure Deployment** ✓

### **Create Function App** (if not exists)

- [ ] **Install Azure CLI:**
  ```bash
  brew install azure-cli
  ```

- [ ] **Login to Azure:**
  ```bash
  az login
  ```

- [ ] **Create Function App:**
  ```bash
  az functionapp create \
    --resource-group <your-resource-group> \
    --consumption-plan-location eastus \
    --runtime dotnet-isolated \
    --runtime-version 8.0 \
    --functions-version 4 \
    --name VideoProcessingFunction \
    --storage-account videomaniadev98e1
  ```

### **Deploy Code**

- [ ] **Build Release:**
  ```bash
  dotnet build --configuration Release
  ```

- [ ] **Deploy to Azure:**
  ```bash
  func azure functionapp publish VideoProcessingFunction
  ```

### **Configure Azure Settings**

- [ ] **Set app settings in Azure Function App:**
  ```bash
  az functionapp config appsettings set \
    --name VideoProcessingFunction \
    --resource-group <your-resource-group> \
    --settings \
      BLOB_STORAGE_CONNECTION_STRING="YOUR_STORAGE_CONNECTION_STRING" \
      COSMOS_DB_CONNECTION_STRING="YOUR_COSMOS_DB_CONNECTION_STRING"
  ```

### **Test in Azure**

- [ ] **Upload video** through web app
- [ ] **Monitor in Azure Portal:**
  - [ ] Function App → Functions → VideoTrigger → Monitor
  - [ ] Check for successful invocations
  - [ ] Review logs in Application Insights
- [ ] **Verify blob storage:**
  - [ ] Thumbnail in `thumbnails` container
  - [ ] Resized video in `processed-videos` container
- [ ] **Verify Cosmos DB:**
  - [ ] Document has `processing` field
  - [ ] `processed = true`
  - [ ] URLs are populated

---

## **Phase 3: Web App Integration** ✓

### **Display Results in Web App**

- [ ] **Add endpoint in VideoController:**
  ```csharp
  [HttpGet("video/{id}/processing-status")]
  public async Task<IActionResult> GetProcessingStatus(string id)
  {
      // See HOW_WEBAPP_GETS_RESULTS.md for code
  }
  ```

- [ ] **Update video details page** to show:
  - [ ] Thumbnail image
  - [ ] Resized video player
  - [ ] Video metadata (duration, resolution, codec, etc.)
  - [ ] "Processing..." status if not yet complete

- [ ] **Optional: Add auto-refresh** while processing
  - [ ] Poll every 2 seconds until `processing.processed = true`
  - [ ] Auto-reload page when complete

### **Test End-to-End**

- [ ] **Upload video** from web app
- [ ] **View video details page** - shows "Processing..."
- [ ] **Wait for processing** (30 seconds to few minutes)
- [ ] **Refresh page** - shows thumbnail and metadata
- [ ] **Play resized video** - works correctly
- [ ] **Download links** - both thumbnail and resized video work

---

## **Phase 4: Production Readiness** 

### **Monitoring & Logging**

- [ ] **Review Application Insights:**
  - [ ] Function App → Application Insights
  - [ ] Check for errors in last 24 hours
  - [ ] Review performance metrics

- [ ] **Set up alerts** (Optional):
  - [ ] Alert if function execution fails
  - [ ] Alert if execution time > 5 minutes
  - [ ] Alert on storage quota warnings

### **Optimization** (Optional)

- [ ] **Adjust timeout** in `host.json` if processing large videos:
  ```json
  "functionTimeout": "01:00:00"
  ```

- [ ] **Consider adjusting** resize resolution if 1280x720 is too high/low

- [ ] **Add error handling** for edge cases:
  - [ ] Very large videos (100+ MB)
  - [ ] Corrupted video files
  - [ ] Unsupported formats

### **Documentation**

- [ ] **Update README** with cloud function info
- [ ] **Document any customizations** you made
- [ ] **Keep local.settings.json** secure (don't commit to GitHub)

---

## **Troubleshooting Guide**

| Issue | Check | Solution |
|-------|-------|----------|
| Cloud function doesn't start | `func start` output | Check error message, missing NuGet package, or settings |
| Blob trigger doesn't fire | Check `videos` container | Ensure container exists and blob is uploaded there |
| "Video ID not found" error | Cosmos DB query | Verify video document was created when uploaded |
| FFmpeg download fails | Network/Temp folder | Check internet connection, clear `/tmp/VideoProcessing` |
| Processed files don't appear | Check container names | Verify `thumbnails` and `processed-videos` containers exist |
| Processing takes too long | `host.json` timeout | Increase `functionTimeout` for large videos |
| Web app doesn't show results | Check Cosmos DB | Verify document has `processing` field after processing |

---

## **Quick Reference: Connection Strings**

### **Storage Account**

Go to: Azure Portal → Storage Accounts → videomaniadev98e1 → Access Keys

```
DefaultEndpointsProtocol=https;AccountName=videomaniadev98e1;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net
```

### **Cosmos DB**

Go to: Azure Portal → Azure Cosmos DB → Your Account → Keys

```
AccountEndpoint=https://YOUR_ACCOUNT.documents.azure.com:443/;AccountKey=YOUR_KEY;
```

---

## **Files Modified/Created**

| File | Status | Description |
|------|--------|-------------|
| `local.settings.json` | Created | Local development configuration |
| `Program.cs` | Modified | Registers Cosmos DB and Blob services |
| `VideoTrigger.cs` | Modified | Uses services to process and save results |
| `Services/CosmosDbService.cs` | Created | Database operations for cloud function |
| `Services/BlobStorageServiceForFunctions.cs` | Created | Blob storage operations for cloud function |
| `CLOUD_FUNCTION_INTEGRATION_GUIDE.md` | Created | Full integration documentation |
| `HOW_WEBAPP_GETS_RESULTS.md` | Created | Web app integration guide |
| `CLOUD_FUNCTION_SETUP_CHECKLIST.md` | Created | This file |

---

## **Next Steps After Setup**

1. Complete all checkboxes in **Phase 1 & 2**
2. Test end-to-end in **Phase 3**
3. Monitor in **Phase 4**
4. Optionally customize processing logic in `VideoTrigger.cs`
5. Optionally add more features (filters, multiple resolutions, etc.)

---

## **Need Help?**

- **Check logs:** Azure Portal → Function App → Monitor
- **Check errors:** Azure Portal → Function App → Application Insights
- **Local testing:** Run `func start` and watch output
- **Read docs:** See `CLOUD_FUNCTION_INTEGRATION_GUIDE.md` for detailed explanations

