using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebFileExplorer.Server.Configuration;
using WebFileExplorer.Server.Services;

namespace WebFileExplorer.Tests.Unit.Services;

[TestClass]
public class FileSystemProviderPhase8Tests
{
    private Mock<IOptions<ExplorerOptions>> _optionsMock = null!;
    private Mock<ILogger<FileSystemProvider>> _loggerMock = null!;
    private FileSystemProvider _provider = null!;
    private string _testRoot = null!;

    [TestInitialize]
    public void Setup()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "WebFileExplorerPhase8Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);
        
        var options = new ExplorerOptions
        {
            AuthorizedRoots = new[] { _testRoot }
        };
        
        _optionsMock = new Mock<IOptions<ExplorerOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(options);
        
        _loggerMock = new Mock<ILogger<FileSystemProvider>>();
        
        _provider = new FileSystemProvider(
            _optionsMock.Object,
            _loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [TestMethod]
    public async Task SearchAsync_ValidPathAndQuery_ReturnsMatchingItems()
    {
        // Arrange
        var subDir = Path.Combine(_testRoot, "sub1");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_testRoot, "test1.txt"), "content");
        File.WriteAllText(Path.Combine(_testRoot, "other.txt"), "content");
        File.WriteAllText(Path.Combine(subDir, "test2.txt"), "content");
        
        // Act
        var results = (await _provider.SearchAsync(_testRoot, "test")).ToList();
        
        // Assert
        if (results.Count != 2) Assert.Fail();
        Assert.IsTrue(results.Any(r => r.Name == "test1.txt" && r.FullPath.Contains(_testRoot)));
        Assert.IsTrue(results.Any(r => r.Name == "test2.txt" && r.FullPath.Contains(subDir)));
    }

    [TestMethod]
    public async Task SearchAsync_UnauthorizedPath_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var unauthorizedPath = "C:\\Windows"; // Or anywhere outside _testRoot
        
        // Act & Assert
        try
        {
            await _provider.SearchAsync(unauthorizedPath, "anything", cancellationToken: default);
            Assert.Fail();
        }
        catch (UnauthorizedAccessException) { }
    }
}
