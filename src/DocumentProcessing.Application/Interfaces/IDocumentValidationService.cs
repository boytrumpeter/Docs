using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Application.Interfaces;

public interface IDocumentValidationService
{
    Task<ValidationResult> ValidateDocumentAsync(string documentContent, CancellationToken cancellationToken = default);
    Task<string?> DetectSchemaAsync(string documentContent, CancellationToken cancellationToken = default);
}