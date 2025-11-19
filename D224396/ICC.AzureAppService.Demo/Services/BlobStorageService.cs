using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ICC.AzureAppService.Demo.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public BlobStorageService(string connectionString, string containerName)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = containerName;
        }

        // Ensure container exists
        public async Task<BlobContainerClient> GetContainerClientAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();
            return containerClient;
        }

        // Generate a SAS URL for client upload
        public async Task<Uri> GetUploadSasUriAsync(string blobName, TimeSpan validDuration)
        {
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!containerClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("BlobContainerClient cannot generate SAS URI. Please check permissions.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(validDuration)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri;
        }

        // Server-side upload (optional, for testing)
        public async Task UploadBlobAsync(string blobName, Stream stream)
        {
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        public async Task<Uri> GetReadSasUriAsync(string blobName, TimeSpan validDuration)
        {
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(validDuration)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri;
        }

        // Delete blob
        public async Task DeleteBlobAsync(string blobName)
        {
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
