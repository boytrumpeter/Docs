using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Infrastructure.Services;

public class XmlValidationService : IXmlValidationService
{
    private readonly ILogger<XmlValidationService> _logger;

    public XmlValidationService(ILogger<XmlValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateXmlStructureAsync(string xmlContent, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async consistency
        
        try
        {
            _logger.LogDebug("Validating XML structure");

            // Check if it's valid XML
            var xmlDoc = XDocument.Parse(xmlContent);
            
            // Check for required root element structure
            var errors = new List<string>();
            
            if (xmlDoc.Root == null)
            {
                errors.Add("XML document has no root element");
                return ValidationResult.Failure(errors);
            }

            // Validate expected structure: <Docs><Doc id="...">base64content</Doc></Docs>
            if (xmlDoc.Root.Name.LocalName != "Docs")
            {
                errors.Add($"Expected root element 'Docs', found '{xmlDoc.Root.Name.LocalName}'");
            }

            var docElements = xmlDoc.Root.Elements("Doc");
            if (!docElements.Any())
            {
                errors.Add("No 'Doc' elements found in the XML");
            }

            foreach (var docElement in docElements)
            {
                var idAttribute = docElement.Attribute("id");
                if (idAttribute == null || string.IsNullOrWhiteSpace(idAttribute.Value))
                {
                    errors.Add($"Doc element missing required 'id' attribute");
                }

                if (string.IsNullOrWhiteSpace(docElement.Value))
                {
                    errors.Add($"Doc element with id '{idAttribute?.Value}' has no content");
                }
                else
                {
                    // Basic check if content looks like base64
                    var content = docElement.Value.Trim();
                    if (content.Length % 4 != 0 || !IsBase64String(content))
                    {
                        errors.Add($"Doc element with id '{idAttribute?.Value}' does not contain valid base64 content");
                    }
                }
            }

            if (errors.Any())
            {
                _logger.LogWarning("XML structure validation failed: {Errors}", string.Join(", ", errors));
                return ValidationResult.Failure(errors);
            }

            _logger.LogDebug("XML structure validation successful");
            return ValidationResult.Success();
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "XML parsing error during validation");
            return ValidationResult.Failure($"Invalid XML format: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during XML validation");
            return ValidationResult.Failure($"XML validation error: {ex.Message}");
        }
    }

    public async Task<ValidationResult> ValidateAgainstSchemaAsync(string xmlContent, string schemaContent, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async consistency

        try
        {
            _logger.LogDebug("Validating XML against provided schema");

            var errors = new List<string>();
            var settings = new XmlReaderSettings();
            
            // Add schema
            using var schemaReader = new StringReader(schemaContent);
            var schema = XmlSchema.Read(schemaReader, (sender, e) => errors.Add(e.Message));
            
            if (schema != null)
            {
                settings.Schemas.Add(schema);
                settings.ValidationType = ValidationType.Schema;
                settings.ValidationEventHandler += (sender, e) => errors.Add(e.Message);

                using var xmlReader = XmlReader.Create(new StringReader(xmlContent), settings);
                
                while (xmlReader.Read()) { } // Read through entire document to trigger validation
            }

            if (errors.Any())
            {
                _logger.LogWarning("XML schema validation failed: {Errors}", string.Join(", ", errors));
                return ValidationResult.Failure(errors);
            }

            _logger.LogDebug("XML schema validation successful");
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during XML schema validation");
            return ValidationResult.Failure($"Schema validation error: {ex.Message}");
        }
    }

    private static bool IsBase64String(string s)
    {
        try
        {
            Convert.FromBase64String(s);
            return true;
        }
        catch
        {
            return false;
        }
    }
}