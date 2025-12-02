using ICC.AzureAppService.Demo.Models;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace ICC.AzureAppService.Demo.Controllers
{
    [ApiController]
    [Route("api/videos")]
    public class VideoController : ControllerBase
    {
        private readonly CosmosDbService _cosmosService;
        private readonly BlobStorageService _blobService;
        private readonly ILogger<VideoController> _logger;

        public VideoController(
            CosmosDbService cosmosService,
            BlobStorageService blobService,
            ILogger<VideoController> logger)
        {
            _cosmosService = cosmosService;
            _blobService = blobService;
            _logger = logger;
        }

        /// <summary>
        /// Delete a video and all its associated comments
        /// </summary>
        [HttpDelete("{videoId}")]
        public async Task<IActionResult> DeleteVideo(string videoId)
        {
            _logger.LogInformation("🔴 DELETE video request: {videoId}", videoId);

            if (string.IsNullOrEmpty(videoId))
            {
                _logger.LogWarning("DeleteVideo: videoId is null or empty");
                return BadRequest(new { success = false, error = "Video ID is required" });
            }

            try
            {
                // Fetch video to get userId (partition key) and blob name
                var video = await _cosmosService.GetVideoByIdAsync(videoId);
                if (video == null)
                {
                    _logger.LogWarning("DeleteVideo: Video {videoId} not found", videoId);
                    return NotFound(new { success = false, error = "Video not found" });
                }

                _logger.LogInformation("Video found: {Title}, userId: {userId}, Url: {Url}",
                    video.title, video.userId, video.url);

                // Step 1: Delete all comments associated with this video
                _logger.LogInformation("Deleting comments for video {videoId}", videoId);
                try
                {
                    var comments = await _cosmosService.GetCommentsAsync(videoId);
                    _logger.LogInformation("Found {Count} comments to delete", comments.Count);

                    foreach (var comment in comments)
                    {
                        if (!string.IsNullOrEmpty(comment?.id))
                        {
                            try
                            {
                                await _cosmosService.DeleteCommentAsync(comment.id, comment.videoId);
                                _logger.LogInformation("Deleted comment {CommentId}", comment.id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error deleting comment {CommentId}", comment.id);
                                // Continue deleting other comments even if one fails
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error fetching or deleting comments for video {videoId}", videoId);
                    // Continue with video deletion even if comment deletion fails
                }

                // Step 2: Delete blob from storage
                if (_blobService != null && !string.IsNullOrEmpty(video.url))
                {
                    try
                    {
                        // Extract blob name from URL
                        string blobName;

                        if (video.url.Contains("?"))
                        {
                            // Has SAS token - remove it
                            blobName = video.url.Split('?')[0];
                        }
                        else
                        {
                            blobName = video.url;
                        }

                        // If it's a full URL, get just the last part (the blob name)
                        if (blobName.Contains("/"))
                        {
                            blobName = blobName.Split('/').Last();
                        }

                        _logger.LogInformation("Attempting to delete blob: {BlobName}", blobName);
                        await _blobService.DeleteBlobAsync(blobName);
                        _logger.LogInformation("Blob deleted successfully: {BlobName}", blobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting blob for video {videoId}. Continuing with database deletion.", videoId);
                        // Continue with database deletion even if blob deletion fails
                    }
                }

                // Step 3: Delete video from Cosmos DB (MUST BE LAST)
                _logger.LogInformation("Deleting video {videoId} from database with partition key {userId}",
                    videoId, video.userId);

                try
                {
                    await _cosmosService.DeleteVideoAsync(videoId, video.userId);
                    _logger.LogInformation("✅ Video {videoId} deleted successfully from database", videoId);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError("Video {videoId} not found in Cosmos DB during deletion", videoId);
                    return StatusCode(500, new
                    {
                        success = false,
                        error = "Video found in query but not found during deletion. Database may be inconsistent."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Video deleted successfully",
                    videoId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting video {videoId}: {Message}", videoId, ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Error deleting video: {ex.Message}"
                });
            }
        }
    }
}