# Azure Cloud Function - Video Processing Service

This is a standalone Azure Functions project for automatic video processing. It's completely separate from the web app and can be deployed independently.

## Project Structure

```
CloudFunction/
├── Program.cs                                    # Application startup & DI
├── VideoTrigger.cs                              # Main cloud function
├── CloudFunction.csproj                         # Project file
├── host.json                                    # Azure Functions config
├── local.settings.json                          # Local development settings
├── Services/
│   ├── VideoProcessingService.cs                # FFmpeg operations
│   ├── CosmosDbService.cs                       # Cosmos DB operations
│   └── BlobStorageServiceForFunctions.cs        # Blob storage operations
└── Properties/
    └── (reserved for future use)
```

---

## What This Function Does

**Trigger:** When a video is uploaded to Azure Blob Storage (`videos` container)

**Processing:**
1. Generates a thumbnail at 1 second into the video
2. Resizes video to 1280x720 resolution
3. Extracts video metadata (duration, codec, resolution, etc.)
4. Uploads processed files to blob storage
5. Updates Cosmos DB with processing results

**Time:** ~10-30 seconds per video (depends on file size)

---

## Quick Start

### 1. Configure Local Settings

Edit `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "YOUR_STORAGE_CONNECTION_STRING",
    "BLOB_STORAGE_CONNECTION_STRING": "YOUR_STORAGE_CONNECTION_STRING",
    "COSMOS_DB_CONNECTION_STRING": "YOUR_COSMOS_DB_CONNECTION_STRING",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

**Get these values from:**
- **Storage Account:** Azure Portal → Storage Accounts → Access Keys
- **Cosmos DB:** Azure Portal → Cosmos DB → Keys

### 2. Create Blob Containers

In Azure Storage Account, ensure these containers exist:
- `videos` (input - created by web app)
- `thumbnails` (output)
- `processed-videos` (output)

### 3. Test Locally

```bash
cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania/CloudFunction

# Restore packages
dotnet restore

# Build project
dotnet build

# Run the function
func start
```

You should see:
```
Functions runtime version: 4.x
...
Listening on http://localhost:7071
```

### 4. Deploy to Azure

```bash
# Build for release
dotnet build --configuration Release

# Create Function App (if not exists)
az functionapp create \
  --resource-group <your-resource-group> \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 8.0 \
  --functions-version 4 \
  --name VideoProcessingFunction \
  --storage-account videomaniadev98e1

# Deploy
func azure functionapp publish VideoProcessingFunction

# Set app settings in Azure
az functionapp config appsettings set \
  --name VideoProcessingFunction \
  --resource-group <your-resource-group> \
  --settings \
    BLOB_STORAGE_CONNECTION_STRING="YOUR_CONNECTION_STRING" \
    COSMOS_DB_CONNECTION_STRING="YOUR_CONNECTION_STRING"
```

---

## Configuration

### Environment Variables

| Variable | Purpose | Where to Set |
|----------|---------|--------------|
| `BLOB_STORAGE_CONNECTION_STRING` | Azure Storage connection | local.settings.json / App Settings |
| `COSMOS_DB_CONNECTION_STRING` | Cosmos DB connection | local.settings.json / App Settings |
| `AzureWebJobsStorage` | Function runtime storage | local.settings.json / App Settings |
| `FUNCTIONS_WORKER_RUNTIME` | Runtime type | local.settings.json |

### Customization Options

Edit `VideoTrigger.cs` to customize:

```csharp
// Change thumbnail time (currently 1 second)
secondsIntoVideo: 5  // Extract frame at 5 seconds

// Change video resolution (currently 1280x720)
width: 1920, height: 1080  // Different resolution

// Change container names
"thumbnails"         // Output container for thumbnails
"processed-videos"   // Output container for resized videos
```

---

## Monitoring

### Local Development

Run with `func start` and watch the console output for logs.

### Azure Deployment

Monitor in Azure Portal:
1. Go to Function App → VideoProcessingFunction
2. Click "Functions" → "VideoTrigger"
3. Click "Monitor" tab
4. See invocations, execution times, and errors
5. Click "Application Insights" for detailed logs

---

## Data Flow

```
Web App Upload
    ↓
Blob Storage (videos) + Cosmos DB
    ↓ (Blob Trigger)
Cloud Function
    ↓ (Processes Video)
Generate Thumbnail → Upload to (thumbnails) container
Resize Video → Upload to (processed-videos) container
Extract Metadata → Update Cosmos DB
    ↓
Web App reads Cosmos DB
    ↓
Display Results
```

---

## Database Schema

### Cosmos DB Document (Before Processing)

```json
{
  "id": "uuid",
  "url": "uuid.mp4",
  "title": "My Video",
  "uploadedAt": "2025-12-03T10:30:00Z"
}
```

### Cosmos DB Document (After Processing)

```json
{
  "id": "uuid",
  "url": "uuid.mp4",
  "title": "My Video",
  "uploadedAt": "2025-12-03T10:30:00Z",
  "processing": {
    "processed": true,
    "processedAt": "2025-12-03T10:35:45Z",
    "status": "completed",
    "thumbnailUrl": "https://storage.../uuid_thumbnail.jpg",
    "resizedVideoUrl": "https://storage.../uuid_1280x720.mp4",
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

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| "Connection string not found" | Missing env vars | Update local.settings.json |
| "Container doesn't exist" | Missing blob containers | Create in Azure Portal |
| "Video ID not found" | Video not in Cosmos DB | Verify video was created when uploaded |
| "FFmpeg download failed" | Network/permissions | Check internet, clear /tmp/VideoProcessing |
| Function timeout | Large video file | Increase `functionTimeout` in host.json |
| "Could not find matching trigger" | Blob not in "videos" container | Upload to correct container |

---

## Performance

| Metric | Value | Notes |
|--------|-------|-------|
| Cold start | 5-10s | Initial function invocation |
| Thumbnail generation | ~5-10s | Depends on video size |
| Video resizing | ~10-20s | Depends on video size & resolution |
| Metadata extraction | <1s | Quick operation |
| Blob upload | 1-5s | Depends on file size |
| Database update | <500ms | Cosmos DB operation |
| **Total per video** | 10-30s | Typical for 100MB video |

---

## Cost Estimation (100 videos/month)

- **Function Execution:** $0.50 - $1.50 (consumption-based)
- **Blob Storage:** $0.10 - $1.00 (storage)
- **Cosmos DB:** $1.00 - $10.00 (database)
- **Total:** ~$2-12/month

---

## Dependencies

See `CloudFunction.csproj`:

```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.50.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs" Version="6.7.0" />
<PackageReference Include="Azure.Cosmos" Version="4.0.0" />
<PackageReference Include="Xabe.FFmpeg" Version="6.0.2" />
<PackageReference Include="Xabe.FFmpeg.Downloader" Version="6.0.2" />
```

---

## Integration with Web App

The web app doesn't need any changes to trigger the cloud function. It automatically:

1. Uploads video to blob storage
2. Saves metadata to Cosmos DB
3. Cloud function triggers automatically
4. Results appear in same Cosmos DB document
5. Web app reads updated document to display results

See `/HOW_WEBAPP_GETS_RESULTS.md` in root folder for code examples.

---

## Useful Commands

```bash
# Install Azure Functions Core Tools
brew tap azure/formulae
brew install azure-functions-core-tools@4

# Restore packages
dotnet restore

# Build
dotnet build

# Run locally
func start

# Deploy
func azure functionapp publish VideoProcessingFunction

# View logs
func azure functionapp logstream VideoProcessingFunction

# List functions
func list

# Run specific function
func run VideoTrigger
```

---

## Security

- ✅ Connection strings in `local.settings.json` (not committed to GitHub)
- ✅ Azure RBAC controls resource access
- ✅ Blob containers are private
- ✅ Temp files cleaned up after processing
- ⚠️ Add authentication to REST endpoints (if needed)
- ⚠️ Consider virus scanning for uploads
- ⚠️ Use Azure Key Vault for production secrets

---

## Next Steps

1. Update `local.settings.json` with your credentials
2. Create required blob containers
3. Test locally: `func start`
4. Deploy to Azure: `func azure functionapp publish VideoProcessingFunction`
5. Update web app to display processing results
6. Monitor with Application Insights

---

## Support

- Check logs: `func start` output or Azure Portal → Application Insights
- Troubleshooting: See "Troubleshooting" section above
- Configuration: Edit `local.settings.json` and `Program.cs`
- Customization: Edit `VideoTrigger.cs` for processing logic

