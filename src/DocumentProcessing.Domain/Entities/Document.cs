namespace DocumentProcessing.Domain.Entities;

public class Document
{
    public string Id { get; }
    public string EncodedContent { get; }
    public string? DecodedContent { get; private set; }
    public string? ValidationSchema { get; private set; }
    public bool IsValid { get; private set; }
    public List<string> ValidationErrors { get; }
    public DocumentStatus Status { get; private set; }

    public Document(string id, string encodedContent)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Document ID cannot be null or empty", nameof(id));
        
        if (string.IsNullOrWhiteSpace(encodedContent))
            throw new ArgumentException("Encoded content cannot be null or empty", nameof(encodedContent));

        Id = id;
        EncodedContent = encodedContent;
        ValidationErrors = new List<string>();
        Status = DocumentStatus.Pending;
    }

    public void SetDecodedContent(string decodedContent)
    {
        if (string.IsNullOrWhiteSpace(decodedContent))
            throw new ArgumentException("Decoded content cannot be null or empty", nameof(decodedContent));

        DecodedContent = decodedContent;
        Status = DocumentStatus.Decoded;
    }

    public void SetValidationResult(bool isValid, List<string> validationErrors, string? schema = null)
    {
        IsValid = isValid;
        ValidationErrors.Clear();
        if (validationErrors.Any())
            ValidationErrors.AddRange(validationErrors);
        
        ValidationSchema = schema;
        Status = isValid ? DocumentStatus.Valid : DocumentStatus.Invalid;
    }

    public void MarkAsSentToPrint()
    {
        if (!IsValid)
            throw new InvalidOperationException("Cannot send invalid document to print service");

        Status = DocumentStatus.SentToPrint;
    }

    public void MarkAsProcessed()
    {
        Status = DocumentStatus.Processed;
    }
}