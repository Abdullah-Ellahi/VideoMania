using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace ICC.AzureAppService.Demo.Models
{
    public class Video
    {
        [BindProperty(SupportsGet = true)]
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;
        
        [JsonPropertyName("userId")]  // CRITICAL: Must match Cosmos DB partition key path exactly (lowercase)
        public string userId { get; set; } = string.Empty;   // Partition key
        
        [JsonPropertyName("title")]  // Lowercase to match Cosmos DB convention
        public string title { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]  // Lowercase to match Cosmos DB convention
        public string? description { get; set; }
        
        [JsonPropertyName("url")]  // Lowercase to match Cosmos DB convention
        public string url { get; set; } = string.Empty;
        
        [JsonPropertyName("uploadedAt")]  // Lowercase to match Cosmos DB convention
        public DateTime uploadedAt { get; set; }
    }
}