using System.Net;
using System.Net.Http.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using WebFileExplorer.Client.Pages;
using WebFileExplorer.Client.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.UI;

[TestClass]
public class HomePhase5Tests : BunitContext
{
    private ClipboardStateContainer _clipboard = null!;
    private DialogService _dialogService = null!;

    [TestInitialize]
    public void Setup()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _clipboard = new ClipboardStateContainer();
        _dialogService = new DialogService(Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>(), Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()); // Radzen DialogService takes NavigationManager, JSRuntime, mock if possible or just use AddScoped
        
        Services.AddSingleton(_clipboard);
        Services.AddScoped<DialogService>();
        Services.AddScoped<NotificationService>();
        Services.AddSingleton(Mock.Of<ILogger<Home>>());
        Services.AddScoped<TooltipService>();
        Services.AddScoped<ContextMenuService>();
    }

    private IRenderedComponent<Home> SetupAndRender(Func<HttpRequestMessage, HttpResponseMessage>? mockHttpHandler = null)
    {
        var httpClient = new HttpClient(new CustomMockHttpMessageHandler(mockHttpHandler))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        Services.AddSingleton(httpClient);
        _dialogService = Services.GetRequiredService<DialogService>();

        return Render<Home>();
    }

    [TestMethod]
    public void Cut_PlacesItemsInClipboardAsCut()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
            }
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));

        var inputs = comp.FindAll(".rz-textbox");
        var addressInput = inputs.First();
        addressInput.Change("C:\\");
        comp.Find("button[title='Go']").Click();

        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var td = comp.FindAll("td").First(td => td.TextContent.Contains("test.txt"));
        td.Click();

        var cutBtn = comp.FindAll("button").First(b => b.TextContent.Contains("Cut") && !b.HasAttribute("disabled"));
        cutBtn.Click();

        Assert.IsTrue(_clipboard.IsCut);
        CollectionAssert.Contains(_clipboard.Items.ToList(), @"C:\test.txt");
    }

    [TestMethod]
    public void Copy_PlacesItemsInClipboardAsCopy()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
            
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };

            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        
        var inputs = comp.FindAll(".rz-textbox");
        var addressInput = inputs.First();
        addressInput.Change("C:\\");
        comp.Find("button[title='Go']").Click();

        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var td = comp.FindAll("td").First(td => td.TextContent.Contains("test.txt"));
        td.Click();

        var copyBtn = comp.FindAll("button").First(b => b.TextContent.Contains("Copy") && !b.HasAttribute("disabled"));
        copyBtn.Click();

        Assert.IsFalse(_clipboard.IsCut);
        CollectionAssert.Contains(_clipboard.Items.ToList(), @"C:\test.txt");
    }

    [TestMethod]
    public void Paste_CallsApiWithOverwriteFalse_OnSuccessLoadsDirectory()
    {
        bool postCalled = false;
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("D:", "D:\\", 100, 100) }) };
            
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new PagedResult<FileSystemItem> { Items = [] })
                };

            if (req.Method == HttpMethod.Post && req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/copy") == true)
            {
                postCalled = true;
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new Result { IsSuccess = true }) };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        _clipboard.SetState(false, new[] { @"C:\test.txt" }, @"C:\");

        comp.WaitForState(() => comp.Markup.Contains("D:"));

        var inputs = comp.FindAll(".rz-textbox");
        inputs.First().Change("D:\\");
        comp.Find("button[title='Go']").Click();

        comp.WaitForState(() => comp.FindAll("button").Any(b => b.TextContent.Contains("Paste") && !b.HasAttribute("disabled")));

        var pasteBtn = comp.FindAll("button").First(b => b.TextContent.Contains("Paste"));
        pasteBtn.Click();

        Assert.IsTrue(postCalled);
    }

    [TestMethod]
    public void Paste_WhenDestinationExists_ShowsConflictDialog()
    {
        bool dialogOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("D:", "D:\\", 100, 100) }) };
            
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new PagedResult<FileSystemItem> { Items = [] })
                };

            if (req.Method == HttpMethod.Post && req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/copy") == true)
            {
                var reqContentTask = req.Content!.ReadFromJsonAsync<ClipboardOperationRequest>();
                reqContentTask.Wait();
                var opReq = reqContentTask.Result;

                if (opReq?.Resolution == ConflictResolution.None)
                {
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new Result { IsSuccess = false, ErrorMessage = "Destination item already exists." }) };
                }
                else
                {
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new Result { IsSuccess = true }) };
                }
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        _dialogService.OnOpen += (title, type, options, defaultOptions) => 
        {
            dialogOpened = true;
        };

        _clipboard.SetState(false, new[] { @"C:\test.txt" }, @"C:\");

        comp.WaitForState(() => comp.Markup.Contains("D:"));

        var inputs = comp.FindAll(".rz-textbox");
        inputs.First().Change("D:\\");
        comp.Find("button[title='Go']").Click();

        comp.WaitForState(() => comp.FindAll("button").Any(b => b.TextContent.Contains("Paste") && !b.HasAttribute("disabled")));

        var pasteBtn = comp.FindAll("button").First(b => b.TextContent.Contains("Paste"));
        pasteBtn.Click();

        Assert.IsTrue(dialogOpened);
    }

    [TestMethod]
    public void DragDrop_OnFolder_SetsClipboardAndPastes()
    {
        bool postCalled = false;
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
            
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { 
                    new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false),
                    new FileSystemItem("subfolder", @"C:\subfolder", FileSystemItemType.Folder, 0, DateTimeOffset.UtcNow, false)
                }) };
            }

            if (req.Method == HttpMethod.Post && req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/move") == true)
            {
                postCalled = true;
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new Result { IsSuccess = true }) };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));

        var inputs = comp.FindAll(".rz-textbox");
        inputs.First().Change("C:\\");
        comp.Find("button[title='Go']").Click();

        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var rows = comp.FindAll(".rz-data-grid-data tbody tr");
        var fileRow = rows.First(r => r.TextContent.Contains("test.txt"));
        var folderRow = rows.First(r => r.TextContent.Contains("subfolder"));

        var fileDraggable = fileRow.QuerySelector("div[draggable='true']");
        var folderDraggable = folderRow.QuerySelector("div[draggable='true']");
        
        fileDraggable?.DragStart();
        folderDraggable?.Drop();

        Assert.IsTrue(postCalled);
    }

    [TestMethod]
    public void Paste_OnSuccess_ShouldSelectPastedItems()
    {
        bool postCalled = false;
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("D:", "D:\\", 100, 100) }) };
            
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
            {
                var qs = req.RequestUri?.Query;
                if (!string.IsNullOrEmpty(qs) && qs.Contains(@"D:%5C"))
                {
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { 
                        new FileSystemItem("test.txt", @"D:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false)
                    }) };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new PagedResult<FileSystemItem> { Items = [] })
                };
            }

            if (req.Method == HttpMethod.Post && req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/copy") == true)
            {
                postCalled = true;
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new Result { IsSuccess = true }) };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        _clipboard.SetState(false, new[] { @"C:\source\test.txt" }, @"C:\source\");

        comp.WaitForState(() => comp.Markup.Contains("D:"));

        var inputs = comp.FindAll(".rz-textbox");
        inputs.First().Change("D:\\");
        comp.Find("button[title='Go']").Click();

        comp.WaitForState(() => comp.FindAll("button").Any(b => b.TextContent.Contains("Paste") && !b.HasAttribute("disabled")));

        var pasteBtn = comp.FindAll("button").First(b => b.TextContent.Contains("Paste"));
        pasteBtn.Click();

        comp.WaitForState(() => postCalled);
        
        // Assert new item should be selected visually (have rz-state-highlight or something similar in DataGrid)
        // RadzenDataGrid selected rows usually have the class 'rz-state-highlight'
        comp.WaitForState(() => comp.FindAll("tr.rz-state-highlight").Any());
        
        var selectedRows = comp.FindAll("tr.rz-state-highlight").ToList();
        Assert.AreNotEqual(0, selectedRows.Count);
        StringAssert.Contains(selectedRows[0].TextContent, "test.txt");
    }
}
