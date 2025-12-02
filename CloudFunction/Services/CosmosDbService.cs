using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Company.Function.Services;

public class CosmosDbService
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<CosmosDbService> _logger;
    private readonly string _databaseName = "videomania";

    public CosmosDbService(string connectionString, ILogger<CosmosDbService> logger)
    {
        _cosmosClient = new CosmosClient(connectionString);
        _logger = logger;
    }

    /// <summary>
    /// Get or create a container
    /// </summary>
    public async Task<Container> GetContainerAsync(string containerName)
    {
        var database = _cosmosClient.GetDatabase(_databaseName);
        return database.GetContainer(containerName);
    }

    /// <summary>
    /// Add or update a video document with processing metadata
    /// </summary>
    public async Task<bool> UpdateVideoProcessingAsync(string videoId, VideoProcessingResult processingResult)
    {
        try
        {
            var container = await GetContainerAsync("Videos");
            
            // Fetch existing video document
            var response = await container.ReadItemAsync<dynamic>(videoId, new PartitionKey(videoId));
            var video = response.Resource;
            
            // Add processing metadata
            video["processing"] = new
            {
                processed = true,
                processedAt = DateTime.UtcNow,
                thumbnailUrl = processingResult.ThumbnailUrl,
                resizedVideoUrl = processingResult.ResizedVideoUrl,
                metadata = new
                {
                    duration = processingResult.Duration,
                    width = processingResult.Width,
                    height = processingResult.Height,
                    videoCodec = processingResult.VideoCodec,
                    audioCodec = processingResult.AudioCodec,
                    frameRate = processingResult.FrameRate,
                    bitRate = processingResult.BitRate
                },
                status = "completed"
            };
            
            // Update the document
            await container.UpsertItemAsync(video);
            _logger.LogInformation("✓ Video processing metadata saved for: {videoId}", videoId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update video processing for: {videoId}", videoId);
            return false;
        }
    }

    /// <summary>
    /// Get video by blob name to find its ID
    /// </summary>
    public async Task<string> GetVideoIdByBlobNameAsync(string blobName)
    {
        try
        {
            var container = await GetContainerAsync("Videos");
            
            // Query to find video by url (blob name)
            var query = new QueryDefinition("SELECT c.id FROM c WHERE c.url = @blobName")
                .WithParameter("@blobName", blobName);
            
            using (var iterator = container.GetItemQueryIterator<VideoRecord>(query))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    if (response.Count > 0)
                    {
                        return response.FirstOrDefault()?.id;
                    }
                }
            }
            
            _logger.LogWarning("No video found with blob name: {blobName}", blobName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying video by blob name: {blobName}", blobName);
            return null;
        }
    }
}

public class VideoProcessingResult
{
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string ResizedVideoUrl { get; set; } = string.Empty;
    public double Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string VideoCodec { get; set; } = string.Empty;
    public string AudioCodec { get; set; } = string.Empty;
    public double FrameRate { get; set; }
    public long BitRate { get; set; }
}

public class VideoRecord
{
    public string id { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
}
