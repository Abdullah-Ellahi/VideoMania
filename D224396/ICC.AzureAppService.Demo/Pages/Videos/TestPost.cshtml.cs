using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ICC.AzureAppService.Demo.Pages.Videos
{
    public class TestPostModel : PageModel
    {
        private readonly ILogger<TestPostModel> _logger;

        [BindProperty(SupportsGet = true)]
        public string? id { get; set; }

        public string Message { get; set; } = "No POST yet";

        public TestPostModel(ILogger<TestPostModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("🟢 GET handler called. id = {Id}", id);
            Message = $"GET request received with id={id}";
        }

        public IActionResult OnPost()
        {
            _logger.LogInformation("🔴 POST handler called. id = {Id}", id);
            Message = $"POST request received with id={id}";
            return Page();
        }

        public IActionResult OnPostTestHandler()
        {
            _logger.LogInformation("🔵 POST TestHandler called. id = {Id}", id);
            Message = $"POST TestHandler received with id={id}";
            return Page();
        }
    }
}