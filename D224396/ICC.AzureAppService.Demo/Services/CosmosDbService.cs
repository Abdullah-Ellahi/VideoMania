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

        public Container UsersContainer { get; private set; }
        public Container VideosContainer { get; private set; }
        public Container CommentsContainer { get; private set; }

        public CosmosDbService(string account, string key, string databaseName,
                               string usersContainer, string videosContainer, string commentsContainer)
        {
            _cosmosClient = new CosmosClient(account, key);
            _database = _cosmosClient.GetDatabase(databaseName);
            UsersContainer = _database.GetContainer(usersContainer);
            VideosContainer = _database.GetContainer(videosContainer);
            CommentsContainer = _database.GetContainer(commentsContainer);
        }

        // Generic method to create an item
        public async Task<ItemResponse<T>> AddItemAsync<T>(T item, string containerName)
        {
            Container container = GetContainerByName(containerName);
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

        public async Task<Video?> GetVideoAsync(string id)
        {
            var query = VideosContainer.GetItemQueryIterator<Video>(
                new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", id));
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                return response.FirstOrDefault();
            }
            return null;
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
                if (video != null) return video;
            }

            return null;
        }

        // Delete video
        public async Task DeleteVideoAsync(string id, string partitionKey)
        {
            await VideosContainer.DeleteItemAsync<Video>(id, new PartitionKey(partitionKey));
        }

        // Delete comment
        public async Task DeleteCommentAsync(string id, string partitionKey)
        {
            await CommentsContainer.DeleteItemAsync<Comment>(id, new PartitionKey(partitionKey));
        }

        // -------------------------------
        // Comment-specific helpers
        // -------------------------------

        public async Task<List<Comment>> GetCommentsAsync(string videoId)
        {
            var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.VideoId = @videoId")
                .WithParameter("@videoId", videoId);

            var query = CommentsContainer.GetItemQueryIterator<Comment>(queryDefinition);
            List<Comment> results = new List<Comment>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }
    }
}
