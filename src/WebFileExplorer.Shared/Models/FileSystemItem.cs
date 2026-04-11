namespace WebFileExplorer.Shared.Models;

public record FileSystemItem(
    string Name,
    string FullPath,
    FileSystemItemType Type,
    long Size,
    DateTimeOffset LastModified,
    bool IsHidden,
    string? OriginalPath = null,
    DateTimeOffset? DeletionTime = null
);
