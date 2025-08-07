using MediatR;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Commands;

public record SendToPrintServiceCommand(List<Document> ValidDocuments, string BatchId) : IRequest<SendToPrintServiceResult>;

public record SendToPrintServiceResult
{
    public bool Success { get; init; }
    public int DocumentsSent { get; init; }
    public string? ErrorMessage { get; init; }
}