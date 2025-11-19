using Microsoft.AspNetCore.Mvc;

namespace ICC.AzureAppService.Demo.Models
{
    public class User
    {
        [BindProperty(SupportsGet = true)]
        public string? id { get; set; }   // Partition key
        required public string Name { get; set; }
        required public string Email { get; set; }
    }
}
