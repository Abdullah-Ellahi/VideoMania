# How Web App Gets Cloud Function Results

## Quick Answer

**The web app reads the updated Cosmos DB document.** The cloud function automatically updates the same database where your web app stores video metadata, adding a `processing` field with thumbnails, resized video URLs, and metadata.

---

## Step-by-Step Flow

### **1. User Uploads Video (Web App)**

```
User selects video file in web app
    ↓
POST /api/getuploadSas with video
    ↓
UploadController creates:
  • Blob in "videos" container
  • Cosmos DB document in "Videos" container
    {
      "id": "uuid",
      "url": "uuid.mp4",        ← This is the blob name
      "title": "My Video",
      "uploadedAt": "2025-12-03..."
    }
    ↓
Web app shows: "Video uploaded successfully"
```

### **2. Cloud Function Processes (Background)**

```
Blob appears in "videos" container
    ↓
Cloud function triggered automatically
    ↓
Processes video (takes a few seconds to minutes depending on size)
    ↓
Uploads thumbnail → "thumbnails" container
Uploads resized → "processed-videos" container
    ↓
Updates same Cosmos DB document with results:
  {
    "id": "uuid",
    "url": "uuid.mp4",
    "title": "My Video",
    "uploadedAt": "2025-12-03...",
    "processing": {              ← NEW FIELD
      "processed": true,
      "processedAt": "2025-12-03...",
      "thumbnailUrl": "https://storage.../uuid_thumbnail.jpg",
      "resizedVideoUrl": "https://storage.../uuid_1280x720.mp4",
      "metadata": {
        "duration": 45.5,
        "width": 1920,
        "height": 1080,
        "videoCodec": "h264",
        ...
      }
    }
  }
```

### **3. Web App Displays Results (On-Demand or Polling)**

```
Option A - Polling (Simple)
  User refreshes video page
    ↓
  Web app queries Cosmos DB: SELECT * FROM Videos WHERE id = "uuid"
    ↓
  If "processing" field exists and "processed" = true:
    • Show thumbnail image
    • Provide download link for resized video
    • Display metadata
  Else:
    • Show "Processing..." status

Option B - Real-time (Advanced)
  Web app displays "Processing..." with auto-refresh every 2 seconds
    ↓
  JavaScript repeatedly calls: GET /api/video/{id}
    ↓
  Returns processing status
    ↓
  When complete, show thumbnail and links
```

---

## Implementation Examples

### **Example 1: Display Processing Status in Web App**

**VideoController.cs (new method):**

```csharp
[HttpGet("video/{id}/processing-status")]
public async Task<IActionResult> GetProcessingStatus(string id)
{
    try
    {
        var container = await _cosmosService.GetContainerAsync("Videos");
        var response = await container.ReadItemAsync<VideoDocument>(id, new PartitionKey(id));
        var video = response.Resource;
        
        if (video.processing == null)
        {
            return Ok(new { 
                processed = false, 
                message = "Processing not started yet" 
            });
        }
        
        if (video.processing.processed)
        {
            return Ok(new { 
                processed = true,
                thumbnailUrl = video.processing.thumbnailUrl,
                resizedVideoUrl = video.processing.resizedVideoUrl,
                metadata = video.processing.metadata,
                processedAt = video.processing.processedAt
            });
        }
        else
        {
            return Ok(new { 
                processed = false, 
                message = "Still processing..." 
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting processing status");
        return StatusCode(500, new { error = ex.Message });
    }
}
```

### **Example 2: Display in Razor Page (View)**

**Videos/Details.cshtml.cs (Code-behind):**

```csharp
public async Task<IActionResult> OnGetAsync(string id)
{
    // Get video with processing metadata
    var container = await _cosmosService.GetContainerAsync("Videos");
    var response = await container.ReadItemAsync<VideoDocument>(id, new PartitionKey(id));
    Video = response.Resource;
    
    // Check if processed
    IsProcessed = Video?.processing?.processed ?? false;
    
    return Page();
}
```

**Videos/Details.cshtml (HTML):**

```html
<div class="video-details">
    <h2>@Model.Video.title</h2>
    
    @if (Model.IsProcessed)
    {
        <div class="processed-content">
            <!-- Thumbnail -->
            <div class="thumbnail-section">
                <h4>Thumbnail</h4>
                <img src="@Model.Video.processing.thumbnailUrl" alt="Thumbnail" />
            </div>
            
            <!-- Resized Video -->
            <div class="resized-video-section">
                <h4>Processed Video (1280x720)</h4>
                <video controls width="640" height="480">
                    <source src="@Model.Video.processing.resizedVideoUrl" type="video/mp4" />
                    Your browser does not support HTML5 video.
                </video>
                <a href="@Model.Video.processing.resizedVideoUrl" download>Download</a>
            </div>
            
            <!-- Metadata -->
            <div class="metadata-section">
                <h4>Video Details</h4>
                <ul>
                    <li>Duration: @Model.Video.processing.metadata.duration seconds</li>
                    <li>Resolution: @Model.Video.processing.metadata.width x @Model.Video.processing.metadata.height</li>
                    <li>Video Codec: @Model.Video.processing.metadata.videoCodec</li>
                    <li>Audio Codec: @Model.Video.processing.metadata.audioCodec</li>
                    <li>Frame Rate: @Model.Video.processing.metadata.frameRate fps</li>
                    <li>Processed: @Model.Video.processing.processedAt</li>
                </ul>
            </div>
        </div>
    }
    else
    {
        <div class="processing-message">
            <p>Video is being processed...</p>
            <p><em>Generated: @Model.Video.uploadedAt</em></p>
        </div>
    }
</div>

<!-- JavaScript to auto-refresh status (optional) -->
<script>
    const videoId = "@Model.Video.id";
    const statusCheckInterval = setInterval(() => {
        fetch(`/api/video/${videoId}/processing-status`)
            .then(r => r.json())
            .then(data => {
                if (data.processed) {
                    clearInterval(statusCheckInterval);
                    location.reload(); // Reload to show processed content
                }
            });
    }, 2000); // Check every 2 seconds
</script>
```

### **Example 3: JSON Response for API Clients**

**Cosmos DB Query:**

```sql
SELECT 
    c.id,
    c.title,
    c.url,
    c.uploadedAt,
    c.processing.processed,
    c.processing.processedAt,
    c.processing.thumbnailUrl,
    c.processing.resizedVideoUrl,
    c.processing.metadata
FROM Videos c
WHERE c.id = "uuid-here"
```

**Response:**

```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "title": "My Video",
  "url": "123e4567-e89b-12d3-a456-426614174000.mp4",
  "uploadedAt": "2025-12-03T10:30:00Z",
  "processing": {
    "processed": true,
    "processedAt": "2025-12-03T10:35:45Z",
    "thumbnailUrl": "https://videomaniadev98e1.blob.core.windows.net/thumbnails/123e4567_thumbnail.jpg?sv=2023-01-01&...",
    "resizedVideoUrl": "https://videomaniadev98e1.blob.core.windows.net/processed-videos/123e4567_1280x720.mp4?sv=2023-01-01&...",
    "metadata": {
      "duration": 45.5,
      "width": 1920,
      "height": 1080,
      "videoCodec": "h264",
      "audioCodec": "aac",
      "frameRate": 30.0,
      "bitRate": 5000000
    }
  }
}
```

---

## Key Points

1. **Single Database**: Both web app and cloud function use the **same Cosmos DB**
2. **Automatic Triggering**: Cloud function starts automatically when video is uploaded
3. **In-Place Updates**: Same document is updated, no new document is created
4. **Always Available**: Once `processing.processed = true`, all URLs are accessible
5. **No Extra Configuration**: No webhooks or message queues needed - it's all automatic

---

## Common Questions

**Q: How long does processing take?**
A: Depends on video size. Usually 5-30 seconds. Larger videos take longer.

**Q: What if processing fails?**
A: Error is logged. The `processing` field remains incomplete or missing. Check Azure Portal → Function App → Monitor for errors.

**Q: Can the web app show real-time progress?**
A: Not with current setup. You can:
- Poll Cosmos DB every 2 seconds
- Add message queue for detailed progress updates
- Use Application Insights for detailed logs

**Q: Are thumbnail and resized video URLs permanent?**
A: Yes, they're stored in blob storage. URLs will work indefinitely (or until manually deleted).

**Q: Can I customize what processing the cloud function does?**
A: Yes! Edit `VideoTrigger.cs` ProcessVideoAsync method to:
- Change thumbnail timing (change `secondsIntoVideo` parameter)
- Change resize dimensions (change `width` and `height`)
- Add additional processing steps (bitrate reduction, format conversion, etc.)

