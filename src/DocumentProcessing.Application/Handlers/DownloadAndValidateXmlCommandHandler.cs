using MediatR;
using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Commands;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.Entities;
using System.Xml.Linq;

namespace DocumentProcessing.Application.Handlers;

public class DownloadAndValidateXmlCommandHandler : IRequestHandler<DownloadAndValidateXmlCommand, DownloadAndValidateXmlResult>
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IXmlValidationService _xmlValidationService;
    private readonly ILogger<DownloadAndValidateXmlCommandHandler> _logger;

    public DownloadAndValidateXmlCommandHandler(
        IBlobStorageService blobStorageService,
        IXmlValidationService xmlValidationService,
        ILogger<DownloadAndValidateXmlCommandHandler> logger)
    {
        _blobStorageService = blobStorageService;
        _xmlValidationService = xmlValidationService;
        _logger = logger;
    }

    public async Task<DownloadAndValidateXmlResult> Handle(DownloadAndValidateXmlCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Downloading XML blob from {BlobUrl} for batch {BatchId}", 
                request.BlobUrl, request.BatchId);

            // Create document batch
            var documentBatch = new DocumentBatch(request.BatchId, request.BlobUrl);

            // Download XML content from third party blob
            var xmlContent = await _blobStorageService.DownloadFromUrlAsync(request.BlobUrl, cancellationToken);
            documentBatch.SetXmlContent(xmlContent);

            // Store in internal blob storage
            var internalBlobUrl = await _blobStorageService.StoreXmlAsync(
                xmlContent, 
                request.BatchId, 
                cancellationToken);
            documentBatch.SetInternalBlobUrl(internalBlobUrl);

            // Validate XML structure
            var validationResult = await _xmlValidationService.ValidateXmlStructureAsync(xmlContent, cancellationToken);
            documentBatch.SetXmlValidationResult(validationResult.IsValid, validationResult.Errors.ToList());

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("XML validation failed for batch {BatchId}: {Errors}", 
                    request.BatchId, string.Join(", ", validationResult.Errors));

                return new DownloadAndValidateXmlResult
                {
                    Success = false,
                    DocumentBatch = documentBatch,
                    ErrorMessage = $"XML validation failed: {string.Join(", ", validationResult.Errors)}"
                };
            }

            // Parse documents from XML
            try
            {
                var xmlDoc = XDocument.Parse(xmlContent);
                var docElements = xmlDoc.Descendants("Doc");

                foreach (var docElement in docElements)
                {
                    var idAttribute = docElement.Attribute("id")?.Value;
                    var encodedContent = docElement.Value?.Trim();

                    if (!string.IsNullOrWhiteSpace(idAttribute) && !string.IsNullOrWhiteSpace(encodedContent))
                    {
                        var document = new Document(idAttribute, encodedContent);
                        documentBatch.AddDocument(document);
                    }
                }

                _logger.LogInformation("Successfully parsed {DocumentCount} documents from batch {BatchId}", 
                    documentBatch.Documents.Count, request.BatchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse documents from XML for batch {BatchId}", request.BatchId);
                
                documentBatch.SetXmlValidationResult(false, new List<string> { $"Failed to parse documents: {ex.Message}" });
                
                return new DownloadAndValidateXmlResult
                {
                    Success = false,
                    DocumentBatch = documentBatch,
                    ErrorMessage = $"Failed to parse documents from XML: {ex.Message}"
                };
            }

            return new DownloadAndValidateXmlResult
            {
                Success = true,
                DocumentBatch = documentBatch
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading and validating XML for batch {BatchId}", request.BatchId);
            
            return new DownloadAndValidateXmlResult
            {
                Success = false,
                ErrorMessage = $"Error downloading and validating XML: {ex.Message}"
            };
        }
    }
}