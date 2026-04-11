using System.Net;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using Radzen.Blazor;
using WebFileExplorer.Client.Pages;
using WebFileExplorer.Shared.Models;
using Microsoft.AspNetCore.Components.Web;

namespace WebFileExplorer.Tests.Unit.UI;

[TestClass]
public class HomePhase4Tests : BunitContext
{
    private Mock<DialogService> _dialogServiceMock = null!;
    private Mock<NotificationService> _notificationServiceMock = null!;
    private bool _apiCalled;
    private HttpResponseMessage _apiResponse = null!;

    private IRenderedComponent<Home> SetupAndRender()
    {
        
        
        _apiCalled = false;

                
        
        Services.AddSingleton(sp => new Mock<DialogService>(sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>(), sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        Services.AddSingleton(sp => new Mock<NotificationService>(sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()));
        var mockHttp = (HttpRequestMessage req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/list"))
            {
                var items = new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }
                {
                    new FileSystemItem("Folder", "C:\\Folder", FileSystemItemType.Folder, 0, DateTime.Now, false),
                    new FileSystemItem("File.txt", "C:\\File.txt", FileSystemItemType.File, 100, DateTime.Now, false)
                };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(items)) };
            }
            if (req.RequestUri!.PathAndQuery.Contains("api/fileexplorer/roots"))
            {
                var dict = new List<DriveItem> { new DriveItem("C:\\", "C:\\", 1000, 1000) };
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(dict)) };
            }
            
            _apiCalled = true;
            return _apiResponse ?? new HttpResponseMessage(HttpStatusCode.OK);
        };

        var httpClient = new HttpClient(new CustomMockHttpMessageHandler(mockHttp))
        {
            BaseAddress = new Uri("http://localhost/")
        };

        Services.AddSingleton(httpClient);
        Services.AddSingleton(new Mock<ILogger<Home>>().Object);
        Services.AddSingleton<DialogService>(sp => { _dialogServiceMock = new Mock<DialogService>(sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>(), sp.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()); return _dialogServiceMock.Object; });
        Services.AddSingleton<NotificationService>(sp => { var ctor = typeof(NotificationService).GetConstructors()[0]; var args = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(ctor.GetParameters(), p => sp.GetRequiredService(p.ParameterType))); _notificationServiceMock = new Mock<NotificationService>(args); return _notificationServiceMock.Object; });
        Services.AddScoped<ContextMenuService>();
        Services.AddSingleton(new WebFileExplorer.Client.Services.ClipboardStateContainer());
        Services.AddSingleton(new WebFileExplorer.Client.Services.ClipboardStateContainer());
        Services.AddScoped<TooltipService>();

        var cut = Render<Home>();
        
        // Load initial directory
        cut.InvokeAsync(() => typeof(Home).GetMethod("LoadDirectoryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(cut.Instance, new object[] { "C:\\" }));
        cut.WaitForState(() => cut.Markup.Contains("File.txt"), TimeSpan.FromSeconds(2));
        
        return cut;
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Task3_CommandBar_CreateFolder_ExecutesApiMethod()
    {
        // AC-7.1 Create Folder
        _apiResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var cut = SetupAndRender();
        
        _dialogServiceMock.Setup(d => d.OpenAsync(It.IsAny<string>(), It.IsAny<Microsoft.AspNetCore.Components.RenderFragment<DialogService>>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync("NewFolderName");
            
        var buttons = cut.FindComponents<RadzenButton>();
        var createBtn = buttons.FirstOrDefault(b => b.Instance.Text == "New Folder");
        Assert.IsNotNull(createBtn);

        createBtn.Find("button").Click();
        
        Assert.IsTrue(_apiCalled, "API should be called to create folder.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Task3_CommandBar_Rename_ExecutesApiMethod()
    {
        // AC-7.2 Rename
        _apiResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var cut = SetupAndRender();
        
        _dialogServiceMock.Setup(d => d.OpenAsync(It.IsAny<string>(), It.IsAny<Microsoft.AspNetCore.Components.RenderFragment<DialogService>>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync("RenamedFile.txt");
            
        // Select an item
        var itemDivs = cut.FindAll(".custom-view-item");
        Assert.IsNotEmpty(itemDivs);
        itemDivs[1].Click(); // select File.txt

        var buttons = cut.FindComponents<RadzenButton>();
        var renameBtn = buttons.FirstOrDefault(b => b.Instance.Text == "Rename");
        Assert.IsNotNull(renameBtn);

        renameBtn.Find("button").Click();
        
        Assert.IsTrue(_apiCalled, "API should be called to rename item.");
    }
    
    [TestMethod]
    [TestCategory("Unit")]
    public void Task3_CommandBar_Delete_ExecutesApiMethod()
    {
        // AC-7.5 Delete
        _apiResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var cut = SetupAndRender();
        
        _dialogServiceMock.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ConfirmOptions>()))
            .ReturnsAsync(true);
            
        // Select an item
        var itemDivs = cut.FindAll(".custom-view-item");
        Assert.IsNotEmpty(itemDivs);
        itemDivs[1].Click(); // select File.txt

        var buttons = cut.FindComponents<RadzenButton>();
        var deleteBtn = buttons.FirstOrDefault(b => b.Instance.Text == "Delete");
        Assert.IsNotNull(deleteBtn);

        deleteBtn.Find("button").Click();
        
        Assert.IsTrue(_apiCalled, "API should be called to delete item.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Task3_Keyboard_ShiftDelete_ExecutesApiMethod_WithPermanentTrue()
    {
        // AC-7.5 Shift+Delete
        _apiResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var cut = SetupAndRender();
        
        string? confirmMessage = null;
        _dialogServiceMock.Setup(d => d.Confirm(It.IsAny<string>(), "Permanently Delete Items", It.IsAny<ConfirmOptions>()))
            .Callback<string, string, ConfirmOptions>((msg, title, opts) => confirmMessage = msg)
            .ReturnsAsync(true);
            
        // Select an item
        var itemDivs = cut.FindAll(".custom-view-item");
        Assert.IsNotEmpty(itemDivs);
        itemDivs[1].Click();

        cut.Find(".custom-view-container").KeyDown(new KeyboardEventArgs { Key = "Delete", ShiftKey = true });

        Assert.IsNotNull(confirmMessage, "Delete confirmation should be shown for permanent delete.");
        Assert.DoesNotContain("Recycle Bin is not implemented", confirmMessage, "Recycle bin warning is only for normal delete.");
        Assert.IsTrue(_apiCalled, "API should be called to delete item.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Task3_CommandBar_Rename_DisabledWhenMultipleSelected()
    {
        var cut = SetupAndRender();
        
        var itemDivs = cut.FindAll(".custom-view-item");
        Assert.IsGreaterThanOrEqualTo(itemDivs.Count, 2);
        
        // Select first
        itemDivs[0].Click(new MouseEventArgs { CtrlKey = false });
        // Select second
        itemDivs[1].Click(new MouseEventArgs { CtrlKey = true });

        var buttons = cut.FindComponents<RadzenButton>();
        var renameBtn = buttons.FirstOrDefault(b => b.Instance.Text == "Rename");
        
        Assert.IsTrue(renameBtn!.Instance.Disabled, "Rename should be disabled when multiple items are selected.");
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Task3_CommandBar_CreateFolder_HandlesErrorAndNotifies()
    {
        // AC-14.2 & AC-14.3 Error handling and fallback/recover
        _apiResponse = new HttpResponseMessage(HttpStatusCode.BadRequest) 
        { 
            Content = new StringContent(JsonSerializer.Serialize(Result<FileSystemItem>.Failure("Access Denied: Cannot create folder."))) 
        };
        
        var cut = SetupAndRender();
        
        _dialogServiceMock.Setup(d => d.OpenAsync(It.IsAny<string>(), It.IsAny<Microsoft.AspNetCore.Components.RenderFragment<DialogService>>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync("NewFolderName");

        NotificationMessage? notifiedError = null;
        _notificationServiceMock.Setup(ns => ns.Notify(It.IsAny<NotificationMessage>()))
            .Callback<NotificationMessage>(msg => notifiedError = msg);
            
        var buttons = cut.FindComponents<RadzenButton>();
        var createBtn = buttons.FirstOrDefault(b => b.Instance.Text == "New Folder");
        
        createBtn!.Find("button").Click();
        
        Assert.IsTrue(_apiCalled, "API should be called to create folder.");
        Assert.IsNotNull(notifiedError, "Should notify user about the error.");
        Assert.AreEqual(NotificationSeverity.Error, notifiedError!.Severity);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void Task3_CommandBar_Delete_HandlesErrorAndRecovers()
    {
        // AC-14.3 recovery flow test for delete.
        _apiResponse = new HttpResponseMessage(HttpStatusCode.Conflict) 
        { 
            Content = new StringContent(JsonSerializer.Serialize(Result.Failure("File is locked by another process."))) 
        };
        
        var cut = SetupAndRender();
        
        _dialogServiceMock.Setup(d => d.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ConfirmOptions>()))
            .ReturnsAsync(true);

        NotificationMessage? notifiedError = null;
        _notificationServiceMock.Setup(ns => ns.Notify(It.IsAny<NotificationMessage>()))
            .Callback<NotificationMessage>(msg => notifiedError = msg);
            
        // Select an item
        var itemDivs = cut.FindAll(".custom-view-item");
        itemDivs[1].Click();

        var buttons = cut.FindComponents<RadzenButton>();
        var deleteBtn = buttons.FirstOrDefault(b => b.Instance.Text == "Delete");
        
        deleteBtn!.Find("button").Click();
        
        Assert.IsTrue(_apiCalled, "API should be called to delete item.");
        Assert.IsNotNull(notifiedError, "Should notify user about the error.");
        Assert.AreEqual(NotificationSeverity.Error, notifiedError!.Severity);
        Assert.AreEqual("File is locked by another process.", notifiedError.Detail);
    }
}

