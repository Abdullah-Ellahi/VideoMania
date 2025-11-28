# VideoMania - Azure Functions Video Processing

🎥 **Automated video processing pipeline using Azure Functions, Blob Storage, and FFmpeg**

## Overview

VideoMania is a serverless video processing application built on Azure Functions that automatically processes videos uploaded to Azure Blob Storage. The function generates thumbnails, resizes videos, and extracts metadata using FFmpeg.

## Features

✨ **Automatic Video Processing**
- 🖼️ **Thumbnail Generation**: Extracts a JPEG thumbnail at 1 second into the video
- 📐 **Video Resizing**: Resizes videos to 1280x720 while maintaining aspect ratio
- 📊 **Metadata Extraction**: Captures duration, resolution, codec, framerate, and bitrate
- 🔍 **File Validation**: Supports multiple video formats (.mp4, .avi, .mov, .wmv, .flv, .mkv, .webm, .m4v)

🚀 **Serverless Architecture**
- Triggered automatically when videos are uploaded to Blob Storage
- Scales automatically based on workload
- No infrastructure management required
- **No manual FFmpeg installation needed** - automatically downloads on first run

📈 **Monitoring & Logging**
- Complete execution logs in Azure Application Insights
- Detailed processing metrics and diagnostics
- Real-time monitoring and alerting

## Architecture

```
Azure Blob Storage (samples-workitems)
           ↓
    Blob Trigger Event
           ↓
   Azure Function (VideoTrigger)
           ↓
    Video Processing Service
           ↓
    FFmpeg Operations (Thumbnail, Resize, Metadata)
           ↓
    Application Insights (Logs & Metrics)
```

## Technology Stack

- **Runtime**: .NET 8 Isolated Worker
- **Cloud Platform**: Azure Functions (Consumption Plan)
- **Storage**: Azure Blob Storage
- **Video Processing**: Xabe.FFmpeg 6.0.2 (auto-downloads FFmpeg binaries)
- **Monitoring**: Azure Application Insights
- **Language**: C#

## Prerequisites

- .NET 8 SDK
- Azure Subscription
- Azure Storage Account
- Azure Functions Core Tools (for local development)
- Visual Studio Code (recommended) or Visual Studio
- **No manual FFmpeg installation required!** ✨

## Project Structure

```
D:\VM\
├── VideoTrigger.cs              # Main Azure Function with blob trigger
├── Services/
│   └── VideoProcessingService.cs # Video processing logic with FFmpeg
├── Program.cs                    # Application startup and DI configuration
├── host.json                     # Function host configuration
├── local.settings.json           # Local development settings
├── VM.csproj                     # Project file with dependencies
└── README.md                     # This file
```

## Installation & Setup

### 1. Clone the Repository

```powershell
git clone https://github.com/Abdullah-Ellahi/VideoMania.git
cd VideoMania
```

### 2. Configure Local Settings

Update `local.settings.json` with your Azure Storage connection string:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "YOUR_STORAGE_CONNECTION_STRING",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "videomaniadev98e1_STORAGE": "YOUR_STORAGE_CONNECTION_STRING"
  }
}
```

### 3. Restore Dependencies

```powershell
dotnet restore
```

### 4. Run Locally

```powershell
func start
```

FFmpeg binaries will be **automatically downloaded** on first execution!

Upload a video to your `samples-workitems` container and watch the logs!

## Deployment to Azure

### Option 1: Using Azure Functions Core Tools

```powershell
# Login to Azure
az login

# Deploy to your function app
func azure functionapp publish YOUR_FUNCTION_APP_NAME
```

### Option 2: Using VS Code Azure Extension

1. Install the Azure Functions extension
2. Right-click the project folder
3. Select **Deploy to Function App...**
4. Choose your function app

### Post-Deployment Configuration

Configure the storage connection string in Azure:

```powershell
az functionapp config appsettings set `
  --name YOUR_FUNCTION_APP_NAME `
  --resource-group YOUR_RESOURCE_GROUP `
  --settings "videomaniadev98e1_STORAGE=YOUR_STORAGE_CONNECTION_STRING"
```

## Usage

### Upload a Video

1. Navigate to your Azure Storage Account
2. Go to **Containers** → **samples-workitems**
3. Upload any supported video file (.mp4, .avi, .mov, etc.)
4. The function will automatically trigger within 1-2 minutes (Consumption plan polling)

### Monitor Execution

**View Logs in Application Insights:**

```kusto
traces
| where timestamp > ago(30m)
| where operation_Name == "VideoTrigger"
| project timestamp, message
| order by timestamp asc
```

**Expected Log Output:**

```
=== Video Upload Triggered ===
File Name: yourvideo.mp4
File Extension: .mp4
File Size: 1596191 bytes (1.52 MB)
Valid video file detected!
Downloading FFmpeg binaries...
FFmpeg initialized successfully!
Generating thumbnail...
✓ Thumbnail generated: thumbnail.jpg (66 KB)
Resizing video to 1280x720...
✓ Video resized: resized.mp4 (1.20 MB)
Extracting video metadata...
✓ Metadata extracted - Duration: 23.37s, Resolution: 1280x720, Codec: h264
Video processing completed successfully!
```

## Code Highlights

### VideoTrigger Function

```csharp
[Function("VideoTrigger")]
public async Task Run(
    [BlobTrigger("samples-workitems/{name}", 
    Connection = "videomaniadev98e1_STORAGE")] Stream myBlob, 
    string name)
{
    _logger.LogInformation("=== Video Upload Triggered ===");
    _logger.LogInformation($"File Name: {name}");
    
    // Validate video file
    if (!IsValidVideoFile(name))
    {
        _logger.LogWarning("Invalid file type. Skipping processing.");
        return;
    }
    
    // Process video
    await _videoProcessingService.ProcessVideoAsync(myBlob, name);
}
```

### Video Processing Service

```csharp
public async Task ProcessVideoAsync(Stream videoStream, string fileName)
{
    await InitializeFFmpegAsync();
    
    string tempFilePath = await SaveStreamToTempFileAsync(videoStream, fileName);
    
    // Generate thumbnail
    await GenerateThumbnailAsync(tempFilePath, fileName);
    
    // Resize video
    await ResizeVideoAsync(tempFilePath, fileName);
    
    // Extract metadata
    await GetVideoMetadataAsync(tempFilePath);
    
    CleanupTempFile(tempFilePath);
}
```

## Dependencies

```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.0.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs" Version="6.8.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="2.0.0" />
<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.22.0" />
<PackageReference Include="Xabe.FFmpeg" Version="6.0.2" />
<PackageReference Include="Xabe.FFmpeg.Downloader" Version="6.0.2" />
```

## Configuration

### host.json

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "maxTelemetryItemsPerSecond": 20
      }
    }
  }
}
```

### Supported Video Formats

- `.mp4` - MPEG-4 Video
- `.avi` - Audio Video Interleave
- `.mov` - QuickTime Movie
- `.wmv` - Windows Media Video
- `.flv` - Flash Video
- `.mkv` - Matroska Video
- `.webm` - WebM Video
- `.m4v` - MPEG-4 Video File

## Troubleshooting

### Function Not Triggering

1. **Check Application Settings**: Ensure `videomaniadev98e1_STORAGE` is configured
2. **Verify Container Name**: Must be `samples-workitems`
3. **Wait for Polling**: Consumption plan polls every 1-2 minutes
4. **Check Logs**: Look for errors in Application Insights

### FFmpeg Issues

- FFmpeg is **automatically downloaded** on first execution
- Stored in temporary directory: `%TEMP%\VideoProcessing\`
- No manual installation required

### Logs Not Appearing

Run this query in Application Insights:

```kusto
traces
| where timestamp > ago(1h)
| order by timestamp desc
```

## Performance

- **Thumbnail Generation**: ~3-5 seconds
- **Video Resize (1280x720)**: ~10-15 seconds for typical videos
- **Total Processing Time**: ~20-45 seconds depending on video size
- **First Execution**: +60 seconds for FFmpeg binary download

## Future Enhancements

- [ ] Upload processed files (thumbnail, resized video) to separate blob containers
- [ ] Store video metadata in Azure Table Storage or Cosmos DB
- [ ] Add support for multiple thumbnail sizes
- [ ] Implement video format conversion (e.g., convert AVI to MP4)
- [ ] Add Azure Queue or Event Grid notifications after processing
- [ ] Implement retry logic for failed processing
- [ ] Add batch processing for multiple videos

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.

## Authors

- **Shozab Mehdii** - Initial development and Azure deployment
- **Abdullah Ellahi** - Repository owner

## Acknowledgments

- **Xabe.FFmpeg** for the excellent FFmpeg wrapper
- **Microsoft Azure** for serverless infrastructure
- **Azure Functions Team** for comprehensive documentation

## Support

For issues and questions:
- Open an issue on GitHub: https://github.com/Abdullah-Ellahi/VideoMania/issues
- Check Azure Functions documentation: https://docs.microsoft.com/azure/azure-functions/

---

**Built with ❤️ using Azure Functions and .NET 8**
