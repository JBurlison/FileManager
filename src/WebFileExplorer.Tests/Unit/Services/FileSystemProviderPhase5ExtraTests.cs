using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebFileExplorer.Server.Configuration;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Services;

[TestClass]
public class FileSystemProviderPhase5ExtraTests
{
    private Mock<ILogger<FileSystemProvider>> _loggerMock = null!;
    private Mock<IOptions<ExplorerOptions>> _optionsMock = null!;
    private FileSystemProvider _provider = null!;
    private string _testDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "WebFileExplorerTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);

        _loggerMock = new Mock<ILogger<FileSystemProvider>>();
        _optionsMock = new Mock<IOptions<ExplorerOptions>>();
        
        var options = new ExplorerOptions
        {
            AuthorizedRoots = new[] { _testDir }
        };
        _optionsMock.Setup(x => x.Value).Returns(options);
        
        _provider = new FileSystemProvider(_optionsMock.Object, _loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    [TestMethod]
    public async Task CopyAsync_DescendantTarget_ReturnsFailure()
    {
        var sourceDir = Path.Combine(_testDir, "source");
        var destDir = Path.Combine(sourceDir, "dest");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(destDir);

        var result = await _provider.CopyAsync(new[] { sourceDir }, destDir, ConflictResolution.None, CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "descendant", "Actual error message: " + result.ErrorMessage);
    }

    [TestMethod]
    public async Task CopyAsync_ConflictResolutionSkip_SkipsExistingFile()
    {
        var sourceDir = Path.Combine(_testDir, "source");
        var destDir = Path.Combine(_testDir, "dest");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(destDir);

        var sourceFile = Path.Combine(sourceDir, "file.txt");
        var destFile = Path.Combine(destDir, "file.txt");
        
        File.WriteAllText(sourceFile, "new content");
        File.WriteAllText(destFile, "old content");

        var result = await _provider.CopyAsync(new[] { sourceFile }, destDir, ConflictResolution.Skip, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        
        var content = File.ReadAllText(destFile);
        Assert.AreEqual("old content", content);
    }

    [TestMethod]
    public async Task CopyAsync_ConflictResolutionOverwrite_OverwritesExistingFile()
    {
        var sourceDir = Path.Combine(_testDir, "source");
        var destDir = Path.Combine(_testDir, "dest");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(destDir);

        var sourceFile = Path.Combine(sourceDir, "file.txt");
        var destFile = Path.Combine(destDir, "file.txt");
        
        File.WriteAllText(sourceFile, "new content");
        File.WriteAllText(destFile, "old content");

        var result = await _provider.CopyAsync(new[] { sourceFile }, destDir, ConflictResolution.Overwrite, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        
        var content = File.ReadAllText(destFile);
        Assert.AreEqual("new content", content);
    }

    [TestMethod]
    public async Task MoveAsync_DescendantTarget_ReturnsFailure()
    {
        var sourceDir = Path.Combine(_testDir, "source");
        var destDir = Path.Combine(sourceDir, "dest");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(destDir);

        var result = await _provider.MoveAsync(new[] { sourceDir }, destDir, ConflictResolution.None, CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.ErrorMessage!, "descendant", "Actual error message: " + result.ErrorMessage);
    }
}
