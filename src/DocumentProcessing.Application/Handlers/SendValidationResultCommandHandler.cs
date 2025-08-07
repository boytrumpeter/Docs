using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Commands;
using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Application.Interfaces;

namespace DocumentProcessing.Application.Handlers;

public class SendValidationResultCommandHandler : ICommandHandler<SendValidationResultCommand, SendValidationResultResult>
{
    private readonly IThirdPartyApiService _thirdPartyApiService;
    private readonly ILogger<SendValidationResultCommandHandler> _logger;

    public SendValidationResultCommandHandler(
        IThirdPartyApiService thirdPartyApiService,
        ILogger<SendValidationResultCommandHandler> logger)
    {
        _thirdPartyApiService = thirdPartyApiService;
        _logger = logger;
    }

    public async Task<SendValidationResultResult> HandleAsync(SendValidationResultCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Sending validation results for batch {BatchId} with {InvalidCount} invalid documents",
                request.DocumentBatch.BatchId, request.InvalidDocuments.Count);

            // Send batch-level validation errors (XML validation)
            if (!request.DocumentBatch.IsXmlValid)
            {
                await _thirdPartyApiService.SendValidationResultAsync(
                    request.DocumentBatch.BatchId,
                    "XML_VALIDATION_ERROR",
                    request.DocumentBatch.XmlValidationErrors,
                    cancellationToken);
            }

            // Send document-level validation errors
            foreach (var invalidDocument in request.InvalidDocuments)
            {
                await _thirdPartyApiService.SendValidationResultAsync(
                    request.DocumentBatch.BatchId,
                    $"DOCUMENT_VALIDATION_ERROR_{invalidDocument.Id}",
                    invalidDocument.ValidationErrors,
                    cancellationToken);
            }

            _logger.LogInformation("Successfully sent validation results for batch {BatchId}", 
                request.DocumentBatch.BatchId);

            return new SendValidationResultResult
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending validation results for batch {BatchId}", 
                request.DocumentBatch.BatchId);
            
            return new SendValidationResultResult
            {
                Success = false,
                ErrorMessage = $"Error sending validation results: {ex.Message}"
            };
        }
    }
}