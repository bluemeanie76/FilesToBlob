using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _containerInitialized;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IOptions<BlobOptions> options, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        BlobOptions blobOptions = options.Value;
        _containerClient = new BlobContainerClient(blobOptions.ConnectionString, blobOptions.ContainerName);
    }

    public async Task<bool> BlobExistsAsync(string blobName, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);
        BlobClient blobClient = _containerClient.GetBlobClient(blobName);
        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async Task UploadAsync(string blobName, Stream content, IDictionary<string, string>? tags, CancellationToken cancellationToken)
    {
        await EnsureContainerAsync(cancellationToken);
        BlobClient blobClient = _containerClient.GetBlobClient(blobName);
        var options = new BlobUploadOptions
        {
            Tags = tags
        };

        await blobClient.UploadAsync(content, options, cancellationToken);
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (_containerInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_containerInitialized)
            {
                return;
            }

            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            _containerInitialized = true;
            _logger.LogInformation("Ensured blob container {ContainerName} exists.", _containerClient.Name);
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
