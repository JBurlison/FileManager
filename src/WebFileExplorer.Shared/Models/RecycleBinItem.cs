namespace WebFileExplorer.Shared.Models;

public record RecycleBinItem(
    string Name,
    string OriginalPath,
    DateTimeOffset DeletionTime,
    string Id,
    long Size
);