using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<AppOptions>()
            .Bind(context.Configuration.GetSection("App"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SourceFolder), "App:SourceFolder is required.")
            .ValidateOnStart();

        services.AddOptions<BlobOptions>()
            .Bind(context.Configuration.GetSection("Blob"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "Blob:ConnectionString is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ContainerName), "Blob:ContainerName is required.")
            .ValidateOnStart();

        services.AddSingleton<IFileScanner, FileScanner>();
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddSingleton<IFileProcessor, FileProcessor>();
        services.AddHostedService<FolderPollingService>();
    })
    .Build();

await host.RunAsync();
