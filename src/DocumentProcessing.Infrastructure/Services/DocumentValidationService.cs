using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Infrastructure.Services;

public class DocumentValidationService : IDocumentValidationService
{
    private readonly ILogger<DocumentValidationService> _logger;
    private readonly IXmlValidationService _xmlValidationService;

    public DocumentValidationService(
        ILogger<DocumentValidationService> logger,
        IXmlValidationService xmlValidationService)
    {
        _logger = logger;
        _xmlValidationService = xmlValidationService;
    }

    public async Task<ValidationResult> ValidateDocumentAsync(string documentContent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating document content");

            var errors = new List<string>();

            // Check if content is valid XML
            XDocument xmlDoc;
            try
            {
                xmlDoc = XDocument.Parse(documentContent);
            }
            catch (XmlException ex)
            {
                _logger.LogWarning("Document contains invalid XML: {Error}", ex.Message);
                return ValidationResult.Failure($"Invalid XML format: {ex.Message}");
            }

            // Detect schema from namespace
            var detectedSchema = await DetectSchemaAsync(documentContent, cancellationToken);
            
            // Basic structural validation
            if (xmlDoc.Root == null)
            {
                errors.Add("Document has no root element");
            }
            else
            {
                // Validate based on detected schema or general rules
                if (!string.IsNullOrEmpty(detectedSchema))
                {
                    _logger.LogDebug("Detected schema: {Schema}", detectedSchema);
                    
                    // Add schema-specific validation here
                    // For now, just basic validation
                    ValidateBasicStructure(xmlDoc, errors);
                }
                else
                {
                    // General validation rules
                    ValidateBasicStructure(xmlDoc, errors);
                }
            }

            if (errors.Any())
            {
                _logger.LogWarning("Document validation failed: {Errors}", string.Join(", ", errors));
                return ValidationResult.Failure(errors, detectedSchema);
            }

            _logger.LogDebug("Document validation successful");
            return ValidationResult.Success(detectedSchema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during document validation");
            return ValidationResult.Failure($"Document validation error: {ex.Message}");
        }
    }

    public async Task<string?> DetectSchemaAsync(string documentContent, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async consistency

        try
        {
            var xmlDoc = XDocument.Parse(documentContent);
            
            // Check for namespace URI which often indicates schema
            var rootNamespace = xmlDoc.Root?.Name.Namespace;
            if (rootNamespace != null && !string.IsNullOrEmpty(rootNamespace.NamespaceName))
            {
                return rootNamespace.NamespaceName;
            }

            // Check for schema location attributes
            var schemaLocation = xmlDoc.Root?.Attribute(XName.Get("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance"));
            if (schemaLocation != null)
            {
                return schemaLocation.Value;
            }

            // Try to infer from root element name
            var rootElementName = xmlDoc.Root?.Name.LocalName;
            return rootElementName switch
            {
                "data" => "DataSchema",
                "document" => "DocumentSchema",
                "invoice" => "InvoiceSchema",
                "order" => "OrderSchema",
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting schema from document");
            return null;
        }
    }

    private static void ValidateBasicStructure(XDocument xmlDoc, List<string> errors)
    {
        // Basic validation rules
        if (xmlDoc.Root == null)
        {
            errors.Add("Document has no root element");
            return;
        }

        // Check for common required elements based on root element
        var rootName = xmlDoc.Root.Name.LocalName.ToLower();
        
        switch (rootName)
        {
            case "data":
                // Validate data structure
                if (!xmlDoc.Root.Elements().Any())
                {
                    errors.Add("Data element must contain child elements");
                }
                break;
                
            case "document":
                // Validate document structure
                ValidateDocumentStructure(xmlDoc.Root, errors);
                break;
                
            default:
                // General validation - ensure it has some content
                if (string.IsNullOrWhiteSpace(xmlDoc.Root.Value) && !xmlDoc.Root.Elements().Any())
                {
                    errors.Add("Document appears to be empty");
                }
                break;
        }
    }

    private static void ValidateDocumentStructure(XElement documentElement, List<string> errors)
    {
        // Example validation for document structure
        var nameElement = documentElement.Element("name");
        if (nameElement == null || string.IsNullOrWhiteSpace(nameElement.Value))
        {
            errors.Add("Document must contain a non-empty 'name' element");
        }

        // Add more specific validation rules as needed
    }
}