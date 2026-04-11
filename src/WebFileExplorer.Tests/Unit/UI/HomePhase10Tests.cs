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
public class HomePhase10Tests : BunitContext
{
    private ClipboardStateContainer _clipboard = null!;

    [TestInitialize]
    public void Setup()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("open", _ => true).SetVoidResult();
        JSInterop.SetupVoid("eval", _ => true).SetVoidResult();
        JSInterop.SetupVoid("registerKeyboardInterceptor", _ => true).SetVoidResult();

        _clipboard = new ClipboardStateContainer();
        Services.AddSingleton(_clipboard);
        Services.AddScoped<DialogService>();
        Services.AddScoped<NotificationService>();
        Services.AddSingleton(Mock.Of<ILogger<Home>>());
        Services.AddScoped<TooltipService>();
        Services.AddScoped<ContextMenuService>();
    }

    private IRenderedComponent<Home> SetupAndRender(string? path = null)
    {
        Func<HttpRequestMessage, HttpResponseMessage> handler = req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
                
            if (req.Method == HttpMethod.Get && req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/recyclebin") == true)
            {
                var rbItems = new[] { 
                    new RecycleBinItem("deleted.txt", "C:\\deleted.txt", DateTimeOffset.Now, "C:\\deleted.txt", 10)
                };
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(rbItems) };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("Not Found") };
        };

        var httpClient = new HttpClient(new CustomMockHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        Services.AddSingleton(httpClient);

        if (path != null)
        {
            return Render<Home>(parameters => parameters.Add(p => p.Path, path));
        }
        return Render<Home>();
    }

    [TestMethod]
    public void RecycleBin_Navigation_ShowsSpecialColumns()
    {
        var comp = SetupAndRender("::RecycleBin::");
        comp.WaitForState(() => comp.FindComponents<RadzenDataGrid<FileSystemItem>>().Count > 0);
        
        comp.WaitForState(() => comp.FindComponents<RadzenDataGridColumn<FileSystemItem>>().Any(c => c.Instance.Title == "Original Path"));

        var columns = comp.FindComponents<RadzenDataGridColumn<FileSystemItem>>();
        Assert.IsTrue(columns.Any(c => c.Instance.Title == "Original Path"), "Missing Original Path column");
        Assert.IsTrue(columns.Any(c => c.Instance.Title == "Date Deleted"), "Missing Date Deleted column");
        
        // Also check if Restore and Empty Bin buttons are enabled/visible in theory 
        // (but RadzenButton might not be easily queryable for Disabled state unless we check the instance)
        var buttons = comp.FindComponents<RadzenButton>();
        var emptyBinBtn = buttons.FirstOrDefault(b => b.Instance.Text == "Empty Bin");
        Assert.IsNotNull(emptyBinBtn, "Empty Bin button missing");
        Assert.IsFalse(emptyBinBtn.Instance.Disabled, "Empty Bin button should be enabled in Recycle Bin view");
    }

    [TestMethod]
    public void RecycleBin_WhenNotSupported_DisplaysClearMessage()
    {
        Func<HttpRequestMessage, HttpResponseMessage> handler = req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
                
            if (req.Method == HttpMethod.Get && req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/recyclebin") == true)
            {
                return new HttpResponseMessage(HttpStatusCode.NotImplemented) { Content = new StringContent("Recycle Bin is unsupported or unavailable on this system.") };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("Not Found") };
        };

        var httpClient = new HttpClient(new CustomMockHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        Services.AddSingleton(httpClient);

        var comp = Render<Home>(parameters => parameters.Add(p => p.Path, "::RecycleBin::"));
        
        // Wait for the alert to appear since it's an async load
        comp.WaitForState(() => comp.FindComponents<RadzenAlert>().Count > 0);

        var alert = comp.FindComponent<RadzenAlert>();
        StringAssert.Contains(alert.Markup, "Recycle Bin is unsupported or unavailable on this system.");
    }
}
