using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Commands;
using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Handlers;

public class ProcessDocumentsCommandHandler : ICommandHandler<ProcessDocumentsCommand, ProcessDocumentsResult>
{
    private readonly IDocumentValidationService _documentValidationService;
    private readonly ILogger<ProcessDocumentsCommandHandler> _logger;

    public ProcessDocumentsCommandHandler(
        IDocumentValidationService documentValidationService,
        ILogger<ProcessDocumentsCommandHandler> logger)
    {
        _documentValidationService = documentValidationService;
        _logger = logger;
    }

    public async Task<ProcessDocumentsResult> HandleAsync(ProcessDocumentsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing {DocumentCount} documents for batch {BatchId}",
                request.DocumentBatch.Documents.Count, request.DocumentBatch.BatchId);

            var validCount = 0;
            var invalidCount = 0;

            foreach (var document in request.DocumentBatch.Documents)
            {
                try
                {
                    // Decode base64 content
                    var decodedContent = await DecodeBase64Content(document.EncodedContent, cancellationToken);
                    document.SetDecodedContent(decodedContent);

                    // Validate decoded document
                    var validationResult = await _documentValidationService.ValidateDocumentAsync(decodedContent, cancellationToken);
                    document.SetValidationResult(validationResult.IsValid, validationResult.Errors.ToList(), validationResult.Schema);

                    if (validationResult.IsValid)
                    {
                        validCount++;
                        _logger.LogDebug("Document {DocumentId} validated successfully", document.Id);
                    }
                    else
                    {
                        invalidCount++;
                        _logger.LogWarning("Document {DocumentId} validation failed: {Errors}",
                            document.Id, string.Join(", ", validationResult.Errors));
                    }
                }
                catch (Exception ex)
                {
                    invalidCount++;
                    _logger.LogError(ex, "Error processing document {DocumentId}", document.Id);
                    document.SetValidationResult(false, new List<string> { $"Processing error: {ex.Message}" });
                }
            }

            _logger.LogInformation("Completed processing documents for batch {BatchId}. Valid: {Valid}, Invalid: {Invalid}",
                request.DocumentBatch.BatchId, validCount, invalidCount);

            return new ProcessDocumentsResult
            {
                Success = true,
                ValidDocuments = validCount,
                InvalidDocuments = invalidCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing documents for batch {BatchId}", request.DocumentBatch.BatchId);
            
            return new ProcessDocumentsResult
            {
                Success = false,
                ErrorMessage = $"Error processing documents: {ex.Message}"
            };
        }
    }

    private static async Task<string> DecodeBase64Content(string encodedContent, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // For async consistency
        
        try
        {
            var bytes = Convert.FromBase64String(encodedContent);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Invalid base64 encoded content");
        }
    }
}