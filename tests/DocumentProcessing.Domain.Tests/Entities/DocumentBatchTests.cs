using FluentAssertions;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Domain.Tests.Entities;

public class DocumentBatchTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateDocumentBatch()
    {
        // Arrange
        var batchId = "batch-123";
        var sourceBlobUrl = "https://example.com/blob.xml";

        // Act
        var batch = new DocumentBatch(batchId, sourceBlobUrl);

        // Assert
        batch.BatchId.Should().Be(batchId);
        batch.SourceBlobUrl.Should().Be(sourceBlobUrl);
        batch.InternalBlobUrl.Should().BeNull();
        batch.RawXmlContent.Should().BeNull();
        batch.Documents.Should().BeEmpty();
        batch.IsXmlValid.Should().BeFalse();
        batch.XmlValidationErrors.Should().BeEmpty();
        batch.Status.Should().Be(BatchStatus.Received);
        batch.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        batch.ProcessedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidBatchId_ShouldThrowArgumentException(string invalidBatchId)
    {
        // Arrange
        var sourceBlobUrl = "https://example.com/blob.xml";

        // Act & Assert
        var action = () => new DocumentBatch(invalidBatchId, sourceBlobUrl);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Batch ID cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidSourceBlobUrl_ShouldThrowArgumentException(string invalidUrl)
    {
        // Arrange
        var batchId = "batch-123";

        // Act & Assert
        var action = () => new DocumentBatch(batchId, invalidUrl);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Source blob URL cannot be null or empty*");
    }

    [Fact]
    public void SetXmlContent_WithValidContent_ShouldSetContentAndStatus()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var xmlContent = "<Docs><Doc id=\"1\">content</Doc></Docs>";

        // Act
        batch.SetXmlContent(xmlContent);

        // Assert
        batch.RawXmlContent.Should().Be(xmlContent);
        batch.Status.Should().Be(BatchStatus.Downloaded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetXmlContent_WithInvalidContent_ShouldThrowArgumentException(string invalidContent)
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");

        // Act & Assert
        var action = () => batch.SetXmlContent(invalidContent);
        action.Should().Throw<ArgumentException>()
            .WithMessage("XML content cannot be null or empty*");
    }

    [Fact]
    public void SetInternalBlobUrl_WithValidUrl_ShouldSetUrlAndStatus()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var internalUrl = "https://internal.storage.com/batch-123.xml";

        // Act
        batch.SetInternalBlobUrl(internalUrl);

        // Assert
        batch.InternalBlobUrl.Should().Be(internalUrl);
        batch.Status.Should().Be(BatchStatus.Stored);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetInternalBlobUrl_WithInvalidUrl_ShouldThrowArgumentException(string invalidUrl)
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");

        // Act & Assert
        var action = () => batch.SetInternalBlobUrl(invalidUrl);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Internal blob URL cannot be null or empty*");
    }

    [Fact]
    public void SetXmlValidationResult_WithValidXml_ShouldSetValidStatus()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");

        // Act
        batch.SetXmlValidationResult(true, new List<string>());

        // Assert
        batch.IsXmlValid.Should().BeTrue();
        batch.XmlValidationErrors.Should().BeEmpty();
        batch.Status.Should().Be(BatchStatus.XmlValid);
    }

    [Fact]
    public void SetXmlValidationResult_WithInvalidXml_ShouldSetInvalidStatus()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var errors = new List<string> { "Invalid XML format", "Missing root element" };

        // Act
        batch.SetXmlValidationResult(false, errors);

        // Assert
        batch.IsXmlValid.Should().BeFalse();
        batch.XmlValidationErrors.Should().BeEquivalentTo(errors);
        batch.Status.Should().Be(BatchStatus.XmlInvalid);
    }

    [Fact]
    public void SetXmlValidationResult_WithExistingErrors_ShouldReplaceErrors()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        batch.SetXmlValidationResult(false, new List<string> { "Old error" });
        
        var newErrors = new List<string> { "New error 1", "New error 2" };

        // Act
        batch.SetXmlValidationResult(false, newErrors);

        // Assert
        batch.XmlValidationErrors.Should().BeEquivalentTo(newErrors);
        batch.XmlValidationErrors.Should().NotContain("Old error");
    }

    [Fact]
    public void AddDocument_WithValidDocument_ShouldAddToCollection()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var document = new Document("doc-1", "encodedContent");

        // Act
        batch.AddDocument(document);

        // Assert
        batch.Documents.Should().Contain(document);
        batch.Documents.Should().HaveCount(1);
    }

    [Fact]
    public void AddDocument_WithNullDocument_ShouldThrowArgumentNullException()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");

        // Act & Assert
        var action = () => batch.AddDocument(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddDocument_WithMultipleDocuments_ShouldAddAllToCollection()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var doc1 = new Document("doc-1", "encodedContent1");
        var doc2 = new Document("doc-2", "encodedContent2");

        // Act
        batch.AddDocument(doc1);
        batch.AddDocument(doc2);

        // Assert
        batch.Documents.Should().HaveCount(2);
        batch.Documents.Should().Contain(doc1);
        batch.Documents.Should().Contain(doc2);
    }

    [Fact]
    public void MarkAsProcessed_ShouldSetStatusAndProcessedTime()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var beforeProcessing = DateTime.UtcNow;

        // Act
        batch.MarkAsProcessed();

        // Assert
        batch.Status.Should().Be(BatchStatus.Processed);
        batch.ProcessedAt.Should().NotBeNull();
        batch.ProcessedAt.Should().BeOnOrAfter(beforeProcessing);
        batch.ProcessedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void HasValidDocuments_WithValidDocuments_ShouldReturnTrue()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var validDoc = new Document("doc-1", "encodedContent");
        var invalidDoc = new Document("doc-2", "encodedContent");
        
        validDoc.SetValidationResult(true, new List<string>());
        invalidDoc.SetValidationResult(false, new List<string> { "Error" });
        
        batch.AddDocument(validDoc);
        batch.AddDocument(invalidDoc);

        // Act & Assert
        batch.HasValidDocuments.Should().BeTrue();
    }

    [Fact]
    public void HasValidDocuments_WithNoValidDocuments_ShouldReturnFalse()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var invalidDoc = new Document("doc-1", "encodedContent");
        invalidDoc.SetValidationResult(false, new List<string> { "Error" });
        batch.AddDocument(invalidDoc);

        // Act & Assert
        batch.HasValidDocuments.Should().BeFalse();
    }

    [Fact]
    public void HasInvalidDocuments_WithInvalidDocuments_ShouldReturnTrue()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var validDoc = new Document("doc-1", "encodedContent");
        var invalidDoc = new Document("doc-2", "encodedContent");
        
        validDoc.SetValidationResult(true, new List<string>());
        invalidDoc.SetValidationResult(false, new List<string> { "Error" });
        
        batch.AddDocument(validDoc);
        batch.AddDocument(invalidDoc);

        // Act & Assert
        batch.HasInvalidDocuments.Should().BeTrue();
    }

    [Fact]
    public void HasInvalidDocuments_WithNoInvalidDocuments_ShouldReturnFalse()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var validDoc = new Document("doc-1", "encodedContent");
        validDoc.SetValidationResult(true, new List<string>());
        batch.AddDocument(validDoc);

        // Act & Assert
        batch.HasInvalidDocuments.Should().BeFalse();
    }

    [Fact]
    public void ValidDocumentCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var validDoc1 = new Document("doc-1", "encodedContent");
        var validDoc2 = new Document("doc-2", "encodedContent");
        var invalidDoc = new Document("doc-3", "encodedContent");
        
        validDoc1.SetValidationResult(true, new List<string>());
        validDoc2.SetValidationResult(true, new List<string>());
        invalidDoc.SetValidationResult(false, new List<string> { "Error" });
        
        batch.AddDocument(validDoc1);
        batch.AddDocument(validDoc2);
        batch.AddDocument(invalidDoc);

        // Act & Assert
        batch.ValidDocumentCount.Should().Be(2);
    }

    [Fact]
    public void InvalidDocumentCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        var validDoc = new Document("doc-1", "encodedContent");
        var invalidDoc1 = new Document("doc-2", "encodedContent");
        var invalidDoc2 = new Document("doc-3", "encodedContent");
        
        validDoc.SetValidationResult(true, new List<string>());
        invalidDoc1.SetValidationResult(false, new List<string> { "Error1" });
        invalidDoc2.SetValidationResult(false, new List<string> { "Error2" });
        
        batch.AddDocument(validDoc);
        batch.AddDocument(invalidDoc1);
        batch.AddDocument(invalidDoc2);

        // Act & Assert
        batch.InvalidDocumentCount.Should().Be(2);
    }

    [Fact]
    public void DocumentBatch_StatusTransitions_ShouldFollowExpectedFlow()
    {
        // Arrange
        var batch = new DocumentBatch("batch-123", "https://example.com/blob.xml");

        // Act & Assert - Initial state
        batch.Status.Should().Be(BatchStatus.Received);

        // Download XML content
        batch.SetXmlContent("<Docs></Docs>");
        batch.Status.Should().Be(BatchStatus.Downloaded);

        // Store internally
        batch.SetInternalBlobUrl("https://internal.com/blob.xml");
        batch.Status.Should().Be(BatchStatus.Stored);

        // Validate XML successfully
        batch.SetXmlValidationResult(true, new List<string>());
        batch.Status.Should().Be(BatchStatus.XmlValid);

        // Mark as processed
        batch.MarkAsProcessed();
        batch.Status.Should().Be(BatchStatus.Processed);
    }
}