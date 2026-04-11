using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WebFileExplorer.Client.Pages;
using WebFileExplorer.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace WebFileExplorer.Tests.Unit.UI
{
    [TestClass]
    public class HomeAccessibilityTests : Bunit.TestContext
    {
        [TestInitialize]
        public void Setup()
        {
            var mockHttp = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(mockHttp.Object) { BaseAddress = new Uri("http://localhost") };
            
            Services.AddSingleton(httpClient);
            Services.AddSingleton(new Mock<ILogger<Home>>().Object);
            Services.AddSingleton(new Mock<IJSRuntime>().Object);
            Services.AddSingleton<DialogService>();
            Services.AddSingleton<NotificationService>();
            Services.AddSingleton<ContextMenuService>();
            Services.AddSingleton<WebFileExplorer.Client.Services.ClipboardStateContainer>();
        }

        [TestMethod]
        public void Home_ShouldHave_AriaAttributes_For_Accessibility()
        {
            // Arrange
            var comp = Render<Home>();

            // wait for async init to avoid exceptions in some cases, although not required if mock not throwing
            comp.WaitForState(() => true);

            // Act & Assert
            // Toolbar
            var toolbarRow = comp.Find(".command-bar-row");
            Assert.AreEqual("toolbar", toolbarRow.GetAttribute("role"));
            Assert.AreEqual("Action Toolbar", toolbarRow.GetAttribute("aria-label"));

            // Breadcrumb Address Bar
            var upBtn = comp.Find("button[title='Up']");
            Assert.AreEqual("Navigate Up", upBtn.GetAttribute("aria-label"));

            var addressInput = comp.Find(".wfe-breadcrumb-container input");
            Assert.AreEqual("Address", addressInput.GetAttribute("aria-label"));

            // Search
            var searchInput = comp.Find("input[placeholder='Search...']");
            Assert.AreEqual("Search items", searchInput.GetAttribute("aria-label"));

            // View Mode
            var viewModeSelect = comp.Find(".rz-selectbar");
            Assert.AreEqual("Select View Mode", viewModeSelect.GetAttribute("aria-label"));
            
            // File container
            var fileContainer = comp.Find("#file-container");
            Assert.AreEqual("region", fileContainer.GetAttribute("role"));
            Assert.AreEqual("File List", fileContainer.GetAttribute("aria-label"));
        }
    }
}
