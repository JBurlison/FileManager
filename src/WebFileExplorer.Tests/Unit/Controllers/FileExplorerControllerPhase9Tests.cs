using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebFileExplorer.Server.Controllers;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Controllers;

[TestClass]
public class FileExplorerControllerPhase9Tests
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
    public async Task Compress_ValidRequest_CallsServiceAndReturnsOk()
    {
        // Arrange
        var request = new CompressRequest
        {
            SourcePaths = new List<string> { "C:\\Source\\file1.txt" },
            DestinationZipPath = "C:\\Source\\archive.zip"
        };

        _providerMock.Setup(p => p.IsAuthorizedPathAsync(It.IsAny<string>())).ReturnsAsync(true);
        
        // Act
        var result = await _controller.Compress(request);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);
        _archiveServiceMock.Verify(a => a.CreateZipAsync(request.SourcePaths, request.DestinationZipPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Extract_ValidRequest_CallsServiceAndReturnsOk()
    {
        // Arrange
        var request = new ExtractRequest
        {
            ZipPath = "C:\\Source\\archive.zip",
            DestinationFolderPath = "C:\\Source\\Folder",
            Resolution = WebFileExplorer.Shared.Models.ConflictResolution.Overwrite
        };

        _providerMock.Setup(p => p.IsAuthorizedPathAsync(It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _controller.Extract(request);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);
        _archiveServiceMock.Verify(a => a.ExtractZipAsync(request.ZipPath, request.DestinationFolderPath, request.Resolution, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Extract_InvalidDataException_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExtractRequest
        {
            ZipPath = "C:\\Source\\invalid.zip",
            DestinationFolderPath = "C:\\Source\\Folder",
            Resolution = WebFileExplorer.Shared.Models.ConflictResolution.None
        };

        _providerMock.Setup(p => p.IsAuthorizedPathAsync(It.IsAny<string>())).ReturnsAsync(true);
        _archiveServiceMock.Setup(a => a.ExtractZipAsync(request.ZipPath, request.DestinationFolderPath, request.Resolution, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("Invalid ZIP file"));

        // Act
        var result = await _controller.Extract(request);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult, "Should return BadRequest when InvalidDataException is thrown");
        Assert.AreEqual(400, badRequestResult.StatusCode);
    }
}
