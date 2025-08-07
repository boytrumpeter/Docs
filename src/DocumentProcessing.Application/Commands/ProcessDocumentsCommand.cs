using MediatR;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Commands;

public record ProcessDocumentsCommand(DocumentBatch DocumentBatch) : IRequest<ProcessDocumentsResult>;

public record ProcessDocumentsResult
{
    public bool Success { get; init; }
    public int ValidDocuments { get; init; }
    public int InvalidDocuments { get; init; }
    public string? ErrorMessage { get; init; }
}