using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddConsole();
});

ILogger logger = loggerFactory.CreateLogger("FilesToBlob");

AppOptions appOptions = configuration.GetSection("App").Get<AppOptions>() ?? new AppOptions();
BlobOptions blobOptions = configuration.GetSection("Blob").Get<BlobOptions>() ?? new BlobOptions();

if (string.IsNullOrWhiteSpace(appOptions.SourceFolder))
{
    logger.LogError("App:SourceFolder is required.");
    return;
}

if (!Directory.Exists(appOptions.SourceFolder))
{
    logger.LogError("Source folder does not exist: {SourceFolder}", appOptions.SourceFolder);
    return;
}

if (string.IsNullOrWhiteSpace(blobOptions.ConnectionString))
{
    logger.LogError("Blob:ConnectionString is required.");
    return;
}

if (string.IsNullOrWhiteSpace(blobOptions.ContainerName))
{
    logger.LogError("Blob:ContainerName is required.");
    return;
}

string searchPattern = string.IsNullOrWhiteSpace(appOptions.SearchPattern) ? "*.pdf" : appOptions.SearchPattern;

var containerClient = new BlobContainerClient(blobOptions.ConnectionString, blobOptions.ContainerName);
await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

IReadOnlyDictionary<string, string>? tags = null;
if (!string.IsNullOrWhiteSpace(appOptions.TagName) && !string.IsNullOrWhiteSpace(appOptions.TagValue))
{
    tags = new Dictionary<string, string>
    {
        [appOptions.TagName] = appOptions.TagValue
    };
}

var files = Directory.EnumerateFiles(appOptions.SourceFolder, searchPattern);
int processedCount = 0;

foreach (string file in files)
{
    try
    {
        string blobName = Path.GetFileName(file);
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        await using FileStream fileStream = File.OpenRead(file);
        var uploadOptions = new BlobUploadOptions
        {
            Tags = tags
        };

        await blobClient.UploadAsync(fileStream, uploadOptions);
        logger.LogInformation("Uploaded {File} to blob {BlobName}", file, blobName);

        if (appOptions.DeleteAfterUpload)
        {
            File.Delete(file);
            logger.LogInformation("Deleted {File} after upload.", file);
        }

        processedCount++;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to process {File}", file);
    }
}

logger.LogInformation("Completed processing. Files uploaded: {ProcessedCount}", processedCount);

internal sealed class AppOptions
{
    public string? SourceFolder { get; set; }
    public string? SearchPattern { get; set; }
    public bool DeleteAfterUpload { get; set; } = true;
    public string? TagName { get; set; }
    public string? TagValue { get; set; }
}

internal sealed class BlobOptions
{
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; }
}
