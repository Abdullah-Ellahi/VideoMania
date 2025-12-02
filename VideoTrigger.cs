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

    public VideoTrigger(ILogger<VideoTrigger> logger, VideoProcessingService videoProcessingService)
    {
        _logger = logger;
        _videoProcessingService = videoProcessingService;
    }

    [Function(nameof(VideoTrigger))]
    public async Task Run(
        [BlobTrigger("samples-workitems/{name}", Connection = "videomaniadev98e1_STORAGE")] Stream stream, 
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
            
            // Here you can add video processing logic:
            // - Extract video metadata (duration, resolution, codec)
            // - Generate thumbnails
            // - Transcode to different formats
            // - Upload to a media service
            // - Store metadata in a database
            // - Send notifications
            
            _logger.LogInformation("Video processing completed successfully for: {name}", name);
            
            // TODO: Add your video processing implementation here
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
            // 1. Save the video stream to a temporary file
            _logger.LogInformation("Saving video to temporary file...");
            tempVideoPath = await _videoProcessingService.SaveStreamToTempFileAsync(videoStream, fileName);

            // 2. Generate thumbnail at 1 second into the video
            _logger.LogInformation("Generating thumbnail...");
            thumbnailPath = await _videoProcessingService.GenerateThumbnailAsync(tempVideoPath, fileName, secondsIntoVideo: 1);
            
            if (thumbnailPath != null)
            {
                _logger.LogInformation("✓ Thumbnail created: {path}", thumbnailPath);
                // TODO: Upload thumbnail to blob storage (e.g., "thumbnails" container)
            }

            // 3. Resize video to 1280x720
            _logger.LogInformation("Resizing video to 1280x720...");
            resizedVideoPath = await _videoProcessingService.ResizeVideoAsync(tempVideoPath, fileName, width: 1280, height: 720);
            
            if (resizedVideoPath != null)
            {
                _logger.LogInformation("✓ Video resized: {path}", resizedVideoPath);
                // TODO: Upload resized video to blob storage (e.g., "processed-videos" container)
            }

            // 4. Get video metadata
            _logger.LogInformation("Extracting video metadata...");
            var metadata = await _videoProcessingService.GetVideoMetadataAsync(tempVideoPath);
            
            if (metadata != null)
            {
                _logger.LogInformation("✓ Metadata extracted - Size: {size} MB", 
                    metadata.FileSize / (1024.0 * 1024.0));
                // TODO: Store metadata in database or blob storage
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