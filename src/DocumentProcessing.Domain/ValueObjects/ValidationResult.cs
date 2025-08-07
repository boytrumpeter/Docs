namespace DocumentProcessing.Domain.ValueObjects;

public record ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }
    public string? Schema { get; }

    public ValidationResult(bool isValid, IEnumerable<string> errors, string? schema = null)
    {
        IsValid = isValid;
        Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        Schema = schema;
    }

    public static ValidationResult Success(string? schema = null) 
        => new(true, Enumerable.Empty<string>(), schema);

    public static ValidationResult Failure(IEnumerable<string> errors, string? schema = null) 
        => new(false, errors, schema);

    public static ValidationResult Failure(string error, string? schema = null) 
        => new(false, new[] { error }, schema);
}