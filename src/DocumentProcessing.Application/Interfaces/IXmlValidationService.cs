using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Application.Interfaces;

public interface IXmlValidationService
{
    Task<ValidationResult> ValidateXmlStructureAsync(string xmlContent, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateAgainstSchemaAsync(string xmlContent, string schemaContent, CancellationToken cancellationToken = default);
}