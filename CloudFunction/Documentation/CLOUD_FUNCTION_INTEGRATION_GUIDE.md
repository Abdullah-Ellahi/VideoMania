# Cloud Function Integration Guide

## Overview

Your Azure Cloud Function is now fully integrated with your web app. Here's how everything works together:

---

## **Architecture Flow**

```
User Upload (Web App)
    ↓
Video stored in Blob Storage ("videos" container)
    ↓
Metadata saved to Cosmos DB ("Videos" container)
    ↓
Cloud Function Triggered (Blob Trigger)
    ↓
Process Video (Thumbnail + Resize)
    ↓
Store Results in Blob ("thumbnails" & "processed-videos" containers)
    ↓
Update Cosmos DB with processing metadata
    ↓
Web App reads updated data to display processed content
```

---

## **File Structure**

```
/VideoMania
├── Program.cs                                    # Registers all services
├── VideoTrigger.cs                              # Main cloud function
├── local.settings.json                          # Local development config
├── VM.csproj                                    # Project dependencies
├── Services/
│   ├── VideoProcessingService.cs                # FFmpeg operations
│   ├── CosmosDbService.cs                       # Database operations (NEW)
│   └── BlobStorageServiceForFunctions.cs        # Blob operations (NEW)
└── host.json                                    # Azure Functions config
```

---

## **Setup Instructions**

### **1. Update `local.settings.json`**

The file has been created at the root of your VideoMania project. Update it with your actual connection strings:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "YOUR_STORAGE_ACCOUNT_CONNECTION_STRING",
    "BLOB_STORAGE_CONNECTION_STRING": "YOUR_STORAGE_ACCOUNT_CONNECTION_STRING",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "COSMOS_DB_CONNECTION_STRING": "YOUR_COSMOS_DB_CONNECTION_STRING"
  }
}
```

**Where to find these:**

1. **Storage Account Connection String:**
   - Azure Portal → Storage Accounts → `videomaniadev98e1`
   - Settings → Access Keys → Copy Connection String

2. **Cosmos DB Connection String:**
   - Azure Portal → Azure Cosmos DB → Your account
   - Settings → Keys → Copy Primary Connection String

### **2. Create Required Blob Containers**

In your Azure Storage Account, ensure these containers exist:

| Container | Purpose | Created By |
|-----------|---------|-----------|
| `videos` | Original uploaded videos | Web App |
| `thumbnails` | Generated thumbnails | Cloud Function |
| `processed-videos` | Resized videos (1280x720) | Cloud Function |

**How to create:**
1. Azure Portal → Storage Account → Containers
2. Click "+ Container"
3. Enter name, set Access level to "Private"
4. Repeat for all three containers

### **3. Add Cosmos DB NuGet Package (if not present)**

The cloud function now uses Cosmos DB. Ensure the project has this package:

```bash
cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania
dotnet add package Azure.Cosmos
```

---

## **How the Integration Works**

### **Phase 1: Web App Upload**

```csharp
// User uploads video through web app
POST /api/getuploadSas

// Web app creates:
1. Blob in "videos" container with name: {Guid}{extension}
2. Cosmos DB document in "Videos" container:
   {
     "id": "video-uuid",
     "url": "123e4567-e89b-12d3-a456-426614174000.mp4",  // blob name
     "title": "My Video",
     "description": "...",
     "uploadedAt": "2025-12-03T10:30:00Z"
   }
```

### **Phase 2: Cloud Function Triggers**

```
1. Blob Trigger fires when video appears in "videos" container
2. Cloud function receives Stream + blob name
3. Looks up video ID in Cosmos DB using blob name
4. Processes video (thumbnail + resize)
5. Uploads results to blob storage
6. Updates Cosmos DB with processing results
```

### **Phase 3: Updated Cosmos DB Document**

After processing, the Cosmos DB document looks like:

```json
{
  "id": "video-uuid",
  "url": "123e4567-e89b-12d3-a456-426614174000.mp4",
  "title": "My Video",
  "uploadedAt": "2025-12-03T10:30:00Z",
  "processing": {
    "processed": true,
    "processedAt": "2025-12-03T10:35:00Z",
    "status": "completed",
    "thumbnailUrl": "https://storage.blob.core.windows.net/thumbnails/123e4567_thumbnail.jpg",
    "resizedVideoUrl": "https://storage.blob.core.windows.net/processed-videos/123e4567_1280x720.mp4",
    "metadata": {
      "duration": 45.5,
      "width": 1920,
      "height": 1080,
      "videoCodec": "h264",
      "audioCodec": "aac",
      "frameRate": 30,
      "bitRate": 5000000
    }
  }
}
```

### **Phase 4: Web App Displays Results**

Update your web app to read the `processing` field and display:
- Thumbnail image
- Resized video option
- Video metadata

---

## **How to Test Locally**

### **Step 1: Start the Cloud Function Locally**

```bash
cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania

# Install Azure Functions Core Tools if needed
brew tap azure/formulae
brew install azure-functions-core-tools@4

# Run the function
func start
```

You should see:
```
Functions runtime version: 4.x
...
Listening on http://localhost:7071
```

### **Step 2: Use Storage Emulator (Optional)**

For true local testing without Azure:

```bash
# Install Azure Storage Emulator (macOS: Use Docker alternative)
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

Then update `local.settings.json`:
```json
"AzureWebJobsStorage": "UseDevelopmentStorage=true"
```

### **Step 3: Upload a Test Video**

Using your web app:
1. Upload a video through the web UI
2. Monitor function logs:
   ```bash
   # In separate terminal while func start is running
   func azure storage blob list --connection-string "YOUR_CONNECTION_STRING" --container-name videos
   ```

### **Step 4: Check Results**

```bash
# List processed files
func azure storage blob list --connection-string "YOUR_CONNECTION_STRING" --container-name thumbnails
func azure storage blob list --connection-string "YOUR_CONNECTION_STRING" --container-name processed-videos

# Query Cosmos DB in Azure Portal
SELECT c.id, c.processing FROM c WHERE c.processing.processed = true
```

---

## **Deployment to Azure**

### **Step 1: Create Azure Function App (if not exists)**

```bash
# Install Azure CLI
brew install azure-cli

# Login
az login

# Create Function App
az functionapp create \
  --resource-group <your-resource-group> \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 8.0 \
  --functions-version 4 \
  --name VideoProcessingFunction \
  --storage-account videomaniadev98e1
```

### **Step 2: Deploy Function Code**

```bash
cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania

# Build
dotnet build --configuration Release

# Publish to Azure
func azure functionapp publish VideoProcessingFunction
```

### **Step 3: Configure App Settings in Azure**

```bash
# Set connection strings in Azure Function App
az functionapp config appsettings set \
  --name VideoProcessingFunction \
  --resource-group <your-resource-group> \
  --settings \
    BLOB_STORAGE_CONNECTION_STRING="YOUR_STORAGE_CONNECTION_STRING" \
    COSMOS_DB_CONNECTION_STRING="YOUR_COSMOS_DB_CONNECTION_STRING"
```

### **Step 4: Monitor Execution**

In Azure Portal:
1. Function App → Functions → VideoTrigger
2. Monitor tab to see invocations and logs
3. Application Insights for detailed telemetry

---

## **Troubleshooting**

### **Problem: "Could not find video ID for blob"**

**Cause:** Cloud function can't find the video in Cosmos DB

**Solution:**
1. Verify video was created in Cosmos DB when uploaded
2. Check blob name matches the `url` field in Cosmos DB exactly
3. Enable detailed logging in VideoTrigger.cs

```csharp
_logger.LogInformation("Searching for blob: {fileName}", fileName);
_logger.LogInformation("Found video ID: {videoId}", videoId ?? "NOT FOUND");
```

### **Problem: "Failed to download FFmpeg binaries"**

**Cause:** Temp directory permissions or network issue

**Solution:**
- Local: Clear temp directory: `rm -rf /tmp/VideoProcessing`
- Azure: Increase function timeout in host.json:

```json
{
  "version": "2.0",
  "functionTimeout": "01:00:00",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true
      }
    }
  }
}
```

### **Problem: Thumbnails/resized videos not appearing in blob storage**

**Cause:** Upload permissions or container doesn't exist

**Solution:**
1. Verify `BlobStorageServiceForFunctions` has credentials
2. Confirm containers exist:
   ```bash
   az storage container list --account-name videomaniadev98e1
   ```
3. Check function logs for actual error

---

## **Web App Integration Example**

Update your web app's video display page to show processed results:

```csharp
// In your VideoController.cs

public async Task<IActionResult> GetVideoDetails(string id)
{
    var cosmosService = new CosmosDbService(connectionString, logger);
    var container = await cosmosService.GetContainerAsync("Videos");
    
    var response = await container.ReadItemAsync<dynamic>(id, new PartitionKey(id));
    var video = response.Resource;
    
    // Check if processed
    if (video.processing?.processed == true)
    {
        var thumbnailUrl = video.processing.thumbnailUrl;
        var resizedVideoUrl = video.processing.resizedVideoUrl;
        var metadata = video.processing.metadata;
        
        // Display these in your view
    }
    
    return Ok(video);
}
```

---

## **Key Differences from Web App Blob Usage**

| Aspect | Web App | Cloud Function |
|--------|---------|-----------------|
| **Upload** | Server-side via UploadBlobAsync | Stream from trigger |
| **Container** | `videos` | Reads `videos`, writes `thumbnails` & `processed-videos` |
| **Storage Service** | BlobStorageService | BlobStorageServiceForFunctions |
| **Metadata** | Stored immediately | Updated after processing |
| **Database** | CosmosDbService | CosmosDbService (same) |

---

## **Next Steps**

1. **Update `local.settings.json`** with your connection strings
2. **Create blob containers** in Azure Storage
3. **Test locally** with `func start`
4. **Deploy to Azure** when ready
5. **Update web app** to display processed results
6. (Optional) **Add Event Grid** for notifications between services

