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
public class HomePhase3Tests : BunitContext
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

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_ViewMode_SwitchesBetweenDetailsListIcons()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("File.txt", "C:\\File.txt", FileSystemItemType.File, 100, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        
        // Act - Simulate load directory
        
        cut.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, new object[] { "C:\\" }));
        cut.WaitForState(() => cut.Markup.Contains("File.txt"), TimeSpan.FromSeconds(2));

        // Default is Details (DataGrid)
        Assert.IsNotEmpty(cut.FindAll(".rz-datatable, .rz-grid-table, .rz-data-grid"), "Expected DataGrid to be rendered in Details view.");

        // Switch to List View
        var selectBar = cut.FindComponent<RadzenSelectBar<string>>();
        cut.InvokeAsync(() => selectBar.Instance.Change.InvokeAsync("List"));
        cut.WaitForState(() => cut.FindAll(".custom-view-container.list").Count > 0, TimeSpan.FromSeconds(2));
        Assert.IsNotEmpty(cut.FindAll(".custom-view-container.list"), "Expected custom list view container after switching to List.");

        // Switch to LargeIcons View
        cut.InvokeAsync(() => selectBar.Instance.Change.InvokeAsync("LargeIcons"));
        cut.WaitForState(() => cut.FindAll(".custom-view-container.largeicons").Count > 0, TimeSpan.FromSeconds(2));
        Assert.IsNotEmpty(cut.FindAll(".custom-view-container.largeicons"), "Expected custom largeicons view container after switching to LargeIcons.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_ShowHidden_TogglingCheckbox_ReloadsDataWithHiddenFlag()
    {
        // Arrange
        bool wasHiddenRequested = false;
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C&showHidden=False"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() } { new FileSystemItem("Visible.txt", "C:\\Visible.txt", FileSystemItemType.File, 100, DateTime.Now, false) };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C&showHidden=True"))
            {
                wasHiddenRequested = true;
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() } 
                { 
                    new FileSystemItem("Visible.txt", "C:\\Visible.txt", FileSystemItemType.File, 100, DateTime.Now, false),
                    new FileSystemItem("Hidden.txt", "C:\\Hidden.txt", FileSystemItemType.File, 100, DateTime.Now, true)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        
        
        cut.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, new object[] { "C:\\" }));
        cut.WaitForState(() => cut.Markup.Contains("Visible.txt"), TimeSpan.FromSeconds(2));

        // Act - Toggle Show Hidden checkbox
        var cbHidden = cut.FindComponent<RadzenCheckBox<bool>>();
        cut.InvokeAsync(() => cbHidden.Instance.Change.InvokeAsync(true));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Hidden.txt"), TimeSpan.FromSeconds(2));
        Assert.IsTrue(wasHiddenRequested, "Expected API call to include showHidden=True");
        Assert.Contains("Hidden.txt", cut.Markup, "Expected hidden item to be rendered.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_MultiSelect_CtrlClick_AddsToSelection()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("File1.txt", "C:\\File1.txt", FileSystemItemType.File, 100, DateTime.Now, false),
                    new FileSystemItem("File2.txt", "C:\\File2.txt", FileSystemItemType.File, 100, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        
        // Load and switch to List view to test custom div multi-select
        
        cut.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, new object[] { "C:\\" }));
        cut.WaitForState(() => cut.Markup.Contains("File1.txt"), TimeSpan.FromSeconds(2));

        var selectBar = cut.FindComponent<RadzenSelectBar<string>>();
        cut.InvokeAsync(() => selectBar.Instance.Change.InvokeAsync("List"));
        cut.WaitForState(() => cut.FindAll(".custom-view-container.list").Count > 0, TimeSpan.FromSeconds(2));

        // Act - Click first item, then Ctrl+Click second item
        var items = cut.FindAll(".custom-view-item");
        Assert.IsGreaterThanOrEqualTo(2, items.Count, "Expected at least 2 items rendered.");
        
        items[0].Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { CtrlKey = false });
        items[1].Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { CtrlKey = true });

        // Assert
        // Re-query mutated DOM
        var selectedItems = cut.FindAll(".custom-view-item.selected");
        Assert.HasCount(2, selectedItems, "Expected both items to be part of the selection after Ctrl+Click.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_MultiSelect_ShiftClick_SelectsRange()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("File1.txt", "C:\\File1.txt", FileSystemItemType.File, 100, DateTime.Now, false),
                    new FileSystemItem("File2.txt", "C:\\File2.txt", FileSystemItemType.File, 100, DateTime.Now, false),
                    new FileSystemItem("File3.txt", "C:\\File3.txt", FileSystemItemType.File, 100, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);

        // Load and switch to List view
        
        cut.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, new object[] { "C:\\" }));
        cut.WaitForState(() => cut.Markup.Contains("File3.txt"), TimeSpan.FromSeconds(2));

        var selectBar = cut.FindComponent<RadzenSelectBar<string>>();
        cut.InvokeAsync(() => selectBar.Instance.Change.InvokeAsync("List"));
        cut.WaitForState(() => cut.FindAll(".custom-view-container.list").Count > 0, TimeSpan.FromSeconds(2));

        // Act - Click first item, then Shift+Click third item
        var items = cut.FindAll(".custom-view-item");
        Assert.IsGreaterThanOrEqualTo(3, items.Count, "Expected 3 items rendered.");

        items[0].Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { ShiftKey = false });
        items[2].Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { ShiftKey = true });

        // Assert
        var selectedItems = cut.FindAll(".custom-view-item.selected");
        Assert.HasCount(3, selectedItems, "Expected all 3 items to be selected via Shift+Click.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_DetailsView_Sorting_IsAvailable_FromGrid()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("B.txt", "C:\\B.txt", FileSystemItemType.File, 100, DateTime.Now, false),
                    new FileSystemItem("A.txt", "C:\\A.txt", FileSystemItemType.File, 100, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        
        cut.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, new object[] { "C:\\" }));
        cut.WaitForState(() => cut.Markup.Contains("B.txt"), TimeSpan.FromSeconds(2));

        // Details view is default. Ensure grid supports sorting.
        var dataGrid = cut.FindComponent<RadzenDataGrid<FileSystemItem>>();
        Assert.IsTrue(dataGrid.Instance.AllowSorting, "Expected DataGrid to allow sorting in Details View.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_Keyboard_CtrlA_SelectsAllItems()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("File1.txt", "C:\\File1.txt", FileSystemItemType.File, 100, DateTime.Now, false),
                    new FileSystemItem("File2.txt", "C:\\File2.txt", FileSystemItemType.File, 100, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        
        cut.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, new object[] { "C:\\" }));
        cut.WaitForState(() => cut.Markup.Contains("File1.txt"), TimeSpan.FromSeconds(2));

        // Switch out of details to access custom view and test container easier
        var selectBar = cut.FindComponent<RadzenSelectBar<string>>();
        cut.InvokeAsync(() => selectBar.Instance.Change.InvokeAsync("List"));
        cut.WaitForState(() => cut.FindAll(".custom-view-container.list").Count > 0, TimeSpan.FromSeconds(2));

        // Act - Simulate Ctrl+A via container DOM
        cut.Find("#file-container").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { CtrlKey = true, Key = "a" });

        // Assert
        var selectedItems = cut.FindAll(".custom-view-item.selected");
        Assert.HasCount(2, selectedItems, "Expected all items to be selected on Ctrl+A.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Home_Keyboard_ShiftArrow_SelectsRange()
    {
        // Arrange
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list?path=C%3A%5C"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("File1.txt", "C:\\File1.txt", FileSystemItemType.File, 100, DateTime.Now, false),
                    new FileSystemItem("File2.txt", "C:\\File2.txt", FileSystemItemType.File, 100, DateTime.Now, false),
                    new FileSystemItem("File3.txt", "C:\\File3.txt", FileSystemItemType.File, 100, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            return null!;
        };
        var cut = SetupAndRender(mockHttp);
        
        cut.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, new object[] { "C:\\" }));
        cut.WaitForState(() => cut.Markup.Contains("File1.txt"), TimeSpan.FromSeconds(2));

        // Switch to 'List' view (so items are in custom view container)
        var selectBar = cut.FindComponent<RadzenSelectBar<string>>();
        cut.InvokeAsync(() => selectBar.Instance.Change.InvokeAsync("List"));
        cut.WaitForState(() => cut.FindAll(".custom-view-container.list").Count > 0, TimeSpan.FromSeconds(2));

        // Let's grab the container
        var container = cut.Find("#file-container");

        // Act - Arrow down once to focus item 0
        container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowDown" });
        
        // Then shift-arrow down twice to select indices 0,1,2
        container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });
        container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });

        // Assert
        var selectedItems = cut.FindAll(".custom-view-item.selected");
        Assert.HasCount(3, selectedItems, "Expected 3 items to be selected via Shift+Arrow.");
    }
}

