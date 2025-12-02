# Cloud Function Architecture Diagram

## Complete System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           YOUR VIDEO SYSTEM                              │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────┐
│     WEB APPLICATION          │
│  (ASP.NET Core 8)            │
├──────────────────────────────┤
│ UploadController:            │
│ POST /api/getuploadSas       │
│                              │
│ 1. Receive video file        │
│ 2. Validate file type/size   │
│ 3. Upload to blob storage    │
│ 4. Save metadata to DB       │
│                              │
│ VideoController:             │
│ GET /videos/{id}             │
│                              │
│ 1. Query Cosmos DB           │
│ 2. Display video details     │
│ 3. Show processing status    │
│ 4. Display thumbnail         │
│ 5. Offer download            │
└──────────────────────────────┘
            │
            │ (Uploads blob)
            ↓
┌──────────────────────────────┐
│   AZURE BLOB STORAGE         │
│   (videomaniadev98e1)        │
├──────────────────────────────┤
│                              │
│  📁 videos (INPUT)           │
│     ├─ uuid-1.mp4            │
│     ├─ uuid-2.mov            │
│     └─ uuid-3.webm           │
│                              │
│  📁 thumbnails (OUTPUT)      │
│     ├─ uuid-1_thumbnail.jpg  │
│     └─ uuid-2_thumbnail.jpg  │
│                              │
│  📁 processed-videos (OUTPUT)│
│     ├─ uuid-1_1280x720.mp4   │
│     └─ uuid-2_1280x720.mp4   │
│                              │
└──────────────────────────────┘
     ▲                │
     │ (Update DB)    │ (Blob Trigger)
     │                ↓
     │         ┌──────────────────────────────┐
     │         │   AZURE CLOUD FUNCTION       │
     │         │   (VideoTrigger.cs)          │
     │         ├──────────────────────────────┤
     │         │ Event: Blob added to         │
     │         │ "videos" container           │
     │         │                              │
     │         │ 1. Receive video stream      │
     │         │ 2. Look up video ID in DB    │
     │         │ 3. Save stream to temp file  │
     │         │ 4. Generate thumbnail       │
     │         │ 5. Resize video (1280x720)  │
     │         │ 6. Extract metadata         │
     │         │ 7. Upload results to blob   │
     │         │ 8. Update Cosmos DB         │
     │         │ 9. Clean up temp files      │
     │         │                              │
     │         │ Duration: 10-30 seconds     │
     │         │ (depending on file size)    │
     │         └──────────────────────────────┘
     │                │
     │ (Query)        │ (Query & Update)
     │                ↓
     └─────────────────────────────────┐
                                       │
                        ┌──────────────────────────────┐
                        │    AZURE COSMOS DB           │
                        │    (videomania database)     │
                        ├──────────────────────────────┤
                        │                              │
                        │  📦 Videos Container         │
                        │                              │
                        │  Document BEFORE processing: │
                        │  {                           │
                        │    "id": "uuid",             │
                        │    "url": "uuid.mp4",        │
                        │    "title": "My Video",      │
                        │    "uploadedAt": "...",      │
                        │    "description": "..."      │
                        │  }                           │
                        │                              │
                        │  Document AFTER processing:  │
                        │  {                           │
                        │    "id": "uuid",             │
                        │    "url": "uuid.mp4",        │
                        │    "title": "My Video",      │
                        │    "uploadedAt": "...",      │
                        │    "description": "...",     │
                        │    "processing": {           │
                        │      "processed": true,      │
                        │      "processedAt": "...",   │
                        │      "thumbnailUrl": "https" │
                        │      "resizedVideoUrl":"http"│
                        │      "metadata": {           │
                        │        "duration": 45.5,     │
                        │        "width": 1920,        │
                        │        "height": 1080,       │
                        │        "videoCodec": "h264"  │
                        │        ...                   │
                        │      }                       │
                        │    }                         │
                        │  }                           │
                        │                              │
                        └──────────────────────────────┘

```

---

## Data Flow Sequence

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         STEP-BY-STEP PROCESS                             │
└─────────────────────────────────────────────────────────────────────────┘

[1] USER UPLOADS VIDEO
    ↓
    Web App: POST /api/getuploadSas with video file
    ↓
    
[2] WEB APP CREATES BLOB
    ↓
    BlobStorageService.UploadBlobAsync()
    ↓
    Creates: videos/uuid.mp4 in Azure Blob Storage
    ↓

[3] WEB APP SAVES METADATA
    ↓
    CosmosDbService.AddItemAsync(video, "Videos")
    ↓
    Creates: Document in Cosmos DB "Videos" container
    {
      "id": "uuid",
      "url": "uuid.mp4",
      "title": "My Video",
      "uploadedAt": "2025-12-03T10:30:00Z"
    }
    ↓

[4] WEB APP RETURNS SUCCESS
    ↓
    200 OK: "Video uploaded successfully"
    ↓

[5] BLOB TRIGGER FIRES (Automatic - no code needed)
    ↓
    Azure detects new blob in "videos" container
    ↓
    Automatically invokes cloud function
    ↓

[6] CLOUD FUNCTION STARTS
    ↓
    VideoTrigger.Run() receives:
    - Stream of video file
    - Blob name: "uuid.mp4"
    ↓

[7] LOOKUP VIDEO ID
    ↓
    CosmosDbService.GetVideoIdByBlobNameAsync(fileName)
    ↓
    Query Cosmos DB: SELECT id FROM Videos WHERE url = "uuid.mp4"
    ↓
    Returns: "uuid"
    ↓

[8] SAVE STREAM TO TEMP FILE
    ↓
    VideoProcessingService.SaveStreamToTempFileAsync()
    ↓
    Creates: /tmp/VideoProcessing/uuid.mp4
    ↓

[9] GENERATE THUMBNAIL
    ↓
    VideoProcessingService.GenerateThumbnailAsync()
    ↓
    FFmpeg extracts frame at 1 second
    ↓
    Creates: /tmp/VideoProcessing/uuid_thumbnail.jpg
    ↓

[10] UPLOAD THUMBNAIL TO BLOB
     ↓
     BlobStorageServiceForFunctions.UploadProcessedBlobAsync()
     ↓
     Creates: thumbnails/uuid_thumbnail.jpg in Azure Blob Storage
     ↓
     Returns: https://storage.../uuid_thumbnail.jpg
     ↓

[11] RESIZE VIDEO
     ↓
     VideoProcessingService.ResizeVideoAsync()
     ↓
     FFmpeg transcodes video to 1280x720
     ↓
     Creates: /tmp/VideoProcessing/uuid_resized.mp4
     ↓

[12] UPLOAD RESIZED VIDEO TO BLOB
     ↓
     BlobStorageServiceForFunctions.UploadProcessedBlobAsync()
     ↓
     Creates: processed-videos/uuid_1280x720.mp4 in Azure Blob Storage
     ↓
     Returns: https://storage.../uuid_1280x720.mp4
     ↓

[13] EXTRACT METADATA
     ↓
     VideoProcessingService.GetVideoMetadataAsync()
     ↓
     FFmpeg reads video properties
     ↓
     Returns: {duration: 45.5, width: 1920, height: 1080, ...}
     ↓

[14] UPDATE COSMOS DB
     ↓
     CosmosDbService.UpdateVideoProcessingAsync()
     ↓
     Updates same document from step [3]:
     {
       "id": "uuid",
       "url": "uuid.mp4",
       "title": "My Video",
       "uploadedAt": "2025-12-03T10:30:00Z",
       "processing": {
         "processed": true,
         "processedAt": "2025-12-03T10:35:45Z",
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
     ↓

[15] CLEANUP
     ↓
     Delete temp files:
     - /tmp/VideoProcessing/uuid.mp4
     - /tmp/VideoProcessing/uuid_thumbnail.jpg
     - /tmp/VideoProcessing/uuid_resized.mp4
     ↓

[16] FUNCTION COMPLETES
     ↓
     Cloud function execution ends
     ↓
     All logs sent to Application Insights
     ↓

[17] WEB APP POLLS (Optional)
     ↓
     JavaScript on web page calls: GET /api/video/uuid/processing-status
     ↓
     Checks if "processing.processed" = true
     ↓
     When true: Display thumbnail, resized video, metadata
     ↓
     When false: Show "Processing..." message
     ↓

[18] USER VIEWS RESULTS
     ↓
     Web page displays:
     - Thumbnail image
     - Resized video player
     - Video metadata (duration, resolution, codec)
     - Links to download files
     ↓

```

---

## Storage Layout

```
Azure Blob Storage (videomaniadev98e1)
│
├─ videos/ (Input Container)
│  │
│  ├─ 123e4567-e89b-12d3-a456-426614174000.mp4      ← User uploads here
│  ├─ 987f6543-a21b-45c6-d789-012345678901.mov      ← Via web app
│  └─ ...
│
├─ thumbnails/ (Output Container - Cloud Function writes)
│  │
│  ├─ 123e4567-e89b-12d3-a456-426614174000_thumbnail.jpg
│  ├─ 987f6543-a21b-45c6-d789-012345678901_thumbnail.jpg
│  └─ ...
│
└─ processed-videos/ (Output Container - Cloud Function writes)
   │
   ├─ 123e4567-e89b-12d3-a456-426614174000_1280x720.mp4
   ├─ 987f6543-a21b-45c6-d789-012345678901_1280x720.mov
   └─ ...
```

---

## Service Dependencies

```
┌─────────────────────────────────────────────────────────┐
│              CloudFunction (VideoTrigger)               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│   Depends on:                                           │
│   ┌──────────────────────────────────────────────┐     │
│   │ VideoProcessingService                       │     │
│   ├──────────────────────────────────────────────┤     │
│   │ • GenerateThumbnailAsync()     (Uses FFmpeg) │     │
│   │ • ResizeVideoAsync()           (Uses FFmpeg) │     │
│   │ • GetVideoMetadataAsync()      (Uses FFmpeg) │     │
│   │ • SaveStreamToTempFileAsync()                │     │
│   │ • CleanupTempFile()                          │     │
│   └──────────────────────────────────────────────┘     │
│                                                         │
│   ┌──────────────────────────────────────────────┐     │
│   │ CosmosDbService                              │     │
│   ├──────────────────────────────────────────────┤     │
│   │ • GetVideoIdByBlobNameAsync()                │     │
│   │ • UpdateVideoProcessingAsync()               │     │
│   └──────────────────────────────────────────────┘     │
│                                                         │
│   ┌──────────────────────────────────────────────┐     │
│   │ BlobStorageServiceForFunctions               │     │
│   ├──────────────────────────────────────────────┤     │
│   │ • UploadProcessedBlobAsync()                 │     │
│   │ • GetReadSasUriAsync()                       │     │
│   │ • GetContainerClientAsync()                  │     │
│   └──────────────────────────────────────────────┘     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## Comparison: Web App vs Cloud Function

```
┌──────────────────────┬─────────────────────────┬──────────────────────┐
│    Aspect            │      Web App             │   Cloud Function     │
├──────────────────────┼─────────────────────────┼──────────────────────┤
│ Triggered by         │ User HTTP request       │ Blob upload event    │
│                      │ (POST /api/upload)      │ (automatic)          │
├──────────────────────┼─────────────────────────┼──────────────────────┤
│ Blob Container       │ Reads/Writes: videos    │ Reads: videos        │
│                      │                         │ Writes: thumbnails   │
│                      │                         │        processed     │
├──────────────────────┼─────────────────────────┼──────────────────────┤
│ Database             │ Cosmos DB (add)         │ Cosmos DB (update)   │
│ Operation            │ Creates new document    │ Updates existing     │
├──────────────────────┼─────────────────────────┼──────────────────────┤
│ Duration             │ < 1 second              │ 10-30 seconds        │
├──────────────────────┼─────────────────────────┼──────────────────────┤
│ Response to user     │ Immediate (sync)        │ Background (async)   │
├──────────────────────┼─────────────────────────┼──────────────────────┤
│ Cost model           │ Always running          │ Pay per execution    │
├──────────────────────┼─────────────────────────┼──────────────────────┤
│ Video processing     │ No (delegates to func)  │ Yes (FFmpeg)         │
├──────────────────────┼─────────────────────────┼──────────────────────┤
│ Uses                 │ User interaction        │ Server processing    │
│ by                   │ Web UI                  │ Schedule/Events      │
└──────────────────────┴─────────────────────────┴──────────────────────┘
```

