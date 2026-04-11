using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebFileExplorer.Server.Configuration;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Services;

[TestClass]
public class FileSystemProviderPhase5Tests
{
    private Mock<ILogger<FileSystemProvider>> _loggerMock = null!;
    private Mock<IOptions<ExplorerOptions>> _optionsMock = null!;
    private FileSystemProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<FileSystemProvider>>();
        _optionsMock = new Mock<IOptions<ExplorerOptions>>();
        
        var options = new ExplorerOptions
        {
            AuthorizedRoots = new[] { "C:\\TestDir" }
        };
        _optionsMock.Setup(x => x.Value).Returns(options);
        
        _provider = new FileSystemProvider(_optionsMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task CopyAsync_FileDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var sourcePaths = new[] { "C:\\TestDir\\DoesNotExist.txt" };
        var destPath = "C:\\TestDir\\Dest";

        // Act
        var result = await _provider.CopyAsync(sourcePaths, destPath, ConflictResolution.None, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task MoveAsync_FileDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var sourcePaths = new[] { "C:\\TestDir\\DoesNotExist.txt" };
        var destPath = "C:\\TestDir\\Dest";

        // Act
        var result = await _provider.MoveAsync(sourcePaths, destPath, ConflictResolution.None, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
    }
}
