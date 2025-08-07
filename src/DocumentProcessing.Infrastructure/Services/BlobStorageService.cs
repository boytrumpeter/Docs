using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Interfaces;

namespace DocumentProcessing.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _internalContainerName;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly HttpClient _httpClient;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<BlobStorageService> logger,
        HttpClient httpClient)
    {
        _blobServiceClient = blobServiceClient;
        _internalContainerName = configuration["BlobStorage:InternalContainerName"] ?? "processed-documents";
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> DownloadFromUrlAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading blob from URL: {BlobUrl}", blobUrl);

            var response = await _httpClient.GetAsync(blobUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogInformation("Successfully downloaded blob content. Size: {Size} characters", content.Length);
            
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading blob from URL: {BlobUrl}", blobUrl);
            throw new InvalidOperationException($"Failed to download blob from URL: {blobUrl}", ex);
        }
    }

    public async Task<string> StoreXmlAsync(string xmlContent, string batchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = await GetOrCreateContainerAsync(cancellationToken);
            var blobName = $"xml/{batchId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}.xml";
            
            _logger.LogInformation("Storing XML content for batch {BatchId} as blob {BlobName}", batchId, blobName);

            var blobClient = containerClient.GetBlobClient(blobName);
            
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

            var blobUrl = blobClient.Uri.ToString();
            
            _logger.LogInformation("Successfully stored XML content for batch {BatchId} at {BlobUrl}", batchId, blobUrl);
            
            return blobUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing XML content for batch {BatchId}", batchId);
            throw new InvalidOperationException($"Failed to store XML content for batch: {batchId}", ex);
        }
    }

    public async Task<string> StoreDocumentAsync(string documentContent, string documentId, string batchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = await GetOrCreateContainerAsync(cancellationToken);
            var blobName = $"documents/{batchId}/{documentId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}.xml";
            
            _logger.LogInformation("Storing document {DocumentId} for batch {BatchId} as blob {BlobName}", 
                documentId, batchId, blobName);

            var blobClient = containerClient.GetBlobClient(blobName);
            
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(documentContent));
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);

            var blobUrl = blobClient.Uri.ToString();
            
            _logger.LogInformation("Successfully stored document {DocumentId} for batch {BatchId} at {BlobUrl}", 
                documentId, batchId, blobUrl);
            
            return blobUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing document {DocumentId} for batch {BatchId}", documentId, batchId);
            throw new InvalidOperationException($"Failed to store document {documentId} for batch: {batchId}", ex);
        }
    }

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_internalContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return containerClient;
    }
}