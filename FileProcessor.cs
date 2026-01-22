using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class FileProcessor : IFileProcessor
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<FileProcessor> _logger;
    private readonly IOptionsMonitor<AppOptions> _appOptions;

    public FileProcessor(
        IBlobStorageService blobStorage,
        ILogger<FileProcessor> logger,
        IOptionsMonitor<AppOptions> appOptions)
    {
        _blobStorage = blobStorage;
        _logger = logger;
        _appOptions = appOptions;
    }

    public async Task<FileProcessResult> ProcessAsync(string filePath, CancellationToken cancellationToken)
    {
        AppOptions options = _appOptions.CurrentValue;

        if (!File.Exists(filePath))
        {
            return FileProcessResult.NoAction;
        }

        string blobName = Path.GetFileName(filePath);
        bool exists = await _blobStorage.BlobExistsAsync(blobName, cancellationToken);
        if (exists)
        {
            string destination = MoveToDuplicateFolder(filePath, options);
            _logger.LogInformation("Duplicate blob detected. Moved {File} to {Destination}.", filePath, destination);
            return new FileProcessResult(false, false, true);
        }

        await using FileStream fileStream = File.OpenRead(filePath);
        await _blobStorage.UploadAsync(blobName, fileStream, BuildTags(options), cancellationToken);
        _logger.LogInformation("Uploaded {File} to blob {BlobName}.", filePath, blobName);

        if (options.DeleteAfterUpload)
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted {File} after upload.", filePath);
            return new FileProcessResult(true, true, false);
        }

        return new FileProcessResult(true, false, false);
    }

    private static IDictionary<string, string>? BuildTags(AppOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.TagName) && !string.IsNullOrWhiteSpace(options.TagValue))
        {
            return new Dictionary<string, string>
            {
                [options.TagName] = options.TagValue
            };
        }

        return null;
    }

    private static string MoveToDuplicateFolder(string filePath, AppOptions options)
    {
        string duplicateFolder = Path.Combine(options.SourceFolder ?? string.Empty, options.DuplicateFolderName);
        Directory.CreateDirectory(duplicateFolder);

        string destinationPath = Path.Combine(duplicateFolder, Path.GetFileName(filePath));
        if (File.Exists(destinationPath))
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
            destinationPath = Path.Combine(duplicateFolder, $"{fileName}-{timestamp}{extension}");
        }

        File.Move(filePath, destinationPath);
        return destinationPath;
    }
}
