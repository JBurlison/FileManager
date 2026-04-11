using System.Net;
using System.Text.Json;
using Bunit;
using WebFileExplorer.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using Radzen.Blazor;
using WebFileExplorer.Client.Pages;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.UI;

[TestClass]
public class HomeTests : BunitContext
{
    private IRenderedComponent<Home> SetupAndRender(Func<HttpRequestMessage, HttpResponseMessage>? mockHttpHandler = null)
    {
        var mockHttp = new CustomMockHttpMessageHandler(mockHttpHandler);
        var httpClient = new HttpClient(mockHttp)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        Services.AddSingleton(httpClient);
        Services.AddSingleton(new Mock<ILogger<Home>>().Object);
        
        Services.AddSingleton(new ClipboardStateContainer());
        Services.AddSingleton(new ClipboardStateContainer());
        Services.AddScoped<DialogService>();
        Services.AddScoped<NotificationService>();
        Services.AddScoped<TooltipService>();
        Services.AddScoped<ContextMenuService>();

        return Render<Home>();
    }

    private DataGridRowMouseEventArgs<T> CreateMouseEventArgs<T>(T data)
    {
        var args = (DataGridRowMouseEventArgs<T>)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(DataGridRowMouseEventArgs<T>));
        var prop = typeof(DataGridRowMouseEventArgs<T>).GetProperty("Data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(args, data);
        }
        else
        {
            typeof(DataGridRowMouseEventArgs<T>).GetField("<Data>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(args, data);
        }
        return args;
    }

    private TreeEventArgs CreateTreeEventArgs(object value)
    {
        var args = new TreeEventArgs();
        var prop = typeof(TreeEventArgs).GetProperty("Value", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(args, value);
        }
        else
        {
            typeof(TreeEventArgs).GetField("<Value>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(args, value);
        }
        return args;
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_WhenRendered_ShowsSplitterAndTree()
    {
        var cut = SetupAndRender();

        var splitters = cut.FindAll(".rz-splitter");
        Assert.IsNotNull(splitters);
        Assert.IsGreaterThanOrEqualTo(1, splitters.Count, "Expected a RadzenSplitter to be rendered.");

        var panes = cut.FindAll(".rz-splitter-pane");
        Assert.IsNotNull(panes);
        Assert.IsGreaterThanOrEqualTo(2, panes.Count, "Expected at least two splitter panes for nav and content.");
        
        cut.WaitForState(() => cut.FindAll(".rz-tree").Count > 0, TimeSpan.FromSeconds(2));
        var tree = cut.Find(".rz-tree");
        Assert.IsNotNull(tree, "Expected RadzenTree to be rendered.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_RootNavigation_LoadsDirectoryItems()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("Folder1", "C:\\Folder1", FileSystemItemType.Folder, 0, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(items))
                };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        cut.WaitForState(() => cut.FindAll(".rz-tree").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        // Simulate tree node selection
        var tree = cut.FindComponent<RadzenTree>();
        cut.InvokeAsync(() => tree.Instance.Change.InvokeAsync(CreateTreeEventArgs(new DriveItem("C:", "C:\\", 0, 0))));

        // Assert
        cut.WaitForState(() => cut.FindAll(".rz-datatable, .rz-grid-table, .rz-data-grid").Count > 0, TimeSpan.FromSeconds(2));
        var gridStr = cut.Markup;
        Assert.Contains("Folder1", gridStr, "Expected the grid to show 'Folder1' from the loaded root.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_FolderDoubleClick_NavigatesToFolder()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("Subfolder", "C:\\Subfolder", FileSystemItemType.Folder, 0, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5CSubfolder"))
            {
                var subItems = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("File.txt", "C:\\Subfolder\\File.txt", FileSystemItemType.File, 1024, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(subItems)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        cut.WaitForState(() => cut.FindAll(".rz-tree").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        // Load root first
        var tree = cut.FindComponent<RadzenTree>();
        cut.InvokeAsync(() => tree.Instance.Change.InvokeAsync(CreateTreeEventArgs(new DriveItem("C:", "C:\\", 0, 0))));
        cut.WaitForState(() => cut.Markup.Contains("Subfolder"), TimeSpan.FromSeconds(2));

        // Simulate double-click on row (Bunit component interaction)
        var grid = cut.FindComponent<RadzenDataGrid<FileSystemItem>>();
        var folderItem = new FileSystemItem("Subfolder", "C:\\Subfolder", FileSystemItemType.Folder, 0, DateTime.Now, false);
        cut.InvokeAsync(() => grid.Instance.RowDoubleClick.InvokeAsync(CreateMouseEventArgs<FileSystemItem>(folderItem)));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("File.txt"), TimeSpan.FromSeconds(2));
        Assert.Contains("File.txt", cut.Markup, "Expected details view to navigate into Subfolder and show File.txt");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_AddressBarNavigation_LoadsDirectoryItems()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=D%3A%5CExplicitPath"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("Doc.pdf", "D:\\ExplicitPath\\Doc.pdf", FileSystemItemType.File, 2048, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);

        // Act
        var input = cut.Find("input.rz-textbox");
        input.Change("D:\\ExplicitPath");
        var buttons = cut.FindAll("button.rz-button");
        // We know Go button has 'keyboard_return' icon
        foreach (var btn in buttons)
        {
            if (btn.InnerHtml.Contains("keyboard_return"))
            {
                btn.Click();
                break;
            }
        }

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Doc.pdf"), TimeSpan.FromSeconds(2));
        Assert.Contains("Doc.pdf", cut.Markup, "Expected grid to display file from explicit address bar navigation");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_InvalidPath_ShowsErrorMessage()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=Z%3A%5CInvalid"))
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);

        // Act
        var input = cut.Find("input.rz-textbox");
        input.Change("Z:\\Invalid");
        input.KeyUp("Enter");

        // Assert
        cut.WaitForState(() => cut.FindAll(".rz-alert-danger").Count > 0, TimeSpan.FromSeconds(2));
        var alert = cut.Find(".rz-alert-danger");
        Assert.Contains("Access Denied", alert.InnerHtml, "Expected forbidden error message on invalid path");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_DirectoryLoad_UpdatesBreadcrumbs()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=" + Uri.EscapeDataString("C:\\Users\\Test")))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);

        // Act
        var input = cut.Find("input.rz-textbox");
        input.Change("C:\\Users\\Test");
        input.KeyUp("Enter");

        // Assert
        cut.WaitForState(() => cut.FindAll(".rz-breadcrumb-item").Count > 2, TimeSpan.FromSeconds(2));
        var breadcrumbsHtml = cut.Find(".rz-breadcrumb").InnerHtml;
        Assert.IsTrue(breadcrumbsHtml.Contains("Users") && breadcrumbsHtml.Contains("Test"), "Expected breadcrumb items for each path segment");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_DetailsGrid_DisplaysExpectedColumns()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("File1", "C:\\File1", FileSystemItemType.File, 1234, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        cut.WaitForState(() => cut.FindAll(".rz-tree").Count > 0, TimeSpan.FromSeconds(2));

        // Act
        var tree = cut.FindComponent<RadzenTree>();
        cut.InvokeAsync(() => tree.Instance.Change.InvokeAsync(CreateTreeEventArgs(new DriveItem("C:", "C:\\", 0, 0))));
        cut.WaitForState(() => cut.FindAll(".rz-column-title").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var headers = cut.FindAll(".rz-column-title");
        var headersHtml = string.Join(" ", headers.Select(h => h.InnerHtml));
        Assert.Contains("Name", headersHtml, "Expected Name column");
        Assert.Contains("Date Modified", headersHtml, "Expected Date Modified column");
        Assert.Contains("Type", headersHtml, "Expected Type column");
        Assert.Contains("Size", headersHtml, "Expected Size column");
    }
}

public class CustomMockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _customHandler;

    public CustomMockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? customHandler = null)
    {
        _customHandler = customHandler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_customHandler != null)
        {
            var response = _customHandler.Invoke(request);
            if (response != null)
            {
                return Task.FromResult(response);
            }
        }

        if (request.RequestUri!.PathAndQuery.Contains("api/fileexplorer/roots"))
        {
            var roots = new List<DriveItem>
            {
                new DriveItem("C:", "C:\\", 0, 0),
                new DriveItem("D:", "D:\\", 0, 0)
            };
            
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(roots))
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        });
    }
}

