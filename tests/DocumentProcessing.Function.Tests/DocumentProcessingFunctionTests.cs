using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Application.Commands;
using DocumentProcessing.Function;

namespace DocumentProcessing.Function.Tests;

public class DocumentProcessingFunctionTests
{
    private readonly Mock<ILogger<DocumentProcessingFunction>> _mockLogger;
    private readonly Mock<ICommandDispatcher> _mockCommandDispatcher;
    private readonly DocumentProcessingFunction _function;

    public DocumentProcessingFunctionTests()
    {
        _mockLogger = new Mock<ILogger<DocumentProcessingFunction>>();
        _mockCommandDispatcher = new Mock<ICommandDispatcher>();
        _function = new DocumentProcessingFunction(_mockLogger.Object, _mockCommandDispatcher.Object);
    }

    [Fact]
    public async Task ProcessDocumentBatch_WithValidBlobCreatedEvent_ShouldProcessSuccessfully()
    {
        // Arrange
        var eventGridEvent = CreateBlobCreatedEvent("https://example.com/test.xml");
        var expectedResult = new ProcessDocumentBatchResult
        {
            BatchId = "test-batch",
            Success = true,
            ProcessedDocuments = 2,
            ValidDocuments = 1,
            InvalidDocuments = 1
        };

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _function.ProcessDocumentBatch(eventGridEvent, CancellationToken.None);

        // Assert
        _mockCommandDispatcher.Verify(
            x => x.DispatchAsync(
                It.Is<ProcessDocumentBatchCommand>(cmd => 
                    cmd.BlobUrl == "https://example.com/test.xml" && 
                    !string.IsNullOrEmpty(cmd.BatchId)), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessDocumentBatch_WithCustomBatchEvent_ShouldUseProvidedBatchId()
    {
        // Arrange
        var eventGridEvent = CreateCustomBatchEvent("https://example.com/batch.xml", "custom-batch-123");
        var expectedResult = new ProcessDocumentBatchResult
        {
            BatchId = "custom-batch-123",
            Success = true
        };

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _function.ProcessDocumentBatch(eventGridEvent, CancellationToken.None);

        // Assert
        _mockCommandDispatcher.Verify(
            x => x.DispatchAsync(
                It.Is<ProcessDocumentBatchCommand>(cmd => 
                    cmd.BlobUrl == "https://example.com/batch.xml" && 
                    cmd.BatchId == "custom-batch-123"), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessDocumentBatch_WithGenericEvent_ShouldExtractDataCorrectly()
    {
        // Arrange
        var eventGridEvent = CreateGenericEvent("https://example.com/generic.xml", "generic-batch");

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDocumentBatchResult { Success = true });

        // Act
        await _function.ProcessDocumentBatch(eventGridEvent, CancellationToken.None);

        // Assert
        _mockCommandDispatcher.Verify(
            x => x.DispatchAsync(
                It.Is<ProcessDocumentBatchCommand>(cmd => 
                    cmd.BlobUrl == "https://example.com/generic.xml" && 
                    cmd.BatchId == "generic-batch"), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessDocumentBatch_WithInvalidEventData_ShouldNotDispatchCommand()
    {
        // Arrange
        var eventGridEvent = CreateInvalidEvent();

        // Act
        await _function.ProcessDocumentBatch(eventGridEvent, CancellationToken.None);

        // Assert
        _mockCommandDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ProcessDocumentBatch_WhenCommandDispatcherThrows_ShouldPropagateException()
    {
        // Arrange
        var eventGridEvent = CreateBlobCreatedEvent("https://example.com/test.xml");
        
        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Command failed"));

        // Act & Assert
        var act = async () => await _function.ProcessDocumentBatch(eventGridEvent, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Command failed");
    }

    [Fact]
    public async Task ProcessDocumentBatch_WithSuccessfulProcessing_ShouldLogSuccessMessage()
    {
        // Arrange
        var eventGridEvent = CreateBlobCreatedEvent("https://example.com/test.xml");
        var expectedResult = new ProcessDocumentBatchResult
        {
            BatchId = "test-batch",
            Success = true,
            ProcessedDocuments = 3,
            ValidDocuments = 2,
            InvalidDocuments = 1
        };

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _function.ProcessDocumentBatch(eventGridEvent, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully processed batch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDocumentBatch_WithFailedProcessing_ShouldLogErrorMessage()
    {
        // Arrange
        var eventGridEvent = CreateBlobCreatedEvent("https://example.com/test.xml");
        var expectedResult = new ProcessDocumentBatchResult
        {
            BatchId = "test-batch",
            Success = false,
            ErrorMessage = "Processing failed"
        };

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _function.ProcessDocumentBatch(eventGridEvent, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process batch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("Microsoft.Storage.BlobCreated")]
    [InlineData("DocumentProcessing.BatchReceived")]
    [InlineData("Custom.Event.Type")]
    public async Task ProcessDocumentBatch_WithDifferentEventTypes_ShouldHandleCorrectly(string eventType)
    {
        // Arrange
        var eventGridEvent = new EventGridEvent
        {
            Id = Guid.NewGuid().ToString(),
            EventType = eventType,
            Subject = "test-subject",
            EventTime = DateTime.UtcNow,
            Data = JsonSerializer.SerializeToElement(new { blobUrl = "https://example.com/test.xml", batchId = "test-batch" }),
            DataVersion = "1.0",
            MetadataVersion = "1",
            Topic = "test-topic"
        };

        _mockCommandDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDocumentBatchResult { Success = true });

        // Act
        await _function.ProcessDocumentBatch(eventGridEvent, CancellationToken.None);

        // Assert
        _mockCommandDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<ProcessDocumentBatchCommand>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    private static EventGridEvent CreateBlobCreatedEvent(string blobUrl)
    {
        return new EventGridEvent
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "Microsoft.Storage.BlobCreated",
            Subject = "/blobServices/default/containers/uploads/blobs/test.xml",
            EventTime = DateTime.UtcNow,
            Data = JsonSerializer.SerializeToElement(new
            {
                url = blobUrl,
                api = "PutBlob",
                clientRequestId = Guid.NewGuid().ToString(),
                requestId = Guid.NewGuid().ToString(),
                eTag = "0x8D123456789",
                contentType = "application/xml",
                contentLength = 1024,
                blobType = "BlockBlob"
            }),
            DataVersion = "1",
            MetadataVersion = "1",
            Topic = "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Storage/storageAccounts/account"
        };
    }

    private static EventGridEvent CreateCustomBatchEvent(string blobUrl, string batchId)
    {
        return new EventGridEvent
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "DocumentProcessing.BatchReceived",
            Subject = $"batch/{batchId}",
            EventTime = DateTime.UtcNow,
            Data = JsonSerializer.SerializeToElement(new
            {
                blobUrl = blobUrl,
                batchId = batchId,
                source = "APIM",
                timestamp = DateTime.UtcNow
            }),
            DataVersion = "1.0",
            MetadataVersion = "1",
            Topic = "/topics/document-processing"
        };
    }

    private static EventGridEvent CreateGenericEvent(string blobUrl, string batchId)
    {
        return new EventGridEvent
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "Generic.Event",
            Subject = "generic-subject",
            EventTime = DateTime.UtcNow,
            Data = JsonSerializer.SerializeToElement(new
            {
                blobUrl = blobUrl,
                batchId = batchId,
                additionalData = "some value"
            }),
            DataVersion = "1.0",
            MetadataVersion = "1",
            Topic = "/topics/generic"
        };
    }

    private static EventGridEvent CreateInvalidEvent()
    {
        return new EventGridEvent
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "Invalid.Event",
            Subject = "invalid-subject",
            EventTime = DateTime.UtcNow,
            Data = JsonSerializer.SerializeToElement(new
            {
                invalidProperty = "invalid value"
                // Missing blobUrl
            }),
            DataVersion = "1.0",
            MetadataVersion = "1",
            Topic = "/topics/invalid"
        };
    }
}