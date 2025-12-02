using ICC.AzureAppService.Demo.Models;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ICC.AzureAppService.Demo.Pages.Videos
{
    public class DetailsModel : PageModel
    {
        private readonly CosmosDbService? _cosmosService;
        private readonly BlobStorageService? _blobService;

        [BindProperty(SupportsGet = true)]
        public string? id { get; set; }

        public Video? Video { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();

        public DetailsModel(CosmosDbService? cosmosService, BlobStorageService? blobService)
        {
            _cosmosService = cosmosService;
            _blobService = blobService;
        }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(id) && _cosmosService != null)
            {
                try
                {
                    Video = await _cosmosService.GetVideoByIdAsync(id);
                    Comments = await _cosmosService.GetCommentsAsync(id);

                    if (Video != null && _blobService != null)
                    {
                        try
                        {
                            Video.url = (await _blobService.GetReadSasUriAsync(Video.url, TimeSpan.FromHours(24))).ToString();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error generating SAS URI: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching video details: {ex.Message}");
                }
            }
        }

        // All POST handlers removed - now handled by API controllers
    }
}