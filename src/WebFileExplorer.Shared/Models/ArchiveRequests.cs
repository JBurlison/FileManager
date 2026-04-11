namespace WebFileExplorer.Shared.Models;

public class CompressRequest
{
    public IEnumerable<string> SourcePaths { get; set; } = Array.Empty<string>();
    public string DestinationZipPath { get; set; } = string.Empty;
}

public class ExtractRequest
{
    public string ZipPath { get; set; } = string.Empty;
    public string DestinationFolderPath { get; set; } = string.Empty;
    public ConflictResolution Resolution { get; set; } = ConflictResolution.None;
}