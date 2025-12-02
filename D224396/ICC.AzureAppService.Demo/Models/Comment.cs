using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace ICC.AzureAppService.Demo.Models
{
    public class Comment
    {
        [BindProperty(SupportsGet = true)]
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;
        
        // IMPORTANT: This must match your Cosmos DB partition key path exactly
        [JsonPropertyName("videoId")]  // For JSON serialization
        public string videoId { get; set; } = string.Empty;  // Property name for C# code
        
        [JsonPropertyName("userId")]
        public string userId { get; set; } = string.Empty;
        
        [JsonPropertyName("text")]
        public string text { get; set; } = string.Empty;
        
        [JsonPropertyName("createdAt")]
        public DateTime createdAt { get; set; }
    }
}