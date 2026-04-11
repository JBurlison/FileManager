using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebFileExplorer.Server.Controllers;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Controllers;

[TestClass]
public class FileExplorerControllerTests
{
    private Mock<IFileSystemProvider>? _providerMock;

    [TestInitialize]
    public void Setup()
    {
        _providerMock = new Mock<IFileSystemProvider>();
    }

    [TestMethod]
    public async Task GetAuthorizedRoots_Always_ReturnsOkWithRoots()
    {
        // Arrange
        var expectedRoots = new List<DriveItem>
        {
            new DriveItem("Test1", @"C:\Test1", 100, 1000),
            new DriveItem("Test2", @"C:\Test2", 200, 2000)
        };

        _providerMock!.Setup(p => p.GetAuthorizedRootsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRoots);

        var loggerMock = new Mock<ILogger<FileExplorerController>>();
        var controller = new FileExplorerController(_providerMock.Object, new Mock<IArchiveService>().Object, WebFileExplorer.Tests.Unit.Controllers.MockHelper.GetMockRecycleBinService(), loggerMock.Object);

        // Act
        var actionResult = await controller.GetAuthorizedRoots(CancellationToken.None);

        // Assert
        Assert.IsNotNull(actionResult);
        var okResult = actionResult.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);

        var returnValue = okResult.Value as IEnumerable<DriveItem>;
        Assert.IsNotNull(returnValue);
        Assert.AreEqual(expectedRoots, returnValue);
    }

    [TestMethod]
    public async Task ListDirectories_WithValidPath_ReturnsOk()
    {
        var path = @"C:\valid";
        var expectedItems = new PagedResult<FileSystemItem>
        {
            Items = [new FileSystemItem(path, "valid", FileSystemItemType.Folder, 0, DateTime.Now, false)]
        };
        _providerMock!.Setup(p => p.ListDirectoriesAsync(path, false, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(expectedItems);
        var loggerMock = new Mock<ILogger<FileExplorerController>>();
        var controller = new FileExplorerController(_providerMock.Object, new Mock<IArchiveService>().Object, WebFileExplorer.Tests.Unit.Controllers.MockHelper.GetMockRecycleBinService(), loggerMock.Object);

        var result = await controller.ListDirectories(path, false, null, null, CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);
        Assert.AreEqual(expectedItems, okResult.Value);
    }

    [TestMethod]
    public async Task ListDirectories_WithEmptyPath_ReturnsBadRequest()
    {
        var loggerMock = new Mock<ILogger<FileExplorerController>>();
        var controller = new FileExplorerController(_providerMock!.Object, new Mock<IArchiveService>().Object, WebFileExplorer.Tests.Unit.Controllers.MockHelper.GetMockRecycleBinService(), loggerMock.Object);

        var result = await controller.ListDirectories("", false, null, null, CancellationToken.None);

        var badRequest = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual(400, badRequest.StatusCode);
    }

    [TestMethod]
    public async Task ListDirectories_Unauthorized_Returns403()
    {
        var path = @"C:\secret";
        _providerMock!.Setup(p => p.ListDirectoriesAsync(path, false, null, null, It.IsAny<CancellationToken>())).ThrowsAsync(new UnauthorizedAccessException());
        var loggerMock = new Mock<ILogger<FileExplorerController>>();
        var controller = new FileExplorerController(_providerMock.Object, new Mock<IArchiveService>().Object, WebFileExplorer.Tests.Unit.Controllers.MockHelper.GetMockRecycleBinService(), loggerMock.Object);

        var result = await controller.ListDirectories(path, false, null, null, CancellationToken.None);

        var objectResult = result.Result as ObjectResult;
        Assert.IsNotNull(objectResult);
        Assert.AreEqual(403, objectResult.StatusCode);
    }

    [TestMethod]
    public async Task ListDirectories_NotFound_ReturnsNotFound()
    {
        var path = @"C:\nonexistent";
        _providerMock!.Setup(p => p.ListDirectoriesAsync(path, false, null, null, It.IsAny<CancellationToken>())).ThrowsAsync(new System.IO.DirectoryNotFoundException());
        var loggerMock = new Mock<ILogger<FileExplorerController>>();
        var controller = new FileExplorerController(_providerMock.Object, new Mock<IArchiveService>().Object, WebFileExplorer.Tests.Unit.Controllers.MockHelper.GetMockRecycleBinService(), loggerMock.Object);

        var result = await controller.ListDirectories(path, false, null, null, CancellationToken.None);

        var notFound = result.Result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
        Assert.AreEqual(404, notFound.StatusCode);
    }

    [TestMethod]
    public async Task ListDirectories_Exception_Returns500()
    {
        var path = @"C:\error";
        _providerMock!.Setup(p => p.ListDirectoriesAsync(path, false, null, null, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("test error"));
        var loggerMock = new Mock<ILogger<FileExplorerController>>();
        var controller = new FileExplorerController(_providerMock.Object, new Mock<IArchiveService>().Object, WebFileExplorer.Tests.Unit.Controllers.MockHelper.GetMockRecycleBinService(), loggerMock.Object);

        var result = await controller.ListDirectories(path, false, null, null, CancellationToken.None);

        var objectResult = result.Result as ObjectResult;
        Assert.IsNotNull(objectResult);
        Assert.AreEqual(500, objectResult.StatusCode);
    }
}
