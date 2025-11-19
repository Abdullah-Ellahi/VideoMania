using Microsoft.AspNetCore.Mvc;

namespace ICC.AzureAppService.Demo.Models
{
    public class Comment
    {
        [BindProperty(SupportsGet = true)]
        public string? id { get; set; }
        required public string VideoId { get; set; }  // Partition key
        required public string UserId { get; set; }
        public string? Text { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
