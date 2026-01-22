internal interface IFileScanner
{
    IEnumerable<string> Scan(string sourceFolder, string searchPattern);
}

internal interface IBlobStorageService
{
    Task<bool> BlobExistsAsync(string blobName, CancellationToken cancellationToken);
    Task UploadAsync(string blobName, Stream content, IDictionary<string, string>? tags, CancellationToken cancellationToken);
}

internal interface IFileProcessor
{
    Task<FileProcessResult> ProcessAsync(string filePath, CancellationToken cancellationToken);
}
