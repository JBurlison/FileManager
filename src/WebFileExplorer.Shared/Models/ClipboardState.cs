namespace WebFileExplorer.Shared.Models
{
    public class ClipboardState
    {
        public bool IsCut { get; set; }
        public List<string> Items { get; set; } = new List<string>();
        public string SourcePath { get; set; } = string.Empty;
    }
}
