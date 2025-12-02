# Cloud Function Quick Reference Card

## **1-Minute Setup**

```bash
# Update config
nano /Users/asani.io/Documents/abdullah_ellahi/VideoMania/local.settings.json
# ↳ Paste your Storage & Cosmos DB connection strings

# Test locally
cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania
func start

# Deploy
func azure functionapp publish VideoProcessingFunction
```

---

## **Connection Strings**

| Service | Where to Find | Format |
|---------|---------------|--------|
| **Storage** | Portal → Storage Account → Access Keys | `DefaultEndpointsProtocol=https;...` |
| **Cosmos DB** | Portal → Cosmos DB → Keys | `AccountEndpoint=https://...;...` |

---

## **Blob Containers**

Must exist in Azure Storage:

```
videos/              ← Original uploads (Web App creates)
thumbnails/          ← Generated thumbnails (Cloud Function creates)
processed-videos/    ← Resized videos (Cloud Function creates)
```

---

## **The Flow**

```
Upload video
  ↓ (Blob Trigger fires)
Cloud Function processes
  ↓ (Automatic)
Cosmos DB updated
  ↓
Web App shows results
```

---

## **Key Files**

| File | What It Does |
|------|--------------|
| `VideoTrigger.cs` | Main cloud function |
| `CosmosDbService.cs` | Database queries/updates |
| `BlobStorageServiceForFunctions.cs` | Blob storage operations |
| `Program.cs` | Service registration |
| `local.settings.json` | Configuration |

---

## **Get Results in Web App**

```csharp
// Query Cosmos DB
var response = await container.ReadItemAsync<dynamic>(videoId, new PartitionKey(videoId));
var video = response.Resource;

// Check if processed
if (video.processing?.processed == true)
{
    var thumb = video.processing.thumbnailUrl;
    var resized = video.processing.resizedVideoUrl;
    var meta = video.processing.metadata;
}
```

---

## **Processing Output**

```json
{
  "processing": {
    "processed": true,
    "thumbnailUrl": "https://storage.../uuid_thumbnail.jpg",
    "resizedVideoUrl": "https://storage.../uuid_1280x720.mp4",
    "metadata": {
      "duration": 45.5,
      "width": 1920,
      "height": 1080,
      "videoCodec": "h264"
    }
  }
}
```

---

## **Local Testing Checklist**

- [ ] Update `local.settings.json`
- [ ] Create blob containers
- [ ] Run `func start`
- [ ] Upload video via web app
- [ ] Check function logs
- [ ] Verify blob storage (3 containers)
- [ ] Check Cosmos DB document

---

## **Troubleshooting**

| Error | Fix |
|-------|-----|
| "Video ID not found" | Verify video saved to Cosmos DB |
| "Connection string error" | Check `local.settings.json` format |
| "Container doesn't exist" | Create containers in Azure Portal |
| "FFmpeg download failed" | Check internet, clear `/tmp/VideoProcessing` |
| "Processing takes too long" | Large file? Increase `functionTimeout` in `host.json` |

---

## **What Gets Updated in Cosmos DB**

**BEFORE:**
```json
{ "id": "uuid", "url": "uuid.mp4", "title": "...", "uploadedAt": "..." }
```

**AFTER:**
```json
{
  "id": "uuid", 
  "url": "uuid.mp4", 
  "title": "...",
  "uploadedAt": "...",
  "processing": { "processed": true, "thumbnailUrl": "...", ... }
}
```

---

## **Service Dependencies**

```
VideoTrigger
  ├─ VideoProcessingService (FFmpeg)
  ├─ CosmosDbService (Database)
  └─ BlobStorageServiceForFunctions (Blob Storage)
```

---

## **Environment Variables**

```json
{
  "BLOB_STORAGE_CONNECTION_STRING": "Your storage connection string",
  "COSMOS_DB_CONNECTION_STRING": "Your Cosmos DB connection string",
  "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
}
```

---

## **Deploy Command**

```bash
# Publish to Azure Function App
func azure functionapp publish VideoProcessingFunction
```

---

## **Monitor Execution**

```
Azure Portal
  → Function Apps
  → VideoProcessingFunction
  → VideoTrigger
  → Monitor (see invocations and logs)
```

---

## **Key Differences: Web App vs Cloud Function**

| Aspect | Web App | Cloud Function |
|--------|---------|-----------------|
| Triggered | HTTP request | Blob upload |
| Duration | < 1 sec | 10-30 sec |
| Runs | Always | On-demand |
| Cost | Fixed | Per execution |

---

## **Storage Paths**

```
Input:  videomaniadev98e1 → videos → uuid.mp4
↓
Processing (Cloud Function)
↓
Output: videomaniadev98e1 → thumbnails → uuid_thumbnail.jpg
Output: videomaniadev98e1 → processed-videos → uuid_1280x720.mp4
```

---

## **Timeline**

```
1. User uploads         0:00
2. Blob trigger fires   0:01
3. Processing starts    0:02
4. Thumbnail done       0:15
5. Video resized        0:20
6. DB updated           0:25
7. Results visible      0:26
```

---

## **Common Customizations**

Edit `VideoTrigger.cs` to change:

```csharp
// Thumbnail timing (default 1 second)
secondsIntoVideo: 5  // Change to 5 seconds

// Video resolution (default 1280x720)
width: 1920, height: 1080  // Change dimensions

// Add more processing steps
// - Bitrate reduction
// - Format conversion
// - Watermarking
```

---

## **Documentation Files**

| File | Purpose |
|------|---------|
| `INTEGRATION_SUMMARY.md` | Complete summary (start here) |
| `CLOUD_FUNCTION_SETUP_CHECKLIST.md` | Step-by-step setup |
| `CLOUD_FUNCTION_INTEGRATION_GUIDE.md` | Detailed technical guide |
| `ARCHITECTURE_DIAGRAM.md` | Visual diagrams |
| `HOW_WEBAPP_GETS_RESULTS.md` | Web app code examples |

---

## **Useful Azure CLI Commands**

```bash
# List function apps
az functionapp list --resource-group <group>

# View logs
az webapp log tail --name VideoProcessingFunction --resource-group <group>

# Set app settings
az functionapp config appsettings set \
  --name VideoProcessingFunction \
  --resource-group <group> \
  --settings KEY=VALUE

# Check deployment
az functionapp deployment show --name VideoProcessingFunction --resource-group <group>
```

---

## **Remember**

✅ Same storage account as web app (`videomaniadev98e1`)
✅ Same database (`videomania`)
✅ Automatic trigger (no extra code in web app)
✅ Results stored in Cosmos DB (read by web app)
✅ Serverless (pay per execution)

---

## **Need Help?**

1. Check `CLOUD_FUNCTION_SETUP_CHECKLIST.md` for setup issues
2. Read `CLOUD_FUNCTION_INTEGRATION_GUIDE.md` for detailed explanations
3. See `HOW_WEBAPP_GETS_RESULTS.md` for web app integration
4. Review logs in Azure Portal

