using System;

namespace WebFileExplorer.Shared.Models;

public record FileProperties(
    string Name,
    string Location,
    string Type,
    long Size,
    int FileCount,
    int FolderCount,
    DateTimeOffset? Created,
    DateTimeOffset? LastModified,
    DateTimeOffset? Accessed,
    bool? IsHidden,
    bool? IsReadOnly,
    bool? IsSystem,
    bool? IsArchive
);