namespace DocumentProcessing.Domain.Entities;

public enum DocumentStatus
{
    Pending,
    Decoded,
    Valid,
    Invalid,
    SentToPrint,
    Processed
}