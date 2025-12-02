# Updated Code Files - Quick Reference

## Complete Updated Files

---

## 1. Controllers/UploadController.cs

```csharp
using ICC.AzureAppService.Demo.Models;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace ICC.AzureAppService.Demo.Controllers
{
    [ApiController]
    [Route("api")]
    public class UploadController : ControllerBase
    {
        private readonly CosmosDbService _cosmosService;
        private readonly BlobStorageService _blobService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(
            BlobStorageService blobService, 
            CosmosDbService cosmosService,
            ILogger<UploadController> logger)
        {
            _blobService = blobService;
            _cosmosService = cosmosService;
            _logger = logger;
        }

        [HttpPost("getuploadSas")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> GetUploadSas([FromForm] UploadVideoRequest request)
        {
            _logger.LogInformation("🔴 Upload request received");

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    _logger.LogWarning("Upload failed: Title is required");
                    return BadRequest(new { success = false, error = "Title is required" });
                }

                if (request.VideoFile == null || request.VideoFile.Length == 0)
                {
                    _logger.LogWarning("Upload failed: Video file is required");
                    return BadRequest(new { success = false, error = "Video file is required" });
                }

                _logger.LogInformation($"Processing upload: {request.Title}, File: {request.VideoFile.FileName}, Size: {request.VideoFile.Length}");

                // Validate file size (500MB limit)
                const long maxFileSize = 500 * 1024 * 1024; // 500MB
                if (request.VideoFile.Length > maxFileSize)
                {
                    _logger.LogWarning($"Upload failed: File size {request.VideoFile.Length} exceeds limit");
                    return BadRequest(new { success = false, error = "File size exceeds 500MB limit" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".mp4", ".webm", ".avi", ".mov", ".mkv" };
                var extension = Path.GetExtension(request.VideoFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"Upload failed: Invalid file type {extension}");
                    return BadRequest(new { success = false, error = "Invalid file type. Allowed: MP4, WebM, AVI, MOV, MKV" });
                }

                // Generate unique blob name
                var blobName = $"{Guid.NewGuid()}{extension}";
                _logger.LogInformation($"Generated blob name: {blobName}");

                // Upload to blob storage
                using var stream = request.VideoFile.OpenReadStream();
                await _blobService.UploadBlobAsync(blobName, stream);
                _logger.LogInformation($"✅ Blob uploaded successfully: {blobName}");

                // Save metadata to Cosmos DB with userId as partition key
                var video = new Video
                {
                    id = Guid.NewGuid().ToString(),
                    userId = "TestUser", // TODO: Replace with actual authenticated user
                    title = request.Title.Trim(),
                    description = request.Description?.Trim(),
                    url = blobName,
                    uploadedAt = DateTime.UtcNow
                };

                await _cosmosService.AddItemAsync(video, "Videos");
                _logger.LogInformation($"✅ Video metadata saved: {video.id}, userId: {video.userId}");

                return Ok(new 
                { 
                    success = true,
                    message = $"Video '{video.title}' uploaded successfully!",
                    videoId = video.id,
                    blobName = blobName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video");
                return StatusCode(500, new { success = false, error = $"Upload failed: {ex.Message}" });
            }
        }
    }

    public class UploadVideoRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IFormFile? VideoFile { get; set; }
    }
}
```

---

## 2. Controllers/CommentsController.cs (Updated)

```csharp
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

                _logger.LogInformation("Creating comment: {CommentId} for video {videoId}", comment.id, comment.videoId);
                await _cosmosService.AddCommentAsync(comment);

                _logger.LogInformation("✅ Comment {CommentId} added successfully", comment.id);

                return Ok(new 
                { 
                    success = true,
                    message = "Comment added successfully",
                    commentId = comment.id,
                    videoId = request.videoId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to video {videoId}", request.videoId);
                return StatusCode(500, new { success = false, error = $"Error adding comment: {ex.Message}" });
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
                return BadRequest(new { success = false, error = "Video ID is required" });
            }

            try
            {
                // Verify video exists
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
                _logger.LogError(ex, "Error deleting comment {CommentId} from video {videoId}", commentId, videoId);
                return StatusCode(500, new { success = false, error = $"Error deleting comment: {ex.Message}" });
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
```

---

## 3. Controllers/VideoController.cs (Complete)

```csharp
using ICC.AzureAppService.Demo.Models;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc;

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
                return BadRequest(new { error = "Video ID is required" });
            }

            try
            {
                // Fetch video to get userId (partition key) and blob name
                var video = await _cosmosService.GetVideoByIdAsync(videoId);
                if (video == null)
                {
                    _logger.LogWarning("DeleteVideo: Video {videoId} not found", videoId);
                    return NotFound(new { error = "Video not found" });
                }

                _logger.LogInformation("Video found: {Title}, userId: {userId}", video.title, video.userId);

                // Delete all comments associated with this video
                _logger.LogInformation("Deleting comments for video {videoId}", videoId);
                var comments = await _cosmosService.GetCommentsAsync(videoId);
                foreach (var comment in comments)
                {
                    if (!string.IsNullOrEmpty(comment?.id))
                    {
                        try
                        {
                            // videoId is the partition key for comments
                            await _cosmosService.DeleteCommentAsync(comment.id, comment.videoId);
                            _logger.LogInformation("Deleted comment {CommentId}", comment.id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error deleting comment {CommentId}", comment.id);
                        }
                    }
                }

                // Delete blob from storage
                if (_blobService != null && !string.IsNullOrEmpty(video.Url))
                {
                    try
                    {
                        // Extract blob name from URL if it's a full URL
                        var blobName = video.Url.Contains("?")
                            ? video.Url.Split('?')[0].Split('/').Last()
                            : video.Url.Split('/').Last();
                        _logger.LogInformation("Deleting blob: {BlobName}", blobName);
                        await _blobService.DeleteBlobAsync(blobName);
                        _logger.LogInformation("✅ Blob deleted successfully: {BlobName}", blobName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting blob for video {videoId}", videoId);
                        // Continue with video deletion even if blob deletion fails
                    }
                }

                // Delete video from Cosmos DB (userId is the partition key for videos)
                _logger.LogInformation("Deleting video {videoId} from database with partition key (userId): {userId}",
                    video.id, video.userId);
                await _cosmosService.DeleteVideoAsync(video.id, video.userId);
                _logger.LogInformation("✅ Video {videoId} deleted successfully", videoId);

                return Ok(new { success = true, message = "Video deleted successfully", videoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting video {videoId}", videoId);
                return StatusCode(500, new { error = $"Error deleting video: {ex.Message}" });
            }
        }
    }
}
```

---

## 4. Services/CosmosDbService.cs (Updated Methods Only)

```csharp
// Delete video - partition key is userId
public async Task DeleteVideoAsync(string id, string userId)
{
    Console.WriteLine($"[CosmosDbService] DeleteVideoAsync called: id={id}, userId={userId}");
    try
    {
        var response = await VideosContainer.DeleteItemAsync<Video>(id, new PartitionKey(userId));
        Console.WriteLine($"[CosmosDbService] ✅ Video deleted successfully, RequestCharge: {response.RequestCharge}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CosmosDbService] ❌ Error deleting video: {ex.Message}");
        throw;
    }
}

// Delete comment - partition key is videoId
public async Task DeleteCommentAsync(string commentId, string videoId)
{
    Console.WriteLine($"[CosmosDbService] DeleteCommentAsync called: commentId={commentId}, videoId={videoId}");
    try
    {
        var response = await CommentsContainer.DeleteItemAsync<Comment>(commentId, new PartitionKey(videoId));
        Console.WriteLine($"[CosmosDbService] ✅ Comment deleted successfully, RequestCharge: {response.RequestCharge}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CosmosDbService] ❌ Error deleting comment: {ex.Message}");
        throw;
    }
}
```

---

## 5. Pages/Videos/Details.cshtml (JavaScript Functions - Updated)

```javascript
async function handleDeleteVideo() {
    console.log('🔴 Deleting video:', videoId);
    
    try {
        const response = await fetch(`/api/videos/${videoId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        console.log('Delete response status:', response.status);
        const data = await response.json();
        console.log('Delete response data:', data);

        if (response.ok && data.success) {
            alert('✅ Video deleted successfully!');
            window.location.href = '/Videos';
        } else {
            alert('❌ Error: ' + (data.error || 'Failed to delete video'));
        }
    } catch (error) {
        console.error('❌ Delete error:', error);
        alert('❌ Error deleting video: ' + error.message);
    }
}

async function handleAddComment(event) {
    event.preventDefault();
    console.log('🔴 Adding comment to video:', videoId);

    const textarea = document.getElementById('commentTextArea');
    const commentText = textarea.value.trim();
    const btn = document.getElementById('commentBtn');

    if (!commentText) {
        alert('Please enter a comment');
        return;
    }

    btn.disabled = true;
    const originalText = btn.innerHTML;
    btn.innerHTML = `
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
        </svg>
        Posting...
    `;

    try {
        const response = await fetch('/api/comments/add', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                videoId: videoId,
                commentText: commentText,
                userId: null
            })
        });

        console.log('Add comment response status:', response.status);
        const data = await response.json();
        console.log('Add comment response data:', data);

        if (response.ok && data.success) {
            console.log('✅ Comment added, reloading page...');
            textarea.value = '';
            btn.innerHTML = originalText;
            btn.disabled = false;
            
            // Reload the page to show the new comment
            setTimeout(() => {
                window.location.reload();
            }, 500);
        } else {
            alert('❌ Error: ' + (data.error || 'Failed to add comment'));
            btn.innerHTML = originalText;
            btn.disabled = false;
        }
    } catch (error) {
        console.error('❌ Add comment error:', error);
        alert('❌ Error adding comment: ' + error.message);
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}

async function handleDeleteComment(commentId) {
    if (!confirm('Delete this comment?')) {
        return;
    }

    console.log('🔴 Deleting comment:', commentId, 'from video:', videoId);
    
    try {
        const response = await fetch(`/api/comments/${commentId}?videoId=${videoId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        console.log('Delete comment response status:', response.status);
        const data = await response.json();
        console.log('Delete comment response data:', data);

        if (response.ok && data.success) {
            console.log('✅ Comment deleted, reloading page...');
            // Remove the comment from the UI
            const commentElement = document.querySelector(`[data-comment-id="${commentId}"]`);
            if (commentElement) {
                commentElement.style.opacity = '0.5';
            }
            
            // Reload the page to refresh comments
            setTimeout(() => {
                window.location.reload();
            }, 500);
        } else {
            alert('❌ Error: ' + (data.error || 'Failed to delete comment'));
        }
    } catch (error) {
        console.error('❌ Delete comment error:', error);
        alert('❌ Error deleting comment: ' + error.message);
    }
}
```

---

## Summary of Changes

| Component | Change | Reason |
|-----------|--------|--------|
| **UploadController** | Added `success` field to responses | Consistency across all APIs |
| **CommentsController** | Added `success` field to all responses | Consistency with upload pattern |
| **CommentsController** | Added comments on partition keys | Clarity for maintenance |
| **VideoController** | Already had good structure, maintained | Keep working code intact |
| **CosmosDbService** | Enhanced logging in delete methods | Better debugging visibility |
| **Details.cshtml JS** | Check `data.success === true` in all handlers | Only proceed on explicit success |

