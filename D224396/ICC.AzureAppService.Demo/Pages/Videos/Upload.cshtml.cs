using ICC.AzureAppService.Demo.Models;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ICC.AzureAppService.Demo.Pages.Videos
{
    public class UploadModel : PageModel
    {
        private readonly CosmosDbService _cosmosService;
        private readonly BlobStorageService _blobService;

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public IFormFile? VideoFile { get; set; }

        public string? Message { get; set; }

        public UploadModel(CosmosDbService cosmosService, BlobStorageService blobService)
        {
            _cosmosService = cosmosService;
            _blobService = blobService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (VideoFile == null || VideoFile.Length == 0)
            {
                Message = "Please select a video file to upload.";
                return Page();
            }

            // Generate a unique blob name
            string blobName = $"{Guid.NewGuid()}_{VideoFile.FileName}";

            // Upload to Blob Storage
            using var stream = VideoFile.OpenReadStream();
            await _blobService.UploadBlobAsync(blobName, stream);

            // Store metadata in Cosmos DB
            var video = new Video
            {
                id = Guid.NewGuid().ToString(),
                UserId = "TestUser", // TODO: replace with actual authenticated user
                Title = Title,
                Description = Description,
                Url = blobName, // save blob name for retrieval
                UploadedAt = DateTime.UtcNow
            };

            await _cosmosService.AddItemAsync(video, "Videos");

            Message = $"Video '{Title}' uploaded successfully!";
            return Page();
        }
    }
}
