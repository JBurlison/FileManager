using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebFileExplorer.Server.Controllers;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Controllers;

[TestClass]
public class FileExplorerControllerPhase7Tests
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
    public async Task Download_EmptyPath_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Download(string.Empty);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequestResult);
        Assert.AreEqual("Path is required.", badRequestResult.Value);
    }

    [TestMethod]
    public async Task Download_FileNotFound_ReturnsNotFound()
    {
        // Arrange
        var path = "C:\\Missing\\file.txt";
        _providerMock.Setup(x => x.GetFileStreamAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Stream>.Failure("File not found."));

        // Act
        var result = await _controller.Download(path);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        Assert.IsNotNull(notFoundResult);
        Assert.AreEqual("File not found.", notFoundResult.Value);
    }

    [TestMethod]
    public async Task Download_ValidPath_ReturnsFileStreamResult()
    {
        // Arrange
        var path = "C:\\Data\\file.txt";
        var stream = new MemoryStream();
        _providerMock.Setup(x => x.GetFileStreamAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Stream>.Success(stream));

        // Act
        var result = await _controller.Download(path);

        // Assert
        var fileResult = result as FileStreamResult;
        Assert.IsNotNull(fileResult);
        Assert.AreEqual("text/plain", fileResult.ContentType);
        Assert.AreEqual("file.txt", fileResult.FileDownloadName);
        Assert.AreSame(stream, fileResult.FileStream);
    }

    [TestMethod]
    public async Task Download_HtmlInline_ForcesTextPlain()
    {
        // Arrange
        var path = "C:\\Data\\index.html";
        var stream = new MemoryStream();
        _providerMock.Setup(x => x.GetFileStreamAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Stream>.Success(stream));

        // Act
        var result = await _controller.Download(path, inline: true);

        // Assert
        var fileResult = result as FileStreamResult;
        Assert.IsNotNull(fileResult);
        Assert.AreEqual("text/plain", fileResult.ContentType);
        Assert.IsNull(fileResult.FileDownloadName); // No download name for inline
        Assert.AreSame(stream, fileResult.FileStream);
    }

    [TestMethod]
    public async Task Download_UnknownExtension_UsesOctetStream()
    {
        // Arrange
        var path = "C:\\Data\\file.unknown";
        var stream = new MemoryStream();
        _providerMock.Setup(x => x.GetFileStreamAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Stream>.Success(stream));

        // Act
        var result = await _controller.Download(path);

        // Assert
        var fileResult = result as FileStreamResult;
        Assert.IsNotNull(fileResult);
        Assert.AreEqual("application/octet-stream", fileResult.ContentType);
        Assert.AreEqual("file.unknown", fileResult.FileDownloadName);
    }
}
