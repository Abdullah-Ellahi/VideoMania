using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace ICC.AzureAppService.Demo.Pages.Api
{
    public class GetUploadSasModel : PageModel
    {
        private readonly BlobStorageService _blobService;
        private readonly IConfiguration _configuration;

        public GetUploadSasModel(BlobStorageService blobService, IConfiguration configuration)
        {
            _blobService = blobService;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnPostAsync([FromBody] SasTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.FileName))
            {
                return BadRequest(new { error = "FileName is required" });
            }

            try
            {
                // Validate file extension
                var allowedExtensions = new[] { ".mp4", ".webm", ".avi", ".mov", ".mkv", ".flv" };
                var extension = Path.GetExtension(request.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = "Invalid file type. Allowed: MP4, WebM, AVI, MOV, MKV, FLV" });
                }

                // Generate unique blob name
                string blobName = $"{Guid.NewGuid()}_{Path.GetFileName(request.FileName)}";

                // Get SAS validity duration from config (default 30 minutes)
                int sasValidityMinutes = _configuration.GetValue("BlobStorage:SasTokenValidityMinutes", 30);
                TimeSpan validDuration = TimeSpan.FromMinutes(sasValidityMinutes);

                // Generate SAS URI
                Uri sasUri = await _blobService.GetUploadSasUriAsync(blobName, validDuration);

                return new JsonResult(new
                {
                    sasUri = sasUri.ToString(),
                    blobName = blobName,
                    expiresIn = sasValidityMinutes * 60 // in seconds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to generate SAS token: {ex.Message}" });
            }
        }
    }

    public class SasTokenRequest
    {
        public string? FileName { get; set; }
    }
}
