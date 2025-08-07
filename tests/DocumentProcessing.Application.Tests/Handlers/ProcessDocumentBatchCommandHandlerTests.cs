using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Application.Commands;
using DocumentProcessing.Application.Handlers;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Tests.Handlers;

public class ProcessDocumentBatchCommandHandlerTests
{
    private readonly Mock<ICommandDispatcher> _mockCommandDispatcher;
    private readonly Mock<ILogger<ProcessDocumentBatchCommandHandler>> _mockLogger;
    private readonly ProcessDocumentBatchCommandHandler _handler;

    public ProcessDocumentBatchCommandHandlerTests()
    {
        _mockCommandDispatcher = new Mock<ICommandDispatcher>();
        _mockLogger = new Mock<ILogger<ProcessDocumentBatchCommandHandler>>();
        _handler = new ProcessDocumentBatchCommandHandler(_mockCommandDispatcher.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithSuccessfulFlow_ShouldReturnSuccessResult()
    {
        // Arrange
        var command = new ProcessDocumentBatchCommand("https://example.com/blob.xml", "batch-123");
        var documentBatch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        
        // Add some documents to the batch
        var validDoc = new Document("doc-1", "validContent");
        var invalidDoc = new Document("doc-2", "invalidContent");
        validDoc.SetValidationResult(true, new List<string>());
        invalidDoc.SetValidationResult(false, new List<string> { "Validation error" });
        documentBatch.AddDocument(validDoc);
        documentBatch.AddDocument(invalidDoc);

        // Setup mock responses
        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAndValidateXmlResult { Success = true, DocumentBatch = documentBatch });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDocumentsResult { Success = true, ValidDocuments = 1, InvalidDocuments = 1 });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<SendValidationResultCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendValidationResultResult { Success = true });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<SendToPrintServiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendToPrintServiceResult { Success = true, DocumentsSent = 1 });

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.BatchId.Should().Be("batch-123");
        result.ProcessedDocuments.Should().Be(2);
        result.ValidDocuments.Should().Be(1);
        result.InvalidDocuments.Should().Be(1);
        result.ErrorMessage.Should().BeNull();

        // Verify all commands were dispatched
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<ProcessDocumentsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<SendValidationResultCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<SendToPrintServiceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenXmlDownloadFails_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new ProcessDocumentBatchCommand("https://example.com/blob.xml", "batch-123");

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAndValidateXmlResult { Success = false, ErrorMessage = "Download failed" });

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.BatchId.Should().Be("batch-123");
        result.ErrorMessage.Should().Be("Download failed");

        // Verify only download command was dispatched
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<ProcessDocumentsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenXmlValidationFailsWithBatch_ShouldSendValidationErrors()
    {
        // Arrange
        var command = new ProcessDocumentBatchCommand("https://example.com/blob.xml", "batch-123");
        var documentBatch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        documentBatch.SetXmlValidationResult(false, new List<string> { "Invalid XML" });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAndValidateXmlResult { Success = false, DocumentBatch = documentBatch, ErrorMessage = "XML validation failed" });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<SendValidationResultCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendValidationResultResult { Success = true });

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("XML validation failed");

        // Verify validation result was sent
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<SendValidationResultCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenDocumentProcessingFails_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new ProcessDocumentBatchCommand("https://example.com/blob.xml", "batch-123");
        var documentBatch = new DocumentBatch("batch-123", "https://example.com/blob.xml");

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAndValidateXmlResult { Success = true, DocumentBatch = documentBatch });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDocumentsResult { Success = false, ErrorMessage = "Document processing failed" });

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Document processing failed");
        result.ProcessedDocuments.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WithOnlyInvalidDocuments_ShouldSendValidationResultsOnly()
    {
        // Arrange
        var command = new ProcessDocumentBatchCommand("https://example.com/blob.xml", "batch-123");
        var documentBatch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        
        var invalidDoc = new Document("doc-1", "invalidContent");
        invalidDoc.SetValidationResult(false, new List<string> { "Validation error" });
        documentBatch.AddDocument(invalidDoc);

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAndValidateXmlResult { Success = true, DocumentBatch = documentBatch });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDocumentsResult { Success = true, ValidDocuments = 0, InvalidDocuments = 1 });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<SendValidationResultCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendValidationResultResult { Success = true });

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ValidDocuments.Should().Be(0);
        result.InvalidDocuments.Should().Be(1);

        // Verify validation results were sent but no print service call
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<SendValidationResultCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<SendToPrintServiceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithOnlyValidDocuments_ShouldSendToPrintServiceOnly()
    {
        // Arrange
        var command = new ProcessDocumentBatchCommand("https://example.com/blob.xml", "batch-123");
        var documentBatch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        
        var validDoc = new Document("doc-1", "validContent");
        validDoc.SetValidationResult(true, new List<string>());
        documentBatch.AddDocument(validDoc);

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAndValidateXmlResult { Success = true, DocumentBatch = documentBatch });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDocumentsResult { Success = true, ValidDocuments = 1, InvalidDocuments = 0 });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<SendToPrintServiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendToPrintServiceResult { Success = true, DocumentsSent = 1 });

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ValidDocuments.Should().Be(1);
        result.InvalidDocuments.Should().Be(0);

        // Verify print service was called but no validation results sent
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<SendToPrintServiceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCommandDispatcher.Verify(x => x.DispatchAsync(It.IsAny<SendValidationResultCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPrintServiceFails_ShouldLogWarningButContinue()
    {
        // Arrange
        var command = new ProcessDocumentBatchCommand("https://example.com/blob.xml", "batch-123");
        var documentBatch = new DocumentBatch("batch-123", "https://example.com/blob.xml");
        
        var validDoc = new Document("doc-1", "validContent");
        validDoc.SetValidationResult(true, new List<string>());
        documentBatch.AddDocument(validDoc);

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAndValidateXmlResult { Success = true, DocumentBatch = documentBatch });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDocumentsResult { Success = true, ValidDocuments = 1, InvalidDocuments = 0 });

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<SendToPrintServiceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendToPrintServiceResult { Success = false, ErrorMessage = "Print service error" });

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); // Overall processing still succeeds
        result.ValidDocuments.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WhenExceptionThrown_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new ProcessDocumentBatchCommand("https://example.com/blob.xml", "batch-123");

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<DownloadAndValidateXmlCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.BatchId.Should().Be("batch-123");
        result.ErrorMessage.Should().Contain("Unexpected error");
    }
}