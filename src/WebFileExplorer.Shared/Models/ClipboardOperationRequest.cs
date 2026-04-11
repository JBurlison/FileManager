using System.ComponentModel.DataAnnotations;

namespace WebFileExplorer.Shared.Models;

public enum ConflictResolution
{
    None,
    Overwrite,
    Merge,
    Skip
}

public class ClipboardOperationRequest
{
    [Required]
    public IEnumerable<string> Items { get; set; } = new List<string>();

    [Required]
    public string DestinationPath { get; set; } = string.Empty;

    public bool Overwrite { get; set; } = false;

    public ConflictResolution Resolution { get; set; } = ConflictResolution.None;
}
