using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DocumentProcessing.Application.Abstractions;
using DocumentProcessing.Application.Commands;

namespace DocumentProcessing.Function;

public class DocumentProcessingFunction
{
    private readonly ILogger<DocumentProcessingFunction> _logger;
    private readonly ICommandDispatcher _commandDispatcher;

    public DocumentProcessingFunction(
        ILogger<DocumentProcessingFunction> logger,
        ICommandDispatcher commandDispatcher)
    {
        _logger = logger;
        _commandDispatcher = commandDispatcher;
    }

    [Function("ProcessDocumentBatch")]
    public async Task ProcessDocumentBatch(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received Event Grid event: {EventType} from {Subject}", 
                eventGridEvent.EventType, eventGridEvent.Subject);

            // Extract blob URL and batch ID from event data
            var eventData = ParseEventData(eventGridEvent);
            
            if (eventData == null)
            {
                _logger.LogWarning("Could not parse event data from Event Grid event");
                return;
            }

            _logger.LogInformation("Processing document batch {BatchId} from blob {BlobUrl}", 
                eventData.BatchId, eventData.BlobUrl);

            // Dispatch the main processing command
            var command = new ProcessDocumentBatchCommand(eventData.BlobUrl, eventData.BatchId);
            var result = await _commandDispatcher.DispatchAsync(command, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Successfully processed batch {BatchId}. Processed: {Processed}, Valid: {Valid}, Invalid: {Invalid}",
                    result.BatchId, result.ProcessedDocuments, result.ValidDocuments, result.InvalidDocuments);
            }
            else
            {
                _logger.LogError("Failed to process batch {BatchId}: {Error}", 
                    result.BatchId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Event Grid event");
            throw; // Re-throw to trigger function retry if configured
        }
    }

    private EventData? ParseEventData(EventGridEvent eventGridEvent)
    {
        try
        {
            // Handle different event types that might contain blob information
            return eventGridEvent.EventType switch
            {
                "Microsoft.Storage.BlobCreated" => ParseBlobCreatedEvent(eventGridEvent),
                "DocumentProcessing.BatchReceived" => ParseCustomBatchEvent(eventGridEvent),
                _ => ParseGenericEvent(eventGridEvent)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing event data from Event Grid event");
            return null;
        }
    }

    private EventData? ParseBlobCreatedEvent(EventGridEvent eventGridEvent)
    {
        var data = JsonSerializer.Deserialize<BlobCreatedEventData>(eventGridEvent.Data.ToString() ?? "{}");
        
        if (string.IsNullOrEmpty(data?.Url))
        {
            _logger.LogWarning("Blob created event missing URL");
            return null;
        }

        // Extract batch ID from blob path or generate one
        var batchId = ExtractBatchIdFromUrl(data.Url) ?? Guid.NewGuid().ToString();

        return new EventData
        {
            BlobUrl = data.Url,
            BatchId = batchId
        };
    }

    private EventData? ParseCustomBatchEvent(EventGridEvent eventGridEvent)
    {
        var data = JsonSerializer.Deserialize<CustomBatchEventData>(eventGridEvent.Data.ToString() ?? "{}");
        
        if (string.IsNullOrEmpty(data?.BlobUrl) || string.IsNullOrEmpty(data?.BatchId))
        {
            _logger.LogWarning("Custom batch event missing required data");
            return null;
        }

        return new EventData
        {
            BlobUrl = data.BlobUrl,
            BatchId = data.BatchId
        };
    }

    private EventData? ParseGenericEvent(EventGridEvent eventGridEvent)
    {
        // Try to extract blob URL and batch ID from generic event
        var dataJson = eventGridEvent.Data.ToString() ?? "{}";
        using var doc = JsonDocument.Parse(dataJson);
        
        var blobUrl = doc.RootElement.TryGetProperty("blobUrl", out var blobUrlProp) ? blobUrlProp.GetString() :
                     doc.RootElement.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
        
        var batchId = doc.RootElement.TryGetProperty("batchId", out var batchIdProp) ? batchIdProp.GetString() :
                     doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        if (string.IsNullOrEmpty(blobUrl))
        {
            _logger.LogWarning("Could not extract blob URL from generic event");
            return null;
        }

        return new EventData
        {
            BlobUrl = blobUrl,
            BatchId = batchId ?? Guid.NewGuid().ToString()
        };
    }

    private static string? ExtractBatchIdFromUrl(string url)
    {
        // Try to extract batch ID from URL path
        // This is implementation-specific based on your blob naming convention
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // Look for a segment that looks like a batch ID (GUID or specific pattern)
            foreach (var segment in segments)
            {
                if (Guid.TryParse(segment, out _))
                {
                    return segment;
                }
            }
            
            // If no GUID found, use the filename without extension
            var fileName = Path.GetFileNameWithoutExtension(segments.LastOrDefault() ?? "");
            return !string.IsNullOrEmpty(fileName) ? fileName : null;
        }
        catch
        {
            return null;
        }
    }

    private class EventData
    {
        public string BlobUrl { get; set; } = string.Empty;
        public string BatchId { get; set; } = string.Empty;
    }

    private class BlobCreatedEventData
    {
        public string? Url { get; set; }
        public string? Api { get; set; }
        public string? ClientRequestId { get; set; }
        public string? RequestId { get; set; }
        public string? ETag { get; set; }
        public string? ContentType { get; set; }
        public long? ContentLength { get; set; }
        public string? BlobType { get; set; }
    }

    private class CustomBatchEventData
    {
        public string? BlobUrl { get; set; }
        public string? BatchId { get; set; }
        public string? Source { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}

public class EventGridEvent
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public object Data { get; set; } = new();
    public string DataVersion { get; set; } = string.Empty;
    public string MetadataVersion { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}