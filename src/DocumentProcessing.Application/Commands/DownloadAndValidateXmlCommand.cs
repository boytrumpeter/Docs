using MediatR;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Commands;

public record DownloadAndValidateXmlCommand(string BlobUrl, string BatchId) : IRequest<DownloadAndValidateXmlResult>;

public record DownloadAndValidateXmlResult
{
    public bool Success { get; init; }
    public DocumentBatch? DocumentBatch { get; init; }
    public string? ErrorMessage { get; init; }
}