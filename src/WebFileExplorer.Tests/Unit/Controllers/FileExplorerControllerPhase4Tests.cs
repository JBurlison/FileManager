using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebFileExplorer.Server.Controllers;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Controllers;

[TestClass]
public class FileExplorerControllerPhase4Tests
{
    private Mock<IFileSystemProvider>? _providerMock;
    private Mock<IArchiveService>? _archiveServiceMock;
    private Mock<ILogger<FileExplorerController>>? _loggerMock;
    private FileExplorerController? _controller;

    [TestInitialize]
    public void Setup()
    {
        _providerMock = new Mock<IFileSystemProvider>();
        _archiveServiceMock = new Mock<IArchiveService>();
        _loggerMock = new Mock<ILogger<FileExplorerController>>();
        _controller = new FileExplorerController(_providerMock.Object, _archiveServiceMock.Object, WebFileExplorer.Tests.Unit.Controllers.MockHelper.GetMockRecycleBinService(), _loggerMock.Object);
    }

    [TestMethod]
    public async Task CreateFolder_ValidParameters_ReturnsOk()
    {
        // Arrange
        var item = new FileSystemItem(@"C:\Parent\New Folder", "New Folder", FileSystemItemType.Folder, 0, DateTime.Now, false);
        var expectedResult = Result<FileSystemItem>.Success(item);
        _providerMock!.Setup(p => p.CreateFolderAsync(@"C:\Parent", "New Folder", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller!.CreateFolder(@"C:\Parent", "New Folder");

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);
        Assert.AreEqual(expectedResult, okResult.Value);
    }

    [TestMethod]
    public async Task CreateFolder_EmptyParentPath_ReturnsBadRequest()
    {
        // Act
        var result = await _controller!.CreateFolder("", "New Folder");

        // Assert
        var badRequest = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual(400, badRequest.StatusCode);
    }

    [TestMethod]
    public async Task Rename_ValidParameters_ReturnsOk()
    {
        // Arrange
        var item = new FileSystemItem(@"C:\Parent\Renamed", "Renamed", FileSystemItemType.Folder, 0, DateTime.Now, false);
        var expectedResult = Result<FileSystemItem>.Success(item);
        _providerMock!.Setup(p => p.RenameAsync(@"C:\Parent\Original", "Renamed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller!.Rename(@"C:\Parent\Original", "Renamed");

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);
        Assert.AreEqual(expectedResult, okResult.Value);
    }

    [TestMethod]
    public async Task Delete_ValidPath_ReturnsOk()
    {
        // Arrange
        var expectedResult = Result.Success();
        _providerMock!.Setup(p => p.DeleteAsync(@"C:\Parent\ToDelete", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller!.Delete(@"C:\Parent\ToDelete");

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);
        Assert.AreEqual(expectedResult, okResult.Value);
    }

    [TestMethod]
    public async Task Rename_EmptyPath_ReturnsBadRequest()
    {
        var result = await _controller!.Rename("", "NewName");
        
        var badRequest = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
    }

    [TestMethod]
    public async Task Delete_EmptyPath_ReturnsBadRequest()
    {
        var result = await _controller!.Delete("");
        
        var badRequest = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
    }

    [TestMethod]
    public async Task Rename_ProviderFails_ReturnsBadRequestWithError()
    {
        _providerMock!.Setup(p => p.RenameAsync(@"C:\Parent\Original", "Renamed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<FileSystemItem>.Failure("Access denied."));

        var result = await _controller!.Rename(@"C:\Parent\Original", "Renamed");

        var badResult = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badResult);
        var resValue = badResult.Value as Result<FileSystemItem>;
        Assert.IsNotNull(resValue);
        Assert.IsFalse(resValue.IsSuccess);
        Assert.AreEqual("Access denied.", resValue.ErrorMessage);
    }

    [TestMethod]
    public async Task Delete_ProviderFails_ReturnsBadRequestWithError()
    {
        _providerMock!.Setup(p => p.DeleteAsync(@"C:\Parent\ToDelete", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Access denied."));

        var result = await _controller!.Delete(@"C:\Parent\ToDelete");

        var badResult = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badResult);
        var resValue = badResult.Value as Result;
        Assert.IsNotNull(resValue);
        Assert.IsFalse(resValue.IsSuccess);
        Assert.AreEqual("Access denied.", resValue.ErrorMessage);
    }
}
