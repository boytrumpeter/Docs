namespace DocumentProcessing.Application.Interfaces;

public interface IBlobStorageService
{
    Task<string> DownloadFromUrlAsync(string blobUrl, CancellationToken cancellationToken = default);
    Task<string> StoreXmlAsync(string xmlContent, string batchId, CancellationToken cancellationToken = default);
    Task<string> StoreDocumentAsync(string documentContent, string documentId, string batchId, CancellationToken cancellationToken = default);
}