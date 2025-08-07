using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Commands;

public record SendValidationResultCommand(DocumentBatch DocumentBatch, List<Document> InvalidDocuments) : ICommand<SendValidationResultResult>;

public record SendValidationResultResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}