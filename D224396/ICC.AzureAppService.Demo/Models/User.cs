using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace ICC.AzureAppService.Demo.Models
{
    public class User
    {
        [BindProperty(SupportsGet = true)]
        [JsonPropertyName("id")]
        public string? id { get; set; }   // Partition key
        
        [JsonPropertyName("name")]  // Lowercase to match Cosmos DB convention
        public string name { get; set; } = string.Empty;
        
        [JsonPropertyName("email")]  // Lowercase to match Cosmos DB convention
        public string email { get; set; } = string.Empty;
    }
}