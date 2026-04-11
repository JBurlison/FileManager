using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebFileExplorer.Server.Controllers;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Controllers;

[TestClass]
public class FileExplorerControllerPhase5Tests
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
    public async Task Copy_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new ClipboardOperationRequest
        {
            Items = new[] { "C:\\Source\\file.txt" },
            DestinationPath = "C:\\Dest",
            Overwrite = false,
            Resolution = ConflictResolution.None
        };
        var expectedResult = Result.Success();
        _providerMock.Setup(x => x.CopyAsync(request.Items, request.DestinationPath, request.Resolution, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.Copy(request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var actualResult = okResult.Value as Result;
        Assert.IsNotNull(actualResult);
        Assert.IsTrue(actualResult.IsSuccess);
    }

    [TestMethod]
    public async Task Copy_WithOverwriteResolution_ForwardsToProvider()
    {
        // Arrange
        var request = new ClipboardOperationRequest
        {
            Items = new[] { "C:\\Source\\file.txt" },
            DestinationPath = "C:\\Dest",
            Overwrite = true,
            Resolution = ConflictResolution.Overwrite
        };
        var expectedResult = Result.Success();
        _providerMock.Setup(x => x.CopyAsync(request.Items, request.DestinationPath, ConflictResolution.Overwrite, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.Copy(request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        _providerMock.Verify(x => x.CopyAsync(request.Items, request.DestinationPath, ConflictResolution.Overwrite, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Move_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new ClipboardOperationRequest
        {
            Items = new[] { "C:\\Source\\file.txt" },
            DestinationPath = "C:\\Dest",
            Overwrite = false,
            Resolution = ConflictResolution.None
        };
        var expectedResult = Result.Success();
        _providerMock.Setup(x => x.MoveAsync(request.Items, request.DestinationPath, request.Resolution, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.Move(request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var actualResult = okResult.Value as Result;
        Assert.IsNotNull(actualResult);
        Assert.IsTrue(actualResult.IsSuccess);
    }

    [TestMethod]
    public async Task Move_WithOverwriteResolution_ForwardsToProvider()
    {
        // Arrange
        var request = new ClipboardOperationRequest
        {
            Items = new[] { "C:\\Source\\file.txt" },
            DestinationPath = "C:\\Dest",
            Overwrite = true,
            Resolution = ConflictResolution.Overwrite
        };
        var expectedResult = Result.Success();
        _providerMock.Setup(x => x.MoveAsync(request.Items, request.DestinationPath, ConflictResolution.Overwrite, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _controller.Move(request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        _providerMock.Verify(x => x.MoveAsync(request.Items, request.DestinationPath, ConflictResolution.Overwrite, It.IsAny<CancellationToken>()), Times.Once);
    }
}
