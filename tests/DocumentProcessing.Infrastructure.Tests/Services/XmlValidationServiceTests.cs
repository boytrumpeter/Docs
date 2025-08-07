using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DocumentProcessing.Infrastructure.Services;

namespace DocumentProcessing.Infrastructure.Tests.Services;

public class XmlValidationServiceTests
{
    private readonly Mock<ILogger<XmlValidationService>> _mockLogger;
    private readonly XmlValidationService _service;

    public XmlValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<XmlValidationService>>();
        _service = new XmlValidationService(_mockLogger.Object);
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithValidXml_ShouldReturnSuccess()
    {
        // Arrange
        var validXml = @"
            <Docs>
                <Doc id=""1"">dGVzdCBjb250ZW50</Doc>
                <Doc id=""2"">YW5vdGhlciB0ZXN0</Doc>
            </Docs>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(validXml, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithInvalidXml_ShouldReturnFailure()
    {
        // Arrange
        var invalidXml = "<Docs><Doc id=\"1\">unclosed tag</Docs>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(invalidXml, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("Invalid XML format"));
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithWrongRootElement_ShouldReturnFailure()
    {
        // Arrange
        var xmlWithWrongRoot = @"
            <Documents>
                <Doc id=""1"">dGVzdCBjb250ZW50</Doc>
            </Documents>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(xmlWithWrongRoot, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Expected root element 'Docs', found 'Documents'");
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithNoDocElements_ShouldReturnFailure()
    {
        // Arrange
        var xmlWithoutDocs = "<Docs></Docs>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(xmlWithoutDocs, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("No 'Doc' elements found in the XML");
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithDocMissingId_ShouldReturnFailure()
    {
        // Arrange
        var xmlWithMissingId = @"
            <Docs>
                <Doc>dGVzdCBjb250ZW50</Doc>
            </Docs>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(xmlWithMissingId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Doc element missing required 'id' attribute");
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithEmptyDocContent_ShouldReturnFailure()
    {
        // Arrange
        var xmlWithEmptyDoc = @"
            <Docs>
                <Doc id=""1""></Doc>
            </Docs>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(xmlWithEmptyDoc, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Doc element with id '1' has no content");
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithInvalidBase64_ShouldReturnFailure()
    {
        // Arrange
        var xmlWithInvalidBase64 = @"
            <Docs>
                <Doc id=""1"">invalid-base64-content!</Doc>
            </Docs>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(xmlWithInvalidBase64, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Doc element with id '1' does not contain valid base64 content");
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var xmlWithMultipleErrors = @"
            <Docs>
                <Doc>dGVzdA==</Doc>
                <Doc id=""2""></Doc>
                <Doc id=""3"">invalid-base64!</Doc>
            </Docs>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(xmlWithMultipleErrors, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
        result.Errors.Should().Contain("Doc element missing required 'id' attribute");
        result.Errors.Should().Contain("Doc element with id '2' has no content");
        result.Errors.Should().Contain("Doc element with id '3' does not contain valid base64 content");
    }

    [Fact]
    public async Task ValidateXmlStructureAsync_WithNoRootElement_ShouldReturnFailure()
    {
        // Arrange
        var xmlWithoutRoot = "<?xml version=\"1.0\"?>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(xmlWithoutRoot, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("XML document has no root element");
    }

    [Theory]
    [InlineData("dGVzdA==")] // "test" in base64
    [InlineData("SGVsbG8gV29ybGQ=")] // "Hello World" in base64
    [InlineData("PGRhdGE+dGVzdDwvZGF0YT4=")] // "<data>test</data>" in base64
    public async Task ValidateXmlStructureAsync_WithValidBase64Content_ShouldReturnSuccess(string validBase64)
    {
        // Arrange
        var validXml = $@"
            <Docs>
                <Doc id=""1"">{validBase64}</Doc>
            </Docs>";

        // Act
        var result = await _service.ValidateXmlStructureAsync(validXml, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAgainstSchemaAsync_WithValidXmlAndSchema_ShouldReturnSuccess()
    {
        // Arrange
        var xmlContent = @"<root><element>value</element></root>";
        var schemaContent = @"
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
                <xs:element name=""root"">
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name=""element"" type=""xs:string""/>
                        </xs:sequence>
                    </xs:complexType>
                </xs:element>
            </xs:schema>";

        // Act
        var result = await _service.ValidateAgainstSchemaAsync(xmlContent, schemaContent, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAgainstSchemaAsync_WithInvalidXmlAgainstSchema_ShouldReturnFailure()
    {
        // Arrange
        var xmlContent = @"<root><wrongElement>value</wrongElement></root>";
        var schemaContent = @"
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
                <xs:element name=""root"">
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name=""element"" type=""xs:string""/>
                        </xs:sequence>
                    </xs:complexType>
                </xs:element>
            </xs:schema>";

        // Act
        var result = await _service.ValidateAgainstSchemaAsync(xmlContent, schemaContent, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAgainstSchemaAsync_WithInvalidSchema_ShouldReturnFailure()
    {
        // Arrange
        var xmlContent = @"<root><element>value</element></root>";
        var invalidSchema = @"<invalid>schema</invalid>";

        // Act
        var result = await _service.ValidateAgainstSchemaAsync(xmlContent, invalidSchema, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAgainstSchemaAsync_WhenExceptionOccurs_ShouldReturnFailure()
    {
        // Arrange
        var xmlContent = @"<root><element>value</element></root>";
        var schemaContent = ""; // Empty schema will cause an exception

        // Act
        var result = await _service.ValidateAgainstSchemaAsync(xmlContent, schemaContent, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("Schema validation error"));
    }
}