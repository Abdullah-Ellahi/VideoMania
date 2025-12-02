using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;

namespace Company.Function.Services;

public class BlobStorageServiceForFunctions
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageServiceForFunctions> _logger;
    private readonly string _videoContainer = "videos";

    public BlobStorageServiceForFunctions(string connectionString, ILogger<BlobStorageServiceForFunctions> logger)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    /// <summary>
    /// Get container client
    /// </summary>
    public async Task<BlobContainerClient> GetContainerClientAsync(string containerName = null)
    {
        var container = containerName ?? _videoContainer;
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync();
        return containerClient;
    }

    /// <summary>
    /// Upload processed file to blob storage
    /// </summary>
    public async Task<string> UploadProcessedBlobAsync(string localFilePath, string containerName, string blobName)
    {
        try
        {
            var containerClient = await GetContainerClientAsync(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            using (var fileStream = File.OpenRead(localFilePath))
            {
                await blobClient.UploadAsync(fileStream, overwrite: true);
            }
            
            _logger.LogInformation("✓ Processed blob uploaded: {container}/{blobName}", containerName, blobName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload processed blob: {blobName}", blobName);
            return null;
        }
    }

    /// <summary>
    /// Generate SAS URL for processed content (read access)
    /// </summary>
    public async Task<Uri> GetReadSasUriAsync(string containerName, string blobName, TimeSpan validDuration)
    {
        try
        {
            var containerClient = await GetContainerClientAsync(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!containerClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Cannot generate SAS URI. Check storage account permissions.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(validDuration)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            _logger.LogInformation("✓ SAS URL generated for: {blobName}", blobName);
            return sasUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URL for: {blobName}", blobName);
            return null;
        }
    }

    /// <summary>
    /// Check if blob exists
    /// </summary>
    public async Task<bool> BlobExistsAsync(string containerName, string blobName)
    {
        try
        {
            var containerClient = await GetContainerClientAsync(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking blob existence: {blobName}", blobName);
            return false;
        }
    }
}
