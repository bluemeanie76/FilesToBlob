internal sealed class FileScanner : IFileScanner
{
    public IEnumerable<string> Scan(string sourceFolder, string searchPattern)
    {
        if (string.IsNullOrWhiteSpace(searchPattern))
        {
            searchPattern = "*.pdf";
        }

        return Directory.EnumerateFiles(sourceFolder, searchPattern);
    }
}
