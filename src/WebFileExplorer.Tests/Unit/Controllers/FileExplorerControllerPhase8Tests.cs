using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebFileExplorer.Server.Controllers;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Controllers;

[TestClass]
public class FileExplorerControllerPhase8Tests
{
    private Mock<IFileSystemProvider> _providerMock = null!;
    private Mock<IArchiveService> _archiveServiceMock = null!;
    private Mock<ILogger<FileExplorerController>> _loggerMock = null!;
    private FileExplorerController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _providerMock = new Mock<IFileSystemProvider>();
        _archiveServiceMock = new Mock<IArchiveService>();
        _loggerMock = new Mock<ILogger<FileExplorerController>>();
        _controller = new FileExplorerController(_providerMock.Object, _archiveServiceMock.Object, WebFileExplorer.Tests.Unit.Controllers.MockHelper.GetMockRecycleBinService(), _loggerMock.Object);
    }

    [TestMethod]
    public async Task Search_EmptyPath_ReturnsBadRequest()
    {
        var result = await _controller.Search(string.Empty, "test");
        var badRequestResult = result.Result as BadRequestObjectResult;
        
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual("Path is required.", badRequestResult.Value);
    }

    [TestMethod]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        var result = await _controller.Search("C:\\Auth", string.Empty);
        var badRequestResult = result.Result as BadRequestObjectResult;
        
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual("Search query is required.", badRequestResult.Value);
    }

    [TestMethod]
    public async Task Search_ValidRequest_ReturnsOkWithItems()
    {
        var expectedItems = new PagedResult<FileSystemItem>
        {
            Items = [new FileSystemItem("test.txt", "C:\\Auth\\test.txt", FileSystemItemType.File, 100, DateTimeOffset.Now, false)]
        };
        
        _providerMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var result = await _controller.Search("C:\\Auth", "test");
        var okResult = result.Result as OkObjectResult;
        
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);
        Assert.AreEqual(expectedItems, okResult.Value);
    }

    [TestMethod]
    public async Task Search_UnauthorizedPath_ReturnsForbidden()
    {
        _providerMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Test auth fail"));

        var result = await _controller.Search("C:\\Windows", "test");
        var objectResult = result.Result as ObjectResult;
        
        Assert.IsNotNull(objectResult);
        Assert.AreEqual(403, objectResult.StatusCode);
    }

    [TestMethod]
    public async Task Search_OperationCancelled_Returns499()
    {
        _providerMock
            .Setup(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var result = await _controller.Search("C:\\Auth", "test");
        var objectResult = result.Result as ObjectResult;
        
        Assert.IsNotNull(objectResult);
        Assert.AreEqual(499, objectResult.StatusCode);
    }
}
