using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using WebFileExplorer.Client.Pages;
using WebFileExplorer.Client.Services;
using Radzen;

namespace WebFileExplorer.Tests.Unit.Client.Pages
{
    [TestClass]
    public class HomeZipWorkflowsTests : BunitContext
    {
        [TestMethod]
        [Ignore("Test asserted only that markup was non-null, which provides no signal about the compress workflow. Needs rewrite to drive the context menu and verify the POST to api/fileexplorer/compress. Tracked for follow-up.")]
        public async Task ContextMenu_CompressToZip_MultipleFiles_Success()
        {
            await Task.CompletedTask;
        }

        [TestMethod]
        [Ignore("Test asserted only that markup was non-null, which provides no signal about the extract workflow. Needs rewrite to drive the context menu and verify the POST to api/fileexplorer/extract. Tracked for follow-up.")]
        public async Task ContextMenu_ExtractAll_ZipFileSelected_Success()
        {
            await Task.CompletedTask;
        }

        [TestMethod]
        [Ignore("Test claimed to verify error surfacing on an invalid zip but only asserted markup was non-null. Needs rewrite that drives the workflow and inspects the NotificationService. Tracked for follow-up.")]
        public async Task Extract_SurfacesError_WhenGivenInvalidZip()
        {
            await Task.CompletedTask;
        }

        [TestMethod]
        [Ignore("Test claimed to validate overwrite/destination behavior but only asserted markup was non-null. Needs rewrite that drives the workflow and inspects the outgoing extract request body. Tracked for follow-up.")]
        public async Task Extract_ValidatesOverwriteBehavior_AndDestinationSelection()
        {
            await Task.CompletedTask;
        }
    }
}
