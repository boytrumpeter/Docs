using FluentAssertions;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Domain.Tests.Entities;

public class DocumentTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateDocument()
    {
        // Arrange
        var id = "test-doc-1";
        var encodedContent = "dGVzdCBjb250ZW50"; // "test content" in base64

        // Act
        var document = new Document(id, encodedContent);

        // Assert
        document.Id.Should().Be(id);
        document.EncodedContent.Should().Be(encodedContent);
        document.DecodedContent.Should().BeNull();
        document.ValidationSchema.Should().BeNull();
        document.IsValid.Should().BeFalse();
        document.ValidationErrors.Should().BeEmpty();
        document.Status.Should().Be(DocumentStatus.Pending);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidId_ShouldThrowArgumentException(string invalidId)
    {
        // Arrange
        var encodedContent = "dGVzdCBjb250ZW50";

        // Act & Assert
        var action = () => new Document(invalidId, encodedContent);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Document ID cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidEncodedContent_ShouldThrowArgumentException(string invalidContent)
    {
        // Arrange
        var id = "test-doc-1";

        // Act & Assert
        var action = () => new Document(id, invalidContent);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Encoded content cannot be null or empty*");
    }

    [Fact]
    public void SetDecodedContent_WithValidContent_ShouldSetContentAndStatus()
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");
        var decodedContent = "<data><name>test</name></data>";

        // Act
        document.SetDecodedContent(decodedContent);

        // Assert
        document.DecodedContent.Should().Be(decodedContent);
        document.Status.Should().Be(DocumentStatus.Decoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetDecodedContent_WithInvalidContent_ShouldThrowArgumentException(string invalidContent)
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");

        // Act & Assert
        var action = () => document.SetDecodedContent(invalidContent);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Decoded content cannot be null or empty*");
    }

    [Fact]
    public void SetValidationResult_WithValidDocument_ShouldSetValidStatus()
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");
        var schema = "TestSchema";

        // Act
        document.SetValidationResult(true, new List<string>(), schema);

        // Assert
        document.IsValid.Should().BeTrue();
        document.ValidationErrors.Should().BeEmpty();
        document.ValidationSchema.Should().Be(schema);
        document.Status.Should().Be(DocumentStatus.Valid);
    }

    [Fact]
    public void SetValidationResult_WithInvalidDocument_ShouldSetInvalidStatus()
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");
        var errors = new List<string> { "Missing required field", "Invalid format" };
        var schema = "TestSchema";

        // Act
        document.SetValidationResult(false, errors, schema);

        // Assert
        document.IsValid.Should().BeFalse();
        document.ValidationErrors.Should().BeEquivalentTo(errors);
        document.ValidationSchema.Should().Be(schema);
        document.Status.Should().Be(DocumentStatus.Invalid);
    }

    [Fact]
    public void SetValidationResult_WithExistingErrors_ShouldReplaceErrors()
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");
        document.SetValidationResult(false, new List<string> { "Old error" });
        
        var newErrors = new List<string> { "New error 1", "New error 2" };

        // Act
        document.SetValidationResult(false, newErrors);

        // Assert
        document.ValidationErrors.Should().BeEquivalentTo(newErrors);
        document.ValidationErrors.Should().NotContain("Old error");
    }

    [Fact]
    public void MarkAsSentToPrint_WithValidDocument_ShouldUpdateStatus()
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");
        document.SetValidationResult(true, new List<string>());

        // Act
        document.MarkAsSentToPrint();

        // Assert
        document.Status.Should().Be(DocumentStatus.SentToPrint);
    }

    [Fact]
    public void MarkAsSentToPrint_WithInvalidDocument_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");
        document.SetValidationResult(false, new List<string> { "Invalid document" });

        // Act & Assert
        var action = () => document.MarkAsSentToPrint();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot send invalid document to print service");
    }

    [Fact]
    public void MarkAsProcessed_ShouldUpdateStatus()
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");

        // Act
        document.MarkAsProcessed();

        // Assert
        document.Status.Should().Be(DocumentStatus.Processed);
    }

    [Fact]
    public void Document_StatusTransitions_ShouldFollowExpectedFlow()
    {
        // Arrange
        var document = new Document("test-doc-1", "dGVzdCBjb250ZW50");

        // Act & Assert - Initial state
        document.Status.Should().Be(DocumentStatus.Pending);

        // Decode content
        document.SetDecodedContent("<data>test</data>");
        document.Status.Should().Be(DocumentStatus.Decoded);

        // Validate successfully
        document.SetValidationResult(true, new List<string>());
        document.Status.Should().Be(DocumentStatus.Valid);

        // Send to print
        document.MarkAsSentToPrint();
        document.Status.Should().Be(DocumentStatus.SentToPrint);

        // Mark as processed
        document.MarkAsProcessed();
        document.Status.Should().Be(DocumentStatus.Processed);
    }
}