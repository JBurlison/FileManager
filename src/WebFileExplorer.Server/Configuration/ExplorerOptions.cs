namespace WebFileExplorer.Server.Configuration;

public class ExplorerOptions
{
    public const string SectionName = "Explorer";

    public string[] AuthorizedRoots { get; set; } = [];
}
