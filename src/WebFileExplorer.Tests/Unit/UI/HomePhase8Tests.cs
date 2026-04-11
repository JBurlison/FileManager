using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using Radzen.Blazor;
using WebFileExplorer.Client.Pages;
using WebFileExplorer.Client.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.UI;

[TestClass]
public class HomePhase8Tests : BunitContext
{
    private ClipboardStateContainer _clipboard = null!;

    [TestInitialize]
    public void Setup()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("open", _ => true).SetVoidResult();
        JSInterop.SetupVoid("eval", _ => true).SetVoidResult();

        _clipboard = new ClipboardStateContainer();
        Services.AddSingleton(_clipboard);
        Services.AddScoped<DialogService>();
        Services.AddScoped<NotificationService>();
        Services.AddSingleton(Mock.Of<ILogger<Home>>());
        Services.AddScoped<TooltipService>();
        Services.AddScoped<ContextMenuService>();
    }

    private IRenderedComponent<Home> SetupAndRender(Func<HttpRequestMessage, HttpResponseMessage>? mockHttpHandler = null)
    {
        Func<HttpRequestMessage, HttpResponseMessage> handler = req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
                
            if (req.Method == HttpMethod.Get && req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list?path=C:%5C") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", "C:\\test.txt", FileSystemItemType.File, 10, DateTimeOffset.Now, false) }) };

            if (mockHttpHandler != null)
                return mockHttpHandler(req);
                
            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("Not Founnd") };
        };

        var httpClient = new HttpClient(new CustomMockHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        Services.AddSingleton(httpClient);

        return Render<Home>();
    }

    [TestMethod]
    public void SearchBox_InitializesCorrectly()
    {
        var comp = SetupAndRender();
        comp.WaitForState(() => comp.FindComponents<RadzenTextBox>().Count > 0);
        
        var searchTextBoxes = comp.FindComponents<RadzenTextBox>();
        Assert.IsTrue(searchTextBoxes.Any(tb => tb.Instance.Placeholder == "Search..."));
    }

    [TestMethod]
    public async Task Search_UpdatesGridWithLocationColumn()
    {
        // Search grid data
        bool searchCalled = false;
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/search") == true)
            {
                searchCalled = true;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new[]
                    {
                        new FileSystemItem("search_match.txt", "C:\\sub1\\search_match.txt", FileSystemItemType.File, 500, DateTimeOffset.Now, false)
                    })
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        comp.WaitForState(() => comp.FindComponents<RadzenDataGrid<FileSystemItem>>().Count > 0);

        // Click search button
        // First we set properties normally
        comp.Instance.GetType().GetField("_currentPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(comp.Instance, "C:\\");
        comp.Instance.GetType().GetField("_searchInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(comp.Instance, "search_match");
        comp.Render(); // rerender

        var searchBtns = comp.FindComponents<RadzenButton>().Where(b => b.Instance.Icon == "search").ToList();
        var performSearchTask = (Task)comp.Instance.GetType().GetMethod("PerformSearch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(comp.Instance, null)!;
        await performSearchTask;

        comp.WaitForState(() => searchCalled == true);

        // Grid should have a DataGridColumn with Title='Location'
        var columns = comp.FindComponents<RadzenDataGridColumn<FileSystemItem>>();
        Assert.IsTrue(columns.Any(c => c.Instance.Title == "Location" || c.Instance.Title == "Path"));
    }

    [TestMethod]
    public async Task CancelSearch_RestoresNormalView()
    {
        var comp = SetupAndRender();
        
        comp.WaitForState(() => comp.FindComponents<RadzenDataGrid<FileSystemItem>>().Count > 0);

        comp.Instance.GetType().GetField("_isSearching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(comp.Instance, true);
        comp.Render();

        var cancelTask = comp.Instance.GetType().GetMethod("CancelSearch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(comp.Instance, null);
        if (cancelTask is Task t) {
            await t;
        }

        var isSearching = (bool)comp.Instance.GetType().GetField("_isSearching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(comp.Instance)!;
        Assert.IsFalse(isSearching);
    }

    [TestMethod]
    public async Task Search_ShowsLoadingIndicator()
    {
        var searchCompletionSource = new TaskCompletionSource<HttpResponseMessage>();

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/search") == true)
            {
                return searchCompletionSource.Task.Result;
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        comp.WaitForState(() => comp.FindComponents<RadzenDataGrid<FileSystemItem>>().Count > 0);

        comp.Instance.GetType().GetField("_currentPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(comp.Instance, "C:\\");
        comp.Instance.GetType().GetField("_searchInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(comp.Instance, "search_match");
        comp.Render();

        var searchTask = (Task)comp.Instance.GetType().GetMethod("PerformSearch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(comp.Instance, null)!;
        
        comp.Render();
        var progressBars = comp.FindComponents<RadzenProgressBar>();
        Assert.IsTrue(progressBars.Any(pb => pb.Instance.Mode == ProgressBarMode.Indeterminate));

        searchCompletionSource.SetResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new[] { new FileSystemItem("search_match.txt", "C:\\search_match.txt", FileSystemItemType.File, 500, DateTimeOffset.Now, false) })
        });

        await searchTask;
        comp.Render();
        
        // After loading is complete, progress bar might still be there for something else but in this main area it should hide, or _isLoading is false
        var isLoading = (bool)comp.Instance.GetType().GetField("_isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(comp.Instance)!;
        Assert.IsFalse(isLoading);
    }
}
