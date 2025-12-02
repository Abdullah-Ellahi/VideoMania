using ICC.AzureAppService.Demo.Models;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Threading.Tasks;

namespace ICC.AzureAppService.Demo.Services
{
    public class CosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Database _database;
        private readonly ILogger<CosmosDbService> _logger;

        public Container UsersContainer { get; private set; }
        public Container VideosContainer { get; private set; }
        public Container CommentsContainer { get; private set; }

        public CosmosDbService(
            string account, 
            string key, 
            string databaseName,
            string usersContainer, 
            string videosContainer, 
            string commentsContainer,
            ILogger<CosmosDbService> logger)
        {
            _cosmosClient = new CosmosClient(account, key);
            _database = _cosmosClient.GetDatabase(databaseName);
            UsersContainer = _database.GetContainer(usersContainer);
            VideosContainer = _database.GetContainer(videosContainer);
            CommentsContainer = _database.GetContainer(commentsContainer);
            _logger = logger;
        }

        // Generic method - kept for backward compatibility
        public async Task<ItemResponse<T>> AddItemAsync<T>(T item, string containerName)
        {
            Container container = GetContainerByName(containerName);
            
            if (containerName == "Videos" && item is Video video)
            {
                _logger.LogInformation("Adding video with partition key: {userId}", video.userId);
                // CRITICAL: Pass the exact partition key value that exists in the serialized JSON document
                // The Video model uses lowercase "userId" in JSON (via [JsonPropertyName("userId")])
                // So we pass video.userId but Cosmos expects it to match what's in the document
                return await container.CreateItemAsync(item, new PartitionKey(video.userId));
            }
            else if (containerName == "Comments" && item is Comment comment)
            {
                _logger.LogInformation("Adding comment with partition key: {videoId}", comment.videoId);
                return await container.CreateItemAsync(item, new PartitionKey(comment.videoId));
            }
            
            return await container.CreateItemAsync(item);
        }

        // Generic method to read an item by id & partition key. Returns null when not found.
        public async Task<T?> GetItemAsync<T>(string id, string partitionKey, string containerName)
        {
            Container container = GetContainerByName(containerName);
            try
            {
                ItemResponse<T> response = await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }
        }

        private Container GetContainerByName(string name) => name switch
        {
            "Users" => UsersContainer,
            "Videos" => VideosContainer,
            "Comments" => CommentsContainer,
            _ => throw new System.Exception("Container not found")
        };

        // -------------------------------
        // Video-specific helpers
        // -------------------------------

        public async Task<List<Video>> GetVideosAsync()
        {
            var query = VideosContainer.GetItemQueryIterator<Video>("SELECT * FROM c");
            List<Video> results = new List<Video>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        // Fetch video safely regardless of partition key
        public async Task<Video?> GetVideoByIdAsync(string id)
        {
            var query = VideosContainer.GetItemQueryIterator<Video>(
                new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id));

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                var video = response.FirstOrDefault();
                if (video != null)
                {
                    _logger.LogInformation("Found video: {videoId} with userId: {userId}", video.id, video.userId);
                    return video;
                }
            }

            _logger.LogWarning("Video not found: {videoId}", id);
            return null;
        }

        // Delete video with proper partition key (userId)
        public async Task DeleteVideoAsync(string id, string userId)
        {
            try
            {
                _logger.LogInformation("Attempting to delete video {videoId} with partition key {userId}", id, userId);
                var response = await VideosContainer.DeleteItemAsync<Video>(id, new PartitionKey(userId));
                _logger.LogInformation("Successfully deleted video {videoId} from Cosmos DB", id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Video {videoId} not found in Cosmos DB (StatusCode: {StatusCode})", id, ex.StatusCode);
                throw; // Re-throw so the controller knows it failed
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Cosmos DB error deleting video {videoId}: {Message}", id, ex.Message);
                throw;
            }
        }

        // -------------------------------
        // Comment-specific helpers
        // -------------------------------

        public async Task<List<Comment>> GetCommentsAsync(string videoId)
        {
            _logger.LogInformation("Fetching comments for video: {videoId}", videoId);
            
            // Query using lowercase property name as defined in JsonPropertyName
            var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.videoId = @videoId")
                .WithParameter("@videoId", videoId);

            var query = CommentsContainer.GetItemQueryIterator<Comment>(queryDefinition);
            List<Comment> results = new List<Comment>();
            
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            
            _logger.LogInformation("Found {Count} comments for video {videoId}", results.Count, videoId);
            return results;
        }

        // Add comment with proper partition key (videoId) - matching upload pattern
        public async Task<Comment> AddCommentAsync(Comment comment)
        {
            try
            {
                _logger.LogInformation("Adding comment {commentId} to video {videoId} with partition key {videoId}", 
                    comment.id, comment.videoId, comment.videoId);
                
                // CRITICAL: videoId is the partition key for Comments container
                // This matches the pattern used in AddItemAsync for Videos
                var response = await CommentsContainer.CreateItemAsync(
                    comment,
                    new PartitionKey(comment.videoId)
                );
                
                _logger.LogInformation("Successfully added comment {commentId}", comment.id);
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Cosmos DB error adding comment: StatusCode={StatusCode}, Message={Message}", 
                    ex.StatusCode, ex.Message);
                throw;
            }
        }

        // Delete comment with proper partition key (videoId)
        public async Task DeleteCommentAsync(string commentId, string videoId)
        {
            try
            {
                _logger.LogInformation("Attempting to delete comment {commentId} with partition key {videoId}", 
                    commentId, videoId);
                
                var response = await CommentsContainer.DeleteItemAsync<Comment>(
                    commentId,
                    new PartitionKey(videoId)
                );
                
                _logger.LogInformation("Successfully deleted comment {commentId}", commentId);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Comment {commentId} not found in Cosmos DB (may already be deleted)", commentId);
                // Don't throw - comment is already gone
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Cosmos DB error deleting comment {commentId}: {Message}", commentId, ex.Message);
                throw;
            }
        }
    }
}