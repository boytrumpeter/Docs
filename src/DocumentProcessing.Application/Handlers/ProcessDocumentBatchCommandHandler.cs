using MediatR;
using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Commands;

namespace DocumentProcessing.Application.Handlers;

public class ProcessDocumentBatchCommandHandler : IRequestHandler<ProcessDocumentBatchCommand, ProcessDocumentBatchResult>
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessDocumentBatchCommandHandler> _logger;

    public ProcessDocumentBatchCommandHandler(
        IMediator mediator,
        ILogger<ProcessDocumentBatchCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ProcessDocumentBatchResult> Handle(ProcessDocumentBatchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting processing of document batch {BatchId} from blob {BlobUrl}", 
                request.BatchId, request.BlobUrl);

            // Step 1: Download and validate XML blob
            var xmlResult = await _mediator.Send(
                new DownloadAndValidateXmlCommand(request.BlobUrl, request.BatchId), 
                cancellationToken);

            if (!xmlResult.Success || xmlResult.DocumentBatch == null)
            {
                _logger.LogError("Failed to download or validate XML for batch {BatchId}: {Error}", 
                    request.BatchId, xmlResult.ErrorMessage);

                // Send XML validation errors to third party
                if (xmlResult.DocumentBatch != null)
                {
                    await _mediator.Send(
                        new SendValidationResultCommand(xmlResult.DocumentBatch, new List<Domain.Entities.Document>()), 
                        cancellationToken);
                }

                return new ProcessDocumentBatchResult
                {
                    BatchId = request.BatchId,
                    Success = false,
                    ErrorMessage = xmlResult.ErrorMessage
                };
            }

            var documentBatch = xmlResult.DocumentBatch;

            // Step 2: Process individual documents (decode and validate)
            var documentsResult = await _mediator.Send(
                new ProcessDocumentsCommand(documentBatch), 
                cancellationToken);

            if (!documentsResult.Success)
            {
                _logger.LogError("Failed to process documents for batch {BatchId}: {Error}", 
                    request.BatchId, documentsResult.ErrorMessage);

                return new ProcessDocumentBatchResult
                {
                    BatchId = request.BatchId,
                    Success = false,
                    ErrorMessage = documentsResult.ErrorMessage,
                    ProcessedDocuments = documentBatch.Documents.Count
                };
            }

            // Step 3: Send validation results for invalid documents
            var invalidDocuments = documentBatch.Documents.Where(d => !d.IsValid).ToList();
            if (invalidDocuments.Any())
            {
                await _mediator.Send(
                    new SendValidationResultCommand(documentBatch, invalidDocuments), 
                    cancellationToken);
            }

            // Step 4: Send valid documents to print service
            var validDocuments = documentBatch.Documents.Where(d => d.IsValid).ToList();
            if (validDocuments.Any())
            {
                var printResult = await _mediator.Send(
                    new SendToPrintServiceCommand(validDocuments, request.BatchId), 
                    cancellationToken);

                if (!printResult.Success)
                {
                    _logger.LogWarning("Failed to send some documents to print service for batch {BatchId}: {Error}", 
                        request.BatchId, printResult.ErrorMessage);
                }
            }

            documentBatch.MarkAsProcessed();

            _logger.LogInformation("Completed processing of batch {BatchId}. Valid: {Valid}, Invalid: {Invalid}", 
                request.BatchId, documentsResult.ValidDocuments, documentsResult.InvalidDocuments);

            return new ProcessDocumentBatchResult
            {
                BatchId = request.BatchId,
                Success = true,
                ProcessedDocuments = documentBatch.Documents.Count,
                ValidDocuments = documentsResult.ValidDocuments,
                InvalidDocuments = documentsResult.InvalidDocuments
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing batch {BatchId}", request.BatchId);
            
            return new ProcessDocumentBatchResult
            {
                BatchId = request.BatchId,
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }
}