namespace DocumentProcessing.Application.Interfaces;

public interface IThirdPartyApiService
{
    Task SendValidationResultAsync(
        string batchId, 
        string errorType, 
        IEnumerable<string> errors, 
        CancellationToken cancellationToken = default);
}