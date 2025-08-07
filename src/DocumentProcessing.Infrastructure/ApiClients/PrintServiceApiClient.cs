using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Interfaces;

namespace DocumentProcessing.Infrastructure.ApiClients;

public class PrintServiceApiClient : IPrintServiceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PrintServiceApiClient> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public PrintServiceApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PrintServiceApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["PrintService:BaseUrl"] ?? throw new InvalidOperationException("PrintService:BaseUrl not configured");
        _apiKey = configuration["PrintService:ApiKey"] ?? throw new InvalidOperationException("PrintService:ApiKey not configured");
        
        // Set up default headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DocumentProcessingService/1.0");
    }

    public async Task SendDocumentAsync(
        string documentId, 
        string documentContent, 
        string batchId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending document {DocumentId} from batch {BatchId} to print service", 
                documentId, batchId);

            var payload = new
            {
                DocumentId = documentId,
                BatchId = batchId,
                Content = documentContent,
                ContentType = "application/xml",
                Timestamp = DateTime.UtcNow,
                Source = "DocumentProcessingService",
                Priority = "Normal"
            };

            var jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var endpoint = $"{_baseUrl}/api/documents/print";
            
            _logger.LogDebug("Sending POST request to {Endpoint} for document {DocumentId}", endpoint, documentId);

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Successfully sent document {DocumentId} to print service. Response: {Response}", 
                    documentId, responseContent);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send document {DocumentId} to print service. Status: {StatusCode}, Response: {Response}", 
                    documentId, response.StatusCode, responseContent);
                
                throw new HttpRequestException($"Failed to send document to print service. Status: {response.StatusCode}, Response: {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending document {DocumentId} to print service", documentId);
            throw;
        }
    }
}