internal sealed record FileProcessResult(bool Uploaded, bool Deleted, bool Duplicated)
{
    public static FileProcessResult NoAction { get; } = new(false, false, false);
}
