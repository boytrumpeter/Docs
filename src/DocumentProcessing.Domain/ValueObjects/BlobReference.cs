namespace DocumentProcessing.Domain.ValueObjects;

public record BlobReference
{
    public string Url { get; }
    public string ContainerName { get; }
    public string BlobName { get; }

    public BlobReference(string url, string containerName, string blobName)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));
        
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));

        Url = url;
        ContainerName = containerName;
        BlobName = blobName;
    }

    public static BlobReference FromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        var uri = new Uri(url);
        var segments = uri.AbsolutePath.TrimStart('/').Split('/');
        
        if (segments.Length < 2)
            throw new ArgumentException("Invalid blob URL format", nameof(url));

        var containerName = segments[0];
        var blobName = string.Join("/", segments.Skip(1));

        return new BlobReference(url, containerName, blobName);
    }
}