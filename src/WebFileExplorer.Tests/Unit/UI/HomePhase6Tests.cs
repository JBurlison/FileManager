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
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor;

namespace WebFileExplorer.Tests.Unit.UI;

[TestClass]
public class HomePhase6Tests : BunitContext
{
    private ClipboardStateContainer _clipboard = null!;
    private DialogService _dialogService = null!;
    private ContextMenuService _contextMenuService = null!;

    [TestInitialize]
    public void Setup()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
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
        var httpClient = new HttpClient(new CustomMockHttpMessageHandler(mockHttpHandler))
        {
            BaseAddress = new Uri("http://localhost/")
        };
        Services.AddSingleton(httpClient);

        _dialogService = Services.GetRequiredService<DialogService>();
        _contextMenuService = Services.GetRequiredService<ContextMenuService>();

        return Render<Home>();
    }

    [TestMethod]
    public void ContextMenu_OnTreeNode_OpensContextMenu()
    {
        bool menuOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        _contextMenuService.OnOpen += (args, options) => { menuOpened = true; };

        comp.WaitForState(() => comp.Markup.Contains("C:"));

        var node = comp.FindAll("div.tree-node-content").FirstOrDefault(div => div.TextContent.Contains("C:"));
        Assert.IsNotNull(node, "Could not find tree node for C:");

        // Act
        node.ContextMenu();

        // Assert
        Assert.IsTrue(menuOpened, "Context menu should have been triggered for tree node.");
    }

    [TestMethod]
    public void ContextMenu_OnRow_OpensContextMenuWithValidItems()
    {
        bool menuOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/roots") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
        });

        _contextMenuService.OnOpen += (args, options) => { menuOpened = true; };

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        var addressInput = comp.FindAll(".rz-textbox").First();
        addressInput.Change("C:\\");
        comp.Find("button[title='Go']").Click();

        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var row = comp.FindAll("tr").FirstOrDefault(tr => tr.TextContent.Contains("test.txt"));
        Assert.IsNotNull(row, "Could not find row for test.txt");

        // Act
        row.ContextMenu();

        // Assert
        Assert.IsTrue(menuOpened, "Context menu should have been triggered.");
    }

    [TestMethod]
    public async Task Keyboard_F2_TriggersRenameAction()
    {
        bool dialogOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        _dialogService.OnOpen += (title, type, options, defaultOptions) => 
        {
            if (title == "Rename") dialogOpened = true;
        };

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var gridComponent = comp.FindComponent<RadzenDataGrid<FileSystemItem>>();
        var item = gridComponent.Instance.Data?.FirstOrDefault(i => i.Name == "test.txt");
        if (item != null)
        {
            await comp.InvokeAsync(async () => 
            {
                await gridComponent.Instance.SelectRow(item);
                await gridComponent.Instance.RowSelect.InvokeAsync(item);
            });
        }

        // Act
        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        if (mainContainer != null)
        {
            mainContainer.KeyDown(new KeyboardEventArgs { Key = "F2" });
        }
        else 
        {
            gridComponent.Find("tr").KeyDown(new KeyboardEventArgs { Key = "F2" });
        }

        // Assert
        Assert.IsTrue(dialogOpened, "F2 should trigger rename dialog.");
    }

    [TestMethod]
    public async Task Keyboard_Delete_TriggersDeleteAction()
    {
        bool dialogOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        _dialogService.OnOpen += (title, type, options, defaultOptions) => 
        {
            if (title != null && title.Contains("Delete")) dialogOpened = true;
        };

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var gridComponent = comp.FindComponent<RadzenDataGrid<FileSystemItem>>();
        var item = gridComponent.Instance.Data?.FirstOrDefault(i => i.Name == "test.txt");
        if (item != null)
        {
            await comp.InvokeAsync(async () => 
            {
                await gridComponent.Instance.SelectRow(item);
                await gridComponent.Instance.RowSelect.InvokeAsync(item);
            });
        }

        // Act
        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        if (mainContainer != null)
        {
            mainContainer.KeyDown(new KeyboardEventArgs { Key = "Delete" });
        }
        else 
        {
            gridComponent.Find("tr").KeyDown(new KeyboardEventArgs { Key = "Delete" });
        }

        // Assert
        Assert.IsTrue(dialogOpened, "Delete key should trigger delete confirmation dialog.");
    }

    [TestMethod]
    public async Task Keyboard_ShiftF10_OpensContextMenu()
    {
        bool menuOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        _contextMenuService.OnOpen += (args, options) => { menuOpened = true; };

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        if (mainContainer != null)
        {
            mainContainer.KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        }
        else 
        {
            comp.FindComponent<RadzenDataGrid<FileSystemItem>>().Find("tr").KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        }

        Assert.IsTrue(menuOpened, "Shift+F10 should trigger context menu.");
    }

    [TestMethod]
    public async Task Keyboard_TypeToFocus_FocusesItem()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { 
                    new FileSystemItem("apple.txt", @"C:\apple.txt", FileSystemItemType.File, 10, DateTimeOffset.UtcNow, false),
                    new FileSystemItem("banana.txt", @"C:\banana.txt", FileSystemItemType.File, 20, DateTimeOffset.UtcNow, false),
                    new FileSystemItem("cherry.txt", @"C:\cherry.txt", FileSystemItemType.File, 30, DateTimeOffset.UtcNow, false)
                }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("banana.txt"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        if (mainContainer != null)
        {
            mainContainer.KeyDown(new KeyboardEventArgs { Key = "b" });
        }
        else 
        {
            comp.FindComponent<RadzenDataGrid<FileSystemItem>>().Find("tr").KeyDown(new KeyboardEventArgs { Key = "b" });
        }

        // Wait to make sure rendering updates
        await Task.Delay(100); // small delay to allow event handling
        
        // We can inspect private state or just trust the selection works. To be sure it selects: we check if grid has banana as selected.
        var gridComponent = comp.FindComponent<RadzenDataGrid<FileSystemItem>>();
        var selectedValue = gridComponent.Instance.Value?.Cast<FileSystemItem>().FirstOrDefault();
        
        Assert.IsNotNull(selectedValue, "An item should be selected.");
        Assert.AreEqual("banana.txt", selectedValue.Name, "banana.txt should be focused and selected.");
    }

    [TestMethod]
    public async Task Keyboard_CtrlShiftN_TriggersNewFolderDialog()
    {
        bool dialogOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        _dialogService.OnOpen += (title, type, options, defaultOptions) => 
        {
            if (title == "New Folder") dialogOpened = true;
        };

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        if (mainContainer != null)
        {
            mainContainer.KeyDown(new KeyboardEventArgs { Key = "N", CtrlKey = true, ShiftKey = true });
        }
        else
        {
            comp.FindComponent<RadzenDataGrid<FileSystemItem>>().Find("tr").KeyDown(new KeyboardEventArgs { Key = "N", CtrlKey = true, ShiftKey = true });
        }

        Assert.IsTrue(dialogOpened, "Ctrl+Shift+N should trigger New Folder dialog.");
    }

    [TestMethod]
    public async Task Keyboard_Backspace_NavigatesBack()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
            {
                var path = req.RequestUri.Query;
                if (path.Contains("subdir"))
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("file2.txt", @"C:\test\subdir\file2.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
                
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("subdir", @"C:\test\subdir", FileSystemItemType.Folder, 0, DateTimeOffset.UtcNow, false) }) };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        
        // Go to C:\test
        comp.FindAll(".rz-textbox").First().Change("C:\\test");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("subdir"));

        // Go to subdir
        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        var subDirRow = comp.FindComponent<RadzenDataGrid<FileSystemItem>>().FindAll("tr").FirstOrDefault(tr => tr.TextContent.Contains("subdir"));
        subDirRow?.DoubleClick();
        comp.WaitForState(() => comp.Markup.Contains("file2.txt"));

        // Press Backspace
        mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "Backspace" });

        comp.WaitForState(() => comp.Markup.Contains("subdir")); // Wait until we see 'subdir' row again

        StringAssert.Contains(comp.Markup, "subdir", "Backspace should navigate back to previous folder.");
    }

    [TestMethod]
    public async Task Keyboard_NavigationKeys_UpdatesFocus()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { 
                    new FileSystemItem("file1.txt", @"C:\file1.txt", FileSystemItemType.File, 10, DateTimeOffset.UtcNow, false),
                    new FileSystemItem("file2.txt", @"C:\file2.txt", FileSystemItemType.File, 20, DateTimeOffset.UtcNow, false),
                    new FileSystemItem("file3.txt", @"C:\file3.txt", FileSystemItemType.File, 30, DateTimeOffset.UtcNow, false)
                }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("file1.txt"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        var gridComponent = comp.FindComponent<RadzenDataGrid<FileSystemItem>>();

        // End
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "End" });
        await Task.Delay(50);
        var selectedValue = gridComponent.Instance.Value?.Cast<FileSystemItem>().FirstOrDefault();
        Assert.AreEqual("file3.txt", selectedValue?.Name, "End key should select the last item.");

        // Home
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "Home" });
        await Task.Delay(50);
        selectedValue = gridComponent.Instance.Value?.Cast<FileSystemItem>().FirstOrDefault();
        Assert.AreEqual("file1.txt", selectedValue?.Name, "Home key should select the first item.");

        // PageDown
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        await Task.Delay(50);
        selectedValue = gridComponent.Instance.Value?.Cast<FileSystemItem>().FirstOrDefault();
        Assert.AreEqual("file3.txt", selectedValue?.Name, "PageDown key should select item further down."); // in a list of 3 it should hit end

        // PageUp
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        await Task.Delay(50);
        selectedValue = gridComponent.Instance.Value?.Cast<FileSystemItem>().FirstOrDefault();
        Assert.AreEqual("file1.txt", selectedValue?.Name, "PageUp key should select item further up.");
    }

    [TestMethod]
    public void Keyboard_ContextMenuKey_OpensContextMenu()
    {
        bool menuOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        _contextMenuService.OnOpen += (args, options) => { menuOpened = true; };

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        if (mainContainer != null)
        {
            mainContainer.KeyDown(new KeyboardEventArgs { Key = "ContextMenu" });
        }
        else 
        {
            comp.FindComponent<RadzenDataGrid<FileSystemItem>>().Find("tr").KeyDown(new KeyboardEventArgs { Key = "ContextMenu" });
        }

        Assert.IsTrue(menuOpened, "ContextMenu key should trigger context menu.");
    }

    [TestMethod]
    public async Task Buttons_DisabledState_ReflectsSelectionAndClipboardState()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { 
                    new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false),
                    new FileSystemItem("test2.txt", @"C:\test2.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false)
                }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        
        // Initial state: No path selected
        var newFolderBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("New Folder"));
        Assert.IsTrue(newFolderBtn?.HasAttribute("disabled"), "New Folder should be disabled initially");

        // Load a directory
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        // No item selected state
        var renameBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Rename"));
        var deleteBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
        var copyBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Copy"));
        
        Assert.IsTrue(renameBtn?.HasAttribute("disabled"), "Rename should be disabled when no selection");
        Assert.IsTrue(deleteBtn?.HasAttribute("disabled"), "Delete should be disabled when no selection");
        Assert.IsTrue(copyBtn?.HasAttribute("disabled"), "Copy should be disabled when no selection");

        // Select an item
        var gridComponent = comp.FindComponent<RadzenDataGrid<FileSystemItem>>();
        var item = gridComponent.Instance.Data?.FirstOrDefault(i => i.Name == "test.txt");
        if (item != null)
        {
            await comp.InvokeAsync(async () => 
            {
                await gridComponent.Instance.SelectRow(item);
                await gridComponent.Instance.RowSelect.InvokeAsync(item);
            });
        }

        // Test single item selection
        renameBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Rename"));
        deleteBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
        copyBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Copy"));

        Assert.IsFalse(renameBtn?.HasAttribute("disabled"), "Rename should be enabled for single selection");
        Assert.IsFalse(deleteBtn?.HasAttribute("disabled"), "Delete should be enabled for selection");
        Assert.IsFalse(copyBtn?.HasAttribute("disabled"), "Copy should be enabled for selection");
        
        // Multi selection
        await comp.InvokeAsync(async () => 
        {
            if (item != null)
            {
                _clipboard.SetState(false, new List<string> { item.FullPath ?? "" }, "C:\\"); // copy an item so Paste gets enabled
            }
        });
        await Task.Delay(50); // let UI update

        var pasteBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Paste"));
        Assert.IsFalse(pasteBtn?.HasAttribute("disabled"), "Paste should be enabled since clipboard has items");
    }

    [TestMethod]
    public async Task Keyboard_TextInput_DoesNotTriggerHotkeys()
    {
        bool dialogOpened = false;

        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        _dialogService.OnOpen += (title, type, options, defaultOptions) => 
        {
            if (title == "New Folder") dialogOpened = true;
        };

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        var addressInput = comp.FindAll(".rz-textbox").First();
        addressInput.Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        // Simulate typing into the address bar text input
        // Using "N" with Ctrl+Shift logic in RadzenTextBox
        addressInput.KeyDown(new KeyboardEventArgs { Key = "N", CtrlKey = true, ShiftKey = true });
        
        await Task.Delay(50); // wait for potential async operations

        // Because RadzenTextBox stops propagation or is not within the file-container's listener,
        // it should NOT trigger the main file container's shortcut.
        Assert.IsFalse(dialogOpened, "Shortcuts should not fire when a text input is focused and receiving keys.");
    }

    [TestMethod]
    public void Keyboard_AltLeft_TriggersNavigateBack()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list?path=C%3A%5Ctest%5Csubdir") == true)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("file2.txt", @"C:\test\subdir\file2.txt", FileSystemItemType.File, 10, DateTimeOffset.UtcNow, false) }) };
            }
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list?path=C%3A%5Ctest") == true)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("subdir", @"C:\test\subdir", FileSystemItemType.Folder, 0, DateTimeOffset.UtcNow, false) }) };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        
        comp.FindAll(".rz-textbox").First().Change("C:\\test");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("subdir"));

        var subDirRow = comp.FindAll("tr").FirstOrDefault(tr => tr.TextContent.Contains("subdir"));
        subDirRow?.DoubleClick();
        comp.WaitForState(() => comp.Markup.Contains("file2.txt"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });

        comp.WaitForState(() => comp.Markup.Contains("subdir"));

        StringAssert.Contains(comp.Markup, "subdir", "Alt+Left should navigate back.");
    }
    
    [TestMethod]
    public void Keyboard_AltRight_TriggersNavigateForward()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list?path=C%3A%5Ctest%5Csubdir") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("file2.txt", @"C:\test\subdir\file2.txt", FileSystemItemType.File, 10, DateTimeOffset.UtcNow, false) }) };
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list?path=C%3A%5Ctest") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("subdir", @"C:\test\subdir", FileSystemItemType.Folder, 0, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        
        comp.FindAll(".rz-textbox").First().Change("C:\\test");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("subdir"));

        var subDirRow = comp.FindAll("tr").FirstOrDefault(tr => tr.TextContent.Contains("subdir"));
        subDirRow?.DoubleClick();
        comp.WaitForState(() => comp.Markup.Contains("file2.txt"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        
        // Go back first
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });
        comp.WaitForState(() => comp.Markup.Contains("subdir"));
        
        // Go forward
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });
        comp.WaitForState(() => comp.Markup.Contains("file2.txt"));

        StringAssert.Contains(comp.Markup, "file2.txt", "Alt+Right should navigate forward.");
    }

    [TestMethod]
    public void Keyboard_AltUp_TriggersNavigateUp()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list?path=C%3A%5Ctest%5Csubdir") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("file2.txt", @"C:\test\subdir\file2.txt", FileSystemItemType.File, 10, DateTimeOffset.UtcNow, false) }) };
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list?path=C%3A%5Ctest") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("subdir", @"C:\test\subdir", FileSystemItemType.Folder, 0, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        
        comp.FindAll(".rz-textbox").First().Change("C:\\test\\subdir");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("file2.txt"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        comp.WaitForState(() => comp.Markup.Contains("subdir"));

        StringAssert.Contains(comp.Markup, "subdir", "Alt+Up should navigate to parent directory.");
    }

    [TestMethod]
    public async Task Keyboard_Enter_OpensItem()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list?path=C%3A%5Ctest%5Csubdir") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("file2.txt", @"C:\test\subdir\file2.txt", FileSystemItemType.File, 10, DateTimeOffset.UtcNow, false) }) };
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list?path=C%3A%5Ctest") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("subdir", @"C:\test\subdir", FileSystemItemType.Folder, 0, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\test");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("subdir"));

        var gridComponent = comp.FindComponent<RadzenDataGrid<FileSystemItem>>();
        var item = gridComponent.Instance.Data?.FirstOrDefault(i => i.Name == "subdir");
        if (item != null)
        {
            await comp.InvokeAsync(async () => 
            {
                await gridComponent.Instance.SelectRow(item);
            });
        }

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        
        // Select it via keyboard to focus an item
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        await Task.Delay(50);
        
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        comp.WaitForState(() => comp.Markup.Contains("file2.txt"));

        StringAssert.Contains(comp.Markup, "file2.txt", "Enter should open the selected folder.");
    }

    [TestMethod]
    public void Keyboard_F5_RefreshesCurrentDirectory()
    {
        int fetchCount = 0;
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains(@"api/fileexplorer/list") == true)
            {
                fetchCount++;
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("file.txt", @"C:\test\file.txt", FileSystemItemType.File, 10, DateTimeOffset.UtcNow, false) }) };
            }
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\test");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("file.txt"));
        
        int preCount = fetchCount;

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "F5" });

        // Component should re-fetch
        comp.WaitForState(() => fetchCount > preCount);

        Assert.IsGreaterThan(fetchCount, preCount, "F5 should trigger a directory reload.");
    }

    [TestMethod]
    public async Task Keyboard_CtrlC_CopiesItem()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var gridComponent = comp.FindComponent<RadzenDataGrid<FileSystemItem>>();
        var item = gridComponent.Instance.Data?.FirstOrDefault(i => i.Name == "test.txt");
        if (item != null)
        {
            await comp.InvokeAsync(async () => 
            {
                await gridComponent.Instance.SelectRow(item);
                await gridComponent.Instance.RowSelect.InvokeAsync(item);
            });
        }

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "c", CtrlKey = true });

        Assert.IsFalse(_clipboard.IsCut, "Operation should be Copy");
        Assert.AreEqual(1, _clipboard.Items.Count);
        StringAssert.EndsWith(_clipboard.Items.First(), "test.txt");
    }
    
    [TestMethod]
    public async Task Keyboard_CtrlX_CutsItem()
    {
        var comp = SetupAndRender(req =>
        {
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("test.txt", @"C:\test.txt", FileSystemItemType.File, 1024, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("test.txt"));

        var gridComponent = comp.FindComponent<RadzenDataGrid<FileSystemItem>>();
        var item = gridComponent.Instance.Data?.FirstOrDefault(i => i.Name == "test.txt");
        if (item != null)
        {
            await comp.InvokeAsync(async () => 
            {
                await gridComponent.Instance.SelectRow(item);
                await gridComponent.Instance.RowSelect.InvokeAsync(item);
            });
        }

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "x", CtrlKey = true });

        Assert.IsTrue(_clipboard.IsCut, "Operation should be Cut");
        Assert.AreEqual(1, _clipboard.Items.Count);
        StringAssert.EndsWith(_clipboard.Items.First(), "test.txt");
    }

    [TestMethod]
    public void Keyboard_CtrlV_PastesItem()
    {
        bool requestSent = false;
        _clipboard.SetState(false, new[] { @"C:\test.txt" }, "C:\\");

        var comp = SetupAndRender(req =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/clipboard") == true)
            {
                requestSent = true;
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
            }
            if (req.RequestUri?.PathAndQuery.Contains("api/fileexplorer/list") == true)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new FileSystemItem("subdir", @"C:\subdir", FileSystemItemType.Folder, 0, DateTimeOffset.UtcNow, false) }) };
            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = JsonContent.Create(new[] { new DriveItem("C:", "C:\\", 100, 100) }) };
        });

        comp.WaitForState(() => comp.Markup.Contains("C:"));
        comp.FindAll(".rz-textbox").First().Change("C:\\subdir");
        comp.Find("button[title='Go']").Click();
        comp.WaitForState(() => comp.Markup.Contains("subdir"));

        var mainContainer = comp.FindAll("div").FirstOrDefault(d => d.Id == "file-container");
        mainContainer?.KeyDown(new KeyboardEventArgs { Key = "v", CtrlKey = true });

        // Waiting for the paste API to have been called.
        comp.WaitForState(() => requestSent);
        Assert.IsTrue(requestSent, "Ctrl+V should trigger a paste API request.");
    }
}




