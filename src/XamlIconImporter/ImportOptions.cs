namespace XamlIconImporter;

internal sealed record ImportOptions(
    string InputPattern,
    string TargetPath,
    bool Overwrite,
    bool DryRun,
    bool Recursive,
    string KeyPrefix);

internal enum ImportStatus { Added, Replaced, Unchanged, Error }

internal sealed record ImportItem(
    string Source,
    string Key,
    ImportStatus Status,
    string Message);

internal sealed class ImportResult
{
    public List<ImportItem> Items { get; } = [];
    public int Matched => Items.Count;
    public int Added => Items.Count(x => x.Status == ImportStatus.Added);
    public int Replaced => Items.Count(x => x.Status == ImportStatus.Replaced);
    public int Skipped => Items.Count(x => x.Status == ImportStatus.Unchanged);
    public int Errors => Items.Count(x => x.Status == ImportStatus.Error);
    public bool Changed => Added + Replaced > 0;
}
