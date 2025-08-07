namespace DocumentProcessing.Domain.Entities;

public class DocumentBatch
{
    public string BatchId { get; }
    public string SourceBlobUrl { get; }
    public string? InternalBlobUrl { get; private set; }
    public string? RawXmlContent { get; private set; }
    public List<Document> Documents { get; }
    public bool IsXmlValid { get; private set; }
    public List<string> XmlValidationErrors { get; }
    public BatchStatus Status { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? ProcessedAt { get; private set; }

    public DocumentBatch(string batchId, string sourceBlobUrl)
    {
        if (string.IsNullOrWhiteSpace(batchId))
            throw new ArgumentException("Batch ID cannot be null or empty", nameof(batchId));
        
        if (string.IsNullOrWhiteSpace(sourceBlobUrl))
            throw new ArgumentException("Source blob URL cannot be null or empty", nameof(sourceBlobUrl));

        BatchId = batchId;
        SourceBlobUrl = sourceBlobUrl;
        Documents = new List<Document>();
        XmlValidationErrors = new List<string>();
        Status = BatchStatus.Received;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetXmlContent(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content cannot be null or empty", nameof(xmlContent));

        RawXmlContent = xmlContent;
        Status = BatchStatus.Downloaded;
    }

    public void SetInternalBlobUrl(string internalBlobUrl)
    {
        if (string.IsNullOrWhiteSpace(internalBlobUrl))
            throw new ArgumentException("Internal blob URL cannot be null or empty", nameof(internalBlobUrl));

        InternalBlobUrl = internalBlobUrl;
        Status = BatchStatus.Stored;
    }

    public void SetXmlValidationResult(bool isValid, List<string> validationErrors)
    {
        IsXmlValid = isValid;
        XmlValidationErrors.Clear();
        if (validationErrors.Any())
            XmlValidationErrors.AddRange(validationErrors);

        Status = isValid ? BatchStatus.XmlValid : BatchStatus.XmlInvalid;
    }

    public void AddDocument(Document document)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        Documents.Add(document);
    }

    public void MarkAsProcessed()
    {
        Status = BatchStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool HasValidDocuments => Documents.Any(d => d.IsValid);
    public bool HasInvalidDocuments => Documents.Any(d => !d.IsValid);
    public int ValidDocumentCount => Documents.Count(d => d.IsValid);
    public int InvalidDocumentCount => Documents.Count(d => !d.IsValid);
}