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
            _logger.LogInformation("Upload request received");

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    _logger.LogWarning("Upload failed: Title is required");
                    return BadRequest(new { error = "Title is required" });
                }

                if (request.VideoFile == null || request.VideoFile.Length == 0)
                {
                    _logger.LogWarning("Upload failed: Video file is required");
                    return BadRequest(new { error = "Video file is required" });
                }

                _logger.LogInformation($"Processing upload: {request.Title}, File: {request.VideoFile.FileName}, Size: {request.VideoFile.Length}");

                // Validate file size (500MB limit)
                const long maxFileSize = 500 * 1024 * 1024; // 500MB
                if (request.VideoFile.Length > maxFileSize)
                {
                    _logger.LogWarning($"Upload failed: File size {request.VideoFile.Length} exceeds limit");
                    return BadRequest(new { error = "File size exceeds 500MB limit" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".mp4", ".webm", ".avi", ".mov", ".mkv" };
                var extension = Path.GetExtension(request.VideoFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"Upload failed: Invalid file type {extension}");
                    return BadRequest(new { error = "Invalid file type. Allowed: MP4, WebM, AVI, MOV, MKV" });
                }

                // Generate unique blob name
                var blobName = $"{Guid.NewGuid()}{extension}";
                _logger.LogInformation($"Generated blob name: {blobName}");

                // Upload to blob storage
                using var stream = request.VideoFile.OpenReadStream();
                await _blobService.UploadBlobAsync(blobName, stream);
                _logger.LogInformation($"Blob uploaded successfully: {blobName}");

                // Save metadata to Cosmos DB
                var video = new Video
                {
                    id = Guid.NewGuid().ToString(),
                    UserId = "TestUser", // TODO: Replace with actual authenticated user
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim(),
                    Url = blobName,
                    UploadedAt = DateTime.UtcNow
                };

                await _cosmosService.AddItemAsync(video, "Videos");
                _logger.LogInformation($"Video metadata saved: {video.id}");

                return Ok(new 
                { 
                    success = true,
                    message = $"Video '{video.Title}' uploaded successfully!",
                    videoId = video.id,
                    blobName = blobName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video");
                return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
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