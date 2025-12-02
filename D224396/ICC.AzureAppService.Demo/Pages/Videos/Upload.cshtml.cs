using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ICC.AzureAppService.Demo.Pages.Videos
{
    public class UploadModel : PageModel
    {
        // This page model is now just for rendering the upload form
        // All upload logic is handled by the UploadController API
        
        public void OnGet()
        {
            // Just render the page
        }
    }
}