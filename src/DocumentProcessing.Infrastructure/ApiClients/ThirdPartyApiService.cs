using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocumentProcessing.Application.Interfaces;

namespace DocumentProcessing.Infrastructure.ApiClients;

public class ThirdPartyApiService : IThirdPartyApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThirdPartyApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public ThirdPartyApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ThirdPartyApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["ThirdPartyApi:BaseUrl"] ?? throw new InvalidOperationException("ThirdPartyApi:BaseUrl not configured");
        _apiKey = configuration["ThirdPartyApi:ApiKey"] ?? throw new InvalidOperationException("ThirdPartyApi:ApiKey not configured");
        
        // Set up default headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DocumentProcessingService/1.0");
    }

    public async Task SendValidationResultAsync(
        string batchId, 
        string errorType, 
        IEnumerable<string> errors, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending validation result for batch {BatchId}, error type: {ErrorType}", 
                batchId, errorType);

            var payload = new
            {
                BatchId = batchId,
                ErrorType = errorType,
                Errors = errors.ToArray(),
                Timestamp = DateTime.UtcNow,
                Source = "DocumentProcessingService"
            };

            var jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            var endpoint = $"{_baseUrl}/api/validation-results";
            
            _logger.LogDebug("Sending POST request to {Endpoint} with payload: {Payload}", endpoint, jsonContent);

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent validation result for batch {BatchId}", batchId);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send validation result for batch {BatchId}. Status: {StatusCode}, Response: {Response}", 
                    batchId, response.StatusCode, responseContent);
                
                throw new HttpRequestException($"Failed to send validation result. Status: {response.StatusCode}, Response: {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending validation result for batch {BatchId}", batchId);
            throw;
        }
    }
}