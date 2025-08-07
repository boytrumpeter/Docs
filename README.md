# Document Processing Service

A clean architecture Azure Function solution for processing XML documents with base64 encoded content, featuring custom command/query/event dispatcher pattern.

## Architecture Overview

This solution implements a clean architecture with the following layers:

- **Domain**: Core business entities, value objects, and domain logic
- **Application**: Use cases, commands, queries, handlers, and interfaces
- **Infrastructure**: External service implementations (Azure Blob Storage, HTTP clients)
- **Function**: Azure Function entry point with Event Grid trigger

## Key Features

- **Event-Driven**: Triggered by Event Grid events (APIM → Event Grid → Azure Function)
- **Clean Architecture**: Separation of concerns with dependency inversion
- **Custom Dispatchers**: ICommandDispatcher, IQueryDispatcher, IEventDispatcher instead of MediatR
- **XML Processing**: Downloads, validates, and processes XML blobs with base64 documents
- **Validation**: XML structure validation and individual document validation
- **Error Handling**: Sends validation errors to third-party API
- **Print Service**: Sends valid documents to print service
- **Blob Storage**: Stores processed content in internal Azure Blob Storage

## Workflow

1. **Event Reception**: Azure Function receives Event Grid event with blob URL
2. **XML Download**: Downloads XML blob from third-party URL
3. **XML Storage**: Stores XML in internal blob storage
4. **XML Validation**: Validates XML structure and schema
5. **Document Processing**: Decodes base64 documents and validates each
6. **Error Reporting**: Sends validation errors to third-party API
7. **Print Processing**: Sends valid documents to print service

## Project Structure

```
src/
├── DocumentProcessing.Domain/           # Domain entities and value objects
│   ├── Entities/
│   │   ├── Document.cs                 # Document aggregate
│   │   ├── DocumentBatch.cs           # Batch aggregate
│   │   ├── DocumentStatus.cs          # Document status enum
│   │   └── BatchStatus.cs             # Batch status enum
│   └── ValueObjects/
│       ├── ValidationResult.cs        # Validation result value object
│       └── BlobReference.cs           # Blob reference value object
├── DocumentProcessing.Application/     # Application layer
│   ├── Abstractions/                  # Command/Query/Event abstractions
│   ├── Commands/                      # Command definitions
│   ├── Handlers/                      # Command handlers
│   ├── Infrastructure/                # Dispatcher implementations
│   └── Interfaces/                    # Service interfaces
├── DocumentProcessing.Infrastructure/  # Infrastructure implementations
│   ├── Services/                      # Service implementations
│   └── ApiClients/                    # HTTP API clients
└── DocumentProcessing.Function/        # Azure Function
    ├── DocumentProcessingFunction.cs   # Main function
    ├── Program.cs                     # DI container setup
    ├── host.json                      # Function configuration
    └── local.settings.json            # Local development settings
```

## Configuration

### Required Settings

#### Azure Storage
- `ConnectionStrings:AzureStorage`: Azure Storage connection string
- `BlobStorage:InternalContainerName`: Container for processed documents

#### Third Party API
- `ThirdPartyApi:BaseUrl`: Base URL for validation result reporting
- `ThirdPartyApi:ApiKey`: API key for authentication

#### Print Service
- `PrintService:BaseUrl`: Base URL for print service
- `PrintService:ApiKey`: API key for authentication

### Example Configuration (local.settings.json)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings:AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...",
    "BlobStorage:InternalContainerName": "processed-documents",
    "ThirdPartyApi:BaseUrl": "https://api.thirdparty.com",
    "ThirdPartyApi:ApiKey": "your-api-key",
    "PrintService:BaseUrl": "https://print.service.com",
    "PrintService:ApiKey": "your-print-api-key"
  }
}
```

## Expected XML Format

The service expects XML in the following format:

```xml
<Docs>
    <Doc id="1">
        Base64EncodedContent
    </Doc>
    <Doc id="2">
        Base64EncodedContent
    </Doc>
</Docs>
```

Each `Doc` element should contain base64 encoded XML content. The decoded content is validated and sent to the print service if valid.

## Deployment

1. **Build the solution**:
   ```bash
   dotnet build
   ```

2. **Publish the Function**:
   ```bash
   cd src/DocumentProcessing.Function
   dotnet publish
   ```

3. **Deploy to Azure**:
   - Create Azure Function App (.NET 8, Isolated)
   - Configure Event Grid subscription
   - Set application settings
   - Deploy the published package

## Event Grid Integration

The function can handle various Event Grid event types:

- `Microsoft.Storage.BlobCreated`: Standard blob creation events
- `DocumentProcessing.BatchReceived`: Custom batch events with explicit blob URL and batch ID
- Generic events with `blobUrl` and `batchId` properties

## Error Handling

- **XML Validation Errors**: Sent to third-party API with error details
- **Document Validation Errors**: Individual document errors reported
- **Processing Errors**: Logged and can trigger function retries
- **Network Errors**: HTTP clients with retry policies

## Monitoring

- Application Insights integration for telemetry
- Structured logging with correlation IDs
- Health monitoring and retry policies
- Performance counters and metrics

## Development

### Prerequisites
- .NET 8 SDK
- Azure Storage Emulator (for local development)
- Azure Functions Core Tools

### Running Locally
1. Start Azure Storage Emulator
2. Update `local.settings.json` with your configuration
3. Run the function:
   ```bash
   cd src/DocumentProcessing.Function
   func start
   ```

### Testing
Send Event Grid events to the function endpoint with the expected payload format.

## Dependencies

- **Azure.Storage.Blobs**: Azure Blob Storage operations
- **Microsoft.Azure.Functions.Worker**: Azure Functions isolated worker
- **FluentValidation**: Input validation
- **System.Text.Json**: JSON serialization

## Custom Dispatcher Pattern

Instead of MediatR, this solution implements custom dispatchers:

```csharp
// Command dispatching
var result = await _commandDispatcher.DispatchAsync(command, cancellationToken);

// Query dispatching  
var result = await _queryDispatcher.DispatchAsync(query, cancellationToken);

// Event dispatching
await _eventDispatcher.DispatchAsync(@event, cancellationToken);
```

This provides:
- Better control over the dispatching logic
- Reduced external dependencies
- Cleaner separation between commands, queries, and events
- Type safety with generic constraints
