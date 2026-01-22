internal sealed class AppOptions
{
    public string? SourceFolder { get; set; }
    public string? SearchPattern { get; set; }
    public bool DeleteAfterUpload { get; set; } = true;
    public string? TagName { get; set; }
    public string? TagValue { get; set; }
    public int PollingIntervalSeconds { get; set; } = 5;
    public string DuplicateFolderName { get; set; } = "duplicate";
}

internal sealed class BlobOptions
{
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; }
}
