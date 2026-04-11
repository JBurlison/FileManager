using System.Net;
using System.Net.Http.Json;
using Moq;
using WebFileExplorer.Server.Controllers;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebFileExplorer.Tests.Unit.Controllers;

[TestClass]
public class FileExplorerControllerReviewGapsTests
{
    private Mock<IFileSystemProvider> _mockProvider = null!;
    private Mock<IArchiveService> _mockArchiveService = null!;
    private Mock<IRecycleBinService> _mockRecycleBinService = null!;
    private Mock<ILogger<FileExplorerController>> _mockLogger = null!;
    private FileExplorerController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockProvider = new Mock<IFileSystemProvider>();
        _mockArchiveService = new Mock<IArchiveService>();
        _mockRecycleBinService = new Mock<IRecycleBinService>();
        _mockLogger = new Mock<ILogger<FileExplorerController>>();
        
        _controller = new FileExplorerController(
            _mockProvider.Object,
            _mockArchiveService.Object,
            _mockRecycleBinService.Object,
            _mockLogger.Object
        );
    }

    [TestMethod]
    public async Task GetProperties_ValidPath_ReturnsProperties()
    {
        var props = new FileProperties("test.txt", "C:\\test", "text/plain", 1024, 1, 0, DateTime.Now, DateTime.Now, DateTime.Now, false, false, false, false);
        _mockProvider.Setup(p => p.GetPropertiesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(props);

        var result = await _controller.GetProperties("C:\\test\\test.txt");

        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var r = okResult.Value as Result<FileProperties>;
        Assert.IsNotNull(r);
        Assert.IsTrue(r.IsSuccess);
        Assert.AreEqual("test.txt", r.Value?.Name);
    }
}
