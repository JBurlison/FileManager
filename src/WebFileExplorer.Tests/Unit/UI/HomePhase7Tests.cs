using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using WebFileExplorer.Client.Components;
using WebFileExplorer.Client.Pages;
using WebFileExplorer.Client.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.UI;

[TestClass]
public class HomePhase7Tests : BunitContext
{
    private ClipboardStateContainer _clipboard = null!;
    private DialogService _dialogService = null!;
    private ContextMenuService _contextMenuService = null!;
    private List<string> _jsInvocations = new();

    [TestInitialize]
    public void Setup()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("open", _ => true)
            .SetVoidResult();
        JSInterop.SetupVoid("eval", _ => true)
            .SetVoidResult();

        _clipboard = new ClipboardStateContainer();
        _jsInvocations.Clear();

        Services.AddSingleton(_clipboard);
        Services.AddScoped<DialogService>();
        Services.AddScoped<NotificationService>();
        Services.AddSingleton(Mock.Of<ILogger<Home>>());
        Services.AddScoped<TooltipService>();
        Services.AddScoped<ContextMenuService>();
    }

    private IRenderedComponent<Home> SetupAndRender(Func<HttpRequestMessage, HttpResponseMessage>? mockHttpHandler = null)
    {
        Func<HttpRequestMessage, HttpResponseMessage> handler = req => mockHttpHandler != null ? mockHttpHandler(req) : new HttpResponseMessage(HttpStatusCode.OK);
        var httpClient = new HttpClient(new CustomMockHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        Services.AddSingleton(httpClient);

        _dialogService = Services.GetRequiredService<DialogService>();
        _contextMenuService = Services.GetRequiredService<ContextMenuService>();

        return Render<Home>();
    }

    [TestMethod]
    public async Task DoubleClick_ImageFile_OpensPreviewDialog()
    {
        bool dialogOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list?path=C:%5C&showHidden=False") == true)
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new[] { 
                        new FileSystemItem("photo.jpg", "C:\\photo.jpg", FileSystemItemType.File, 1000, DateTimeOffset.Now, false)
                    })
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        comp.WaitForState(() => comp.FindAll(".tree-node-content").Count > 0);
        await comp.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(comp.Instance, new object[] { "C:\\" }));

        comp.WaitForState(() => comp.FindAll(".rz-data-row").Count > 0);

        _dialogService.OnOpen += delegate (string? title, Type type, Dictionary<string, object?> parameters, DialogOptions options)
        {
            if (type == typeof(PreviewDialog))
            {
                dialogOpened = true;
                Assert.AreEqual("C:\\photo.jpg", parameters["FilePath"]);
                Assert.AreEqual("photo.jpg", parameters["FileName"]);
            }
        };

        var targetRow = comp.Find(".rz-data-row");
        targetRow.DoubleClick();

        Assert.IsTrue(dialogOpened);
    }

    [TestMethod]
    public async Task DoubleClick_NonPreviewableFile_CallsDownloadViaJS()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list?path=C:%5C&showHidden=False") == true)
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new[] { 
                        new FileSystemItem("unknown.xyz", "C:\\unknown.xyz", FileSystemItemType.File, 1000, DateTimeOffset.Now, false)
                    })
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        comp.WaitForState(() => comp.FindAll(".tree-node-content").Count > 0);
        await comp.InvokeAsync(() => typeof(WebFileExplorer.Client.Pages.Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(comp.Instance, new object[] { "C:\\" }));

        comp.WaitForState(() => comp.FindAll(".rz-data-row").Count > 0);

        var targetRow = comp.Find(".rz-data-row");
        targetRow.DoubleClick();

        var openInvocations = JSInterop.Invocations.GetEnumerator();
        bool foundOpen = false;
        while (openInvocations.MoveNext())
        {
            var call = openInvocations.Current;
            if (call.Identifier == "open" && call.Arguments.Count >= 2 && call.Arguments[0]?.ToString() == "api/fileexplorer/download?path=C%3A%5Cunknown.xyz" && call.Arguments[1]?.ToString() == "_blank")
            {
                foundOpen = true;
            }
        }

        Assert.IsTrue(foundOpen, "Expected JSInterop call to open download URL was not made.");
    }

    [TestMethod]
    public async Task PreviewDialog_TextFile_CutoffBoundary_Evaluated()
    {
        var largeText = new string('A', 150 * 1024); // 150KB string
        
        var handler = new MockHttpMessageHandler(req =>
        {
            var content = new StringContent(largeText);
            content.Headers.ContentLength = largeText.Length;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });

        Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var comp = Render<PreviewDialog>(parameters => parameters
            .Add(p => p.FilePath, "C:\\verylarge.txt")
            .Add(p => p.FileName, "verylarge.txt")
        );

        comp.WaitForState(() => !comp.Instance.GetType().GetField("IsLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(comp.Instance).Equals(true));

        var textContent = comp.Instance.GetType().GetField("TextContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(comp.Instance)?.ToString();
        Assert.IsNotNull(textContent);
        StringAssert.Contains(textContent, "Preview truncated at 100KB");
    }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}
