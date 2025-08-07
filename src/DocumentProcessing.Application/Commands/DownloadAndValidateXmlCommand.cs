using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Commands;

public record DownloadAndValidateXmlCommand(string BlobUrl, string BatchId) : ICommand<DownloadAndValidateXmlResult>;

public record DownloadAndValidateXmlResult
{
    public bool Success { get; init; }
    public DocumentBatch? DocumentBatch { get; init; }
    public string? ErrorMessage { get; init; }
}