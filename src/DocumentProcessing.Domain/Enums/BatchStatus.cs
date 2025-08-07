namespace DocumentProcessing.Domain.Entities;

public enum BatchStatus
{
    Received,
    Downloaded,
    Stored,
    XmlValid,
    XmlInvalid,
    DocumentsProcessed,
    Processed
}