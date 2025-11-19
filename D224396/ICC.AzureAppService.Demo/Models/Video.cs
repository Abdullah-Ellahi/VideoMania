using Microsoft.AspNetCore.Mvc;

namespace ICC.AzureAppService.Demo.Models
{
    public class Video
    {
        [BindProperty(SupportsGet = true)]
        public string? id { get; set; }
        required public string UserId { get; set; }   // Partition key
        required public string Title { get; set; }
        public string? Description { get; set; }
        required public string Url { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
