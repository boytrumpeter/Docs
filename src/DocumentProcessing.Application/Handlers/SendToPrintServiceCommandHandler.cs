using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Commands;
using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Application.Interfaces;

namespace DocumentProcessing.Application.Handlers;

public class SendToPrintServiceCommandHandler : ICommandHandler<SendToPrintServiceCommand, SendToPrintServiceResult>
{
    private readonly IPrintServiceApiClient _printServiceApiClient;
    private readonly ILogger<SendToPrintServiceCommandHandler> _logger;

    public SendToPrintServiceCommandHandler(
        IPrintServiceApiClient printServiceApiClient,
        ILogger<SendToPrintServiceCommandHandler> logger)
    {
        _printServiceApiClient = printServiceApiClient;
        _logger = logger;
    }

    public async Task<SendToPrintServiceResult> HandleAsync(SendToPrintServiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Sending {DocumentCount} valid documents to print service for batch {BatchId}",
                request.ValidDocuments.Count, request.BatchId);

            var sentCount = 0;
            var errors = new List<string>();

            foreach (var document in request.ValidDocuments)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(document.DecodedContent))
                    {
                        _logger.LogWarning("Document {DocumentId} has no decoded content, skipping", document.Id);
                        continue;
                    }

                    await _printServiceApiClient.SendDocumentAsync(
                        document.Id,
                        document.DecodedContent,
                        request.BatchId,
                        cancellationToken);

                    document.MarkAsSentToPrint();
                    sentCount++;
                    
                    _logger.LogDebug("Document {DocumentId} sent to print service successfully", document.Id);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to send document {document.Id} to print service: {ex.Message}";
                    errors.Add(errorMessage);
                    _logger.LogError(ex, "Error sending document {DocumentId} to print service", document.Id);
                }
            }

            _logger.LogInformation("Completed sending documents to print service for batch {BatchId}. Sent: {Sent}, Errors: {Errors}",
                request.BatchId, sentCount, errors.Count);

            return new SendToPrintServiceResult
            {
                Success = errors.Count == 0,
                DocumentsSent = sentCount,
                ErrorMessage = errors.Any() ? string.Join("; ", errors) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending documents to print service for batch {BatchId}", request.BatchId);
            
            return new SendToPrintServiceResult
            {
                Success = false,
                DocumentsSent = 0,
                ErrorMessage = $"Error sending documents to print service: {ex.Message}"
            };
        }
    }
}