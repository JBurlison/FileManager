namespace WebFileExplorer.Shared.Models;
public class PagedResult<T> {
    public System.Collections.Generic.IEnumerable<T> Items { get; set; } = System.Array.Empty<T>();
    public int TotalCount { get; set; }
}
