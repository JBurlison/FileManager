namespace WebFileExplorer.Client.Services
{
    public class ClipboardStateContainer
    {
        public bool IsCut { get; private set; }
        public List<string> Items { get; private set; } = new List<string>();
        public string SourcePath { get; private set; } = string.Empty;

        public event Action? OnChange;

        public void SetState(bool isCut, IEnumerable<string> items, string sourcePath)
        {
            IsCut = isCut;
            Items = items.ToList();
            SourcePath = sourcePath;
            NotifyStateChanged();
        }

        public void Clear()
        {
            IsCut = false;
            Items.Clear();
            SourcePath = string.Empty;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}