using ICC.AzureAppService.Demo.Models;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace ICC.AzureAppService.Demo.Controllers
{
    [ApiController]
    [Route("api/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly CosmosDbService _cosmosService;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            CosmosDbService cosmosService,
            ILogger<CommentsController> logger)
        {
            _cosmosService = cosmosService;
            _logger = logger;
        }

        /// <summary>
        /// Add a comment to a video
        /// </summary>
        [HttpPost("add")]
        [Consumes("application/json")]
        public async Task<IActionResult> AddComment([FromBody] AddCommentRequest request)
        {
            _logger.LogInformation("🔴 ADD comment request: videoId={videoId}, Text={Text}", 
                request.videoId, request.CommentText?.Substring(0, Math.Min(50, request.CommentText?.Length ?? 0)));

            // Validate input
            if (string.IsNullOrEmpty(request.videoId))
            {
                _logger.LogWarning("AddComment: videoId is required");
                return BadRequest(new { success = false, error = "Video ID is required" });
            }

            if (string.IsNullOrWhiteSpace(request.CommentText))
            {
                _logger.LogWarning("AddComment: CommentText is required");
                return BadRequest(new { success = false, error = "Comment text cannot be empty" });
            }

            try
            {
                // Verify video exists
                _logger.LogInformation("Verifying video {videoId} exists", request.videoId);
                var video = await _cosmosService.GetVideoByIdAsync(request.videoId);

                if (video == null)
                {
                    _logger.LogWarning("AddComment: Video {videoId} not found", request.videoId);
                    return NotFound(new { success = false, error = "Video not found" });
                }

                _logger.LogInformation("Video verified: {Title}", video.title);

                // Create comment with videoId as partition key
                var comment = new Comment
                {
                    id = Guid.NewGuid().ToString(),
                    videoId = request.videoId,  // This is the partition key
                    userId = request.userId ?? "Anonymous",
                    text = request.CommentText.Trim(),
                    createdAt = DateTime.UtcNow
                };

                _logger.LogInformation("Creating comment: {CommentId} for video {videoId} with partition key {videoId}", 
                    comment.id, comment.videoId, comment.videoId);
                
                // Use the specialized AddCommentAsync method which handles partition key properly
                var createdComment = await _cosmosService.AddCommentAsync(comment);
                
                _logger.LogInformation("✅ Comment {CommentId} added successfully", createdComment.id);

                return Ok(new 
                { 
                    success = true,
                    message = "Comment added successfully",
                    commentId = createdComment.id,
                    videoId = request.videoId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to video {videoId}: {Message}", 
                    request.videoId, ex.Message);
                return StatusCode(500, new 
                { 
                    success = false, 
                    error = $"Error adding comment: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Delete a comment from a video
        /// </summary>
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(string commentId, [FromQuery] string videoId)
        {
            _logger.LogInformation("🔴 DELETE comment request: CommentId={CommentId}, videoId={videoId}", 
                commentId, videoId);

            // Validate input
            if (string.IsNullOrEmpty(commentId))
            {
                _logger.LogWarning("DeleteComment: CommentId is required");
                return BadRequest(new { success = false, error = "Comment ID is required" });
            }

            if (string.IsNullOrEmpty(videoId))
            {
                _logger.LogWarning("DeleteComment: videoId is required");
                return BadRequest(new { success = false, error = "Video ID is required (partition key)" });
            }

            try
            {
                // Verify video exists (optional but good practice)
                _logger.LogInformation("Verifying video {videoId} exists", videoId);
                var video = await _cosmosService.GetVideoByIdAsync(videoId);

                if (video == null)
                {
                    _logger.LogWarning("DeleteComment: Video {videoId} not found", videoId);
                    return NotFound(new { success = false, error = "Video not found" });
                }

                _logger.LogInformation("Video verified: {Title}", video.title);

                // Delete comment (videoId is the partition key for comments)
                _logger.LogInformation("Deleting comment {CommentId} from video {videoId}", commentId, videoId);
                await _cosmosService.DeleteCommentAsync(commentId, videoId);

                _logger.LogInformation("✅ Comment {CommentId} deleted successfully", commentId);

                return Ok(new 
                { 
                    success = true,
                    message = "Comment deleted successfully",
                    commentId,
                    videoId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId} from video {videoId}: {Message}", 
                    commentId, videoId, ex.Message);
                return StatusCode(500, new 
                { 
                    success = false, 
                    error = $"Error deleting comment: {ex.Message}" 
                });
            }
        }
    }

    /// <summary>
    /// Request model for adding a comment
    /// </summary>
    public class AddCommentRequest
    {
        public string videoId { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public string? userId { get; set; }
    }
}