using System.IO;
using System.Threading.Tasks;
using Company.Function.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class VideoTrigger
{
    private readonly ILogger<VideoTrigger> _logger;
    private readonly VideoProcessingService _videoProcessingService;
    private readonly CosmosDbService _cosmosDbService;
    private readonly BlobStorageServiceForFunctions _blobStorageService;

    public VideoTrigger(
        ILogger<VideoTrigger> logger, 
        VideoProcessingService videoProcessingService,
        CosmosDbService cosmosDbService,
        BlobStorageServiceForFunctions blobStorageService)
    {
        _logger = logger;
        _videoProcessingService = videoProcessingService;
        _cosmosDbService = cosmosDbService;
        _blobStorageService = blobStorageService;
    }

    [Function(nameof(VideoTrigger))]
    public async Task Run(
        [BlobTrigger("videos/{name}", Connection = "BLOB_STORAGE_CONNECTION_STRING")] Stream stream, 
        string name)
    {
        _logger.LogInformation("=== Video Upload Triggered ===");
        _logger.LogInformation("File Name: {name}", name);
        
        try
        {
            // Get file metadata
            var fileExtension = Path.GetExtension(name).ToLowerInvariant();
            var fileSize = stream.Length;
            var fileSizeInMB = fileSize / (1024.0 * 1024.0);
            
            _logger.LogInformation("File Extension: {extension}", fileExtension);
            _logger.LogInformation("File Size: {size} bytes ({sizeMB:F2} MB)", fileSize, fileSizeInMB);
            
            // Validate if it's a video file
            var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v" };
            var isVideoFile = videoExtensions.Contains(fileExtension);
            
            if (!isVideoFile)
            {
                _logger.LogWarning("Uploaded file is not a recognized video format: {extension}", fileExtension);
                return;
            }
            
            _logger.LogInformation("Valid video file detected!");
            
            // Process the video
            await ProcessVideoAsync(stream, name, fileExtension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video file: {name}", name);
            throw;
        }
    }
    
    private async Task ProcessVideoAsync(Stream videoStream, string fileName, string fileExtension)
    {
        string tempVideoPath = null;
        string thumbnailPath = null;
        string resizedVideoPath = null;

        try
        {
            // Find the video ID in Cosmos DB by blob name
            _logger.LogInformation("Looking up video ID from Cosmos DB...");
            var videoId = await _cosmosDbService.GetVideoIdByBlobNameAsync(fileName);
            
            if (string.IsNullOrEmpty(videoId))
            {
                _logger.LogWarning("Could not find video ID for blob: {fileName}. Skipping processing.", fileName);
                return;
            }
            
            _logger.LogInformation("Found video ID: {videoId}", videoId);

            // 1. Save the video stream to a temporary file
            _logger.LogInformation("Saving video to temporary file...");
            tempVideoPath = await _videoProcessingService.SaveStreamToTempFileAsync(videoStream, fileName);

            // 2. Generate thumbnail at 1 second into the video
            _logger.LogInformation("Generating thumbnail...");
            thumbnailPath = await _videoProcessingService.GenerateThumbnailAsync(tempVideoPath, fileName, secondsIntoVideo: 1);
            
            string thumbnailUrl = null;
            if (thumbnailPath != null)
            {
                _logger.LogInformation("✓ Thumbnail created: {path}", thumbnailPath);
                
                // Upload thumbnail to blob storage in "thumbnails" container
                var thumbnailBlobName = $"{Path.GetFileNameWithoutExtension(fileName)}_thumbnail.jpg";
                thumbnailUrl = await _blobStorageService.UploadProcessedBlobAsync(
                    thumbnailPath, 
                    "thumbnails", 
                    thumbnailBlobName);
                
                if (thumbnailUrl != null)
                {
                    _logger.LogInformation("✓ Thumbnail uploaded to blob storage: {url}", thumbnailUrl);
                }
            }

            // 3. Resize video to 1280x720
            _logger.LogInformation("Resizing video to 1280x720...");
            resizedVideoPath = await _videoProcessingService.ResizeVideoAsync(tempVideoPath, fileName, width: 1280, height: 720);
            
            string resizedVideoUrl = null;
            if (resizedVideoPath != null)
            {
                _logger.LogInformation("✓ Video resized: {path}", resizedVideoPath);
                
                // Upload resized video to blob storage in "processed-videos" container
                var resizedBlobName = $"{Path.GetFileNameWithoutExtension(fileName)}_1280x720{Path.GetExtension(fileName)}";
                resizedVideoUrl = await _blobStorageService.UploadProcessedBlobAsync(
                    resizedVideoPath, 
                    "processed-videos", 
                    resizedBlobName);
                
                if (resizedVideoUrl != null)
                {
                    _logger.LogInformation("✓ Resized video uploaded to blob storage: {url}", resizedVideoUrl);
                }
            }

            // 4. Get video metadata
            _logger.LogInformation("Extracting video metadata...");
            var metadata = await _videoProcessingService.GetVideoMetadataAsync(tempVideoPath);
            
            if (metadata != null)
            {
                _logger.LogInformation("✓ Metadata extracted - Duration: {duration:F2}s, Resolution: {width}x{height}", 
                    metadata.Duration, metadata.Width, metadata.Height);
                
                // Save processing results to Cosmos DB
                var processingResult = new VideoProcessingResult
                {
                    ThumbnailUrl = thumbnailUrl ?? string.Empty,
                    ResizedVideoUrl = resizedVideoUrl ?? string.Empty,
                    Duration = metadata.Duration,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    VideoCodec = metadata.VideoCodec,
                    AudioCodec = metadata.AudioCodec,
                    FrameRate = metadata.FrameRate,
                    BitRate = metadata.BitRate
                };
                
                var updated = await _cosmosDbService.UpdateVideoProcessingAsync(videoId, processingResult);
                
                if (updated)
                {
                    _logger.LogInformation("✓ Video processing metadata saved to Cosmos DB!");
                }
            }

            _logger.LogInformation("Video processing completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during video processing");
            throw;
        }
        finally
        {
            // Cleanup temporary files
            _logger.LogInformation("Cleaning up temporary files...");
            
            if (tempVideoPath != null)
                _videoProcessingService.CleanupTempFile(tempVideoPath);
            
            if (thumbnailPath != null)
                _videoProcessingService.CleanupTempFile(thumbnailPath);
            
            if (resizedVideoPath != null)
                _videoProcessingService.CleanupTempFile(resizedVideoPath);
        }
    }
}