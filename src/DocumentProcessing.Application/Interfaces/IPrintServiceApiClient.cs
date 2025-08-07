namespace DocumentProcessing.Application.Interfaces;

public interface IPrintServiceApiClient
{
    Task SendDocumentAsync(
        string documentId, 
        string documentContent, 
        string batchId, 
        CancellationToken cancellationToken = default);
}