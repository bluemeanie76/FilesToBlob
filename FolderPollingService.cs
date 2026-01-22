using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class FolderPollingService : BackgroundService
{
    private readonly ILogger<FolderPollingService> _logger;
    private readonly IFileScanner _scanner;
    private readonly IFileProcessor _processor;
    private readonly IOptionsMonitor<AppOptions> _appOptions;

    public FolderPollingService(
        ILogger<FolderPollingService> logger,
        IFileScanner scanner,
        IFileProcessor processor,
        IOptionsMonitor<AppOptions> appOptions)
    {
        _logger = logger;
        _scanner = scanner;
        _processor = processor;
        _appOptions = appOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Folder polling service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            AppOptions options = _appOptions.CurrentValue;
            string sourceFolder = options.SourceFolder ?? string.Empty;
            string searchPattern = string.IsNullOrWhiteSpace(options.SearchPattern) ? "*.pdf" : options.SearchPattern;
            int pollingInterval = Math.Max(1, options.PollingIntervalSeconds);

            try
            {
                Directory.CreateDirectory(sourceFolder);

                List<string> files = _scanner.Scan(sourceFolder, searchPattern).ToList();
                int fileFound = files.Count;
                int processed = 0;
                int deleted = 0;
                int duplicates = 0;

                foreach (string file in files)
                {
                    try
                    {
                        FileProcessResult result = await _processor.ProcessAsync(file, stoppingToken);
                        if (result.Uploaded)
                        {
                            processed++;
                        }

                        if (result.Deleted)
                        {
                            deleted++;
                        }

                        if (result.Duplicated)
                        {
                            duplicates++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process {File}.", file);
                    }
                }

                _logger.LogInformation("File Found: {FileFound}", fileFound);
                _logger.LogInformation("Files Processed: {Processed}", processed);
                _logger.LogInformation("Files Deleted: {Deleted}", deleted);
                _logger.LogInformation("Duplicate Files: {Duplicate}", duplicates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Folder polling encountered an error.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(pollingInterval), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}
