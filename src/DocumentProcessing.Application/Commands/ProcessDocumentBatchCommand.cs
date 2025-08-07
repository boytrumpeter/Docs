using MediatR;

namespace DocumentProcessing.Application.Commands;

public record ProcessDocumentBatchCommand(string BlobUrl, string BatchId) : IRequest<ProcessDocumentBatchResult>;

public record ProcessDocumentBatchResult
{
    public string BatchId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int ProcessedDocuments { get; init; }
    public int ValidDocuments { get; init; }
    public int InvalidDocuments { get; init; }
}