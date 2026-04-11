namespace WebFileExplorer.Shared.Models;

public record DriveItem(
    string Name,
    string RootDirectory,
    long AvailableFreeSpace,
    long TotalSize
);
