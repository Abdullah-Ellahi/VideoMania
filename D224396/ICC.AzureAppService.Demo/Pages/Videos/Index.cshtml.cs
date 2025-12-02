using ICC.AzureAppService.Demo.Models;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ICC.AzureAppService.Demo.Pages.Videos
{
    public class IndexModel : PageModel
    {
        private readonly CosmosDbService? _cosmosService;
        public List<Video> Videos { get; set; } = new List<Video>();

        public IndexModel(CosmosDbService? cosmosService)
        {
            _cosmosService = cosmosService;
        }

        public async Task OnGetAsync()
        {
            if (_cosmosService != null)
            {
                try
                {
                    Videos = await _cosmosService.GetVideosAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching videos: {ex.Message}");
                    Videos = new List<Video>();
                }
            }
            else
            {
                // Development mode - show empty list
                Videos = new List<Video>();
            }
        }
    }
}
