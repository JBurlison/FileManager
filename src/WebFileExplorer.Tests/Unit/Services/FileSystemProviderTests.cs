using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebFileExplorer.Server.Configuration;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Tests.Unit.Services;

[TestClass]
public class FileSystemProviderTests
{
    private Mock<ILogger<FileSystemProvider>>? _loggerMock;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<FileSystemProvider>>();
    }

    [TestMethod]
    public async Task GetAuthorizedRootsAsync_WithValidRoots_ReturnsFormattedRoots()
    {
        var testPath1 = Path.Combine(Path.GetTempPath(), "TestRoot1");
        var testPath2 = Path.Combine(Path.GetTempPath(), "TestRoot2");
        Directory.CreateDirectory(testPath1);
        Directory.CreateDirectory(testPath2);

        var options = new ExplorerOptions { AuthorizedRoots = new[] { testPath1, testPath2 } };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);

        try
        {
            var result = (await service.GetAuthorizedRootsAsync(CancellationToken.None)).ToList();
            Assert.HasCount(2, result);
            
            var expectedRoot1 = Path.GetFullPath(testPath1);
            Assert.AreEqual(expectedRoot1, result[0].RootDirectory);
            Assert.AreEqual(new DirectoryInfo(expectedRoot1).Name, result[0].Name);

            var expectedRoot2 = Path.GetFullPath(testPath2);
            Assert.AreEqual(expectedRoot2, result[1].RootDirectory);
        }
        finally
        {
            if (Directory.Exists(testPath1)) try { Directory.Delete(testPath1); } catch { }
            if (Directory.Exists(testPath2)) try { Directory.Delete(testPath2); } catch { }
        }
    }

    [TestMethod]
    public async Task GetAuthorizedRootsAsync_WithInvalidRoots_IgnoresInvalidRoots()
    {
        var testPath1 = Path.Combine(Path.GetTempPath(), "ValidRoot");
        Directory.CreateDirectory(testPath1);
        var invalidPath = @"Z:\NonExistentDrive\xyz123";

        var options = new ExplorerOptions { AuthorizedRoots = new[] { testPath1, invalidPath } };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);

        try
        {
            var result = (await service.GetAuthorizedRootsAsync(CancellationToken.None)).ToList();
            Assert.HasCount(1, result);
            Assert.AreEqual(Path.GetFullPath(testPath1), result[0].RootDirectory);
        }
        finally
        {
            if (Directory.Exists(testPath1)) try { Directory.Delete(testPath1); } catch { }
        }
    }

    [TestMethod]
    public async Task GetAuthorizedRootsAsync_WithEmptyRoots_ReturnsEmptyList()
    {
        var options = new ExplorerOptions { AuthorizedRoots = Array.Empty<string>() };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);
        var result = (await service.GetAuthorizedRootsAsync(CancellationToken.None)).ToList();
        Assert.IsEmpty(result);
    }
    
    [TestMethod]
    public async Task ListDirectoriesAsync_InsideAuthorizedRoot_ReturnsItems()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "AuthorizedRoot1");
        var subDir = Path.Combine(rootPath, "SubDir");
        var file = Path.Combine(rootPath, "file.txt");
        
        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(subDir);
        File.WriteAllText(file, "test");
        
        var options = new ExplorerOptions { AuthorizedRoots = new[] { rootPath } };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);
        
        try
        {
            var result = (await service.ListDirectoriesAsync(rootPath, false, CancellationToken.None)).ToList();
            Assert.IsTrue(result.Any(r => r.Name == "SubDir" && r.Type == FileSystemItemType.Folder));
            Assert.IsTrue(result.Any(r => r.Name == "file.txt" && r.Type == FileSystemItemType.File));
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
        }
    }
    
    [TestMethod]
    
    public async Task ListDirectoriesAsync_OutsideAuthorizedRoot_ThrowsUnauthorized()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "AuthorizedRoot2");
        var otherPath = Path.Combine(Path.GetTempPath(), "Unauthorized");
        
        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(otherPath);
        
        var options = new ExplorerOptions { AuthorizedRoots = new[] { rootPath } };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        
        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);
        
        try
        {
            try { await service.ListDirectoriesAsync(otherPath, false, CancellationToken.None); Assert.Fail(); } catch (UnauthorizedAccessException) { }
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
            if (Directory.Exists(otherPath)) Directory.Delete(otherPath, true);
        }
    }

    [TestMethod]
    public async Task CreateFolderAsync_InsideAuthorizedRoot_CreatesFolder()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "AuthorizedRootCreate");
        Directory.CreateDirectory(rootPath);
        
        var options = new ExplorerOptions { AuthorizedRoots = new[] { rootPath } };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);
        
        try
        {
            var result = await service.CreateFolderAsync(rootPath, "NewFolder");
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("NewFolder", result.Value!.Name);
            Assert.IsTrue(Directory.Exists(Path.Combine(rootPath, "NewFolder")));
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
        }
    }

    [TestMethod]
    public async Task CreateFolderAsync_OutsideAuthorizedRoot_ReturnsFailure()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "AuthorizedRootCreate2");
        var outsidePath = Path.Combine(Path.GetTempPath(), "OutsideRootCreate");
        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(outsidePath);
        
        var options = new ExplorerOptions { AuthorizedRoots = new[] { rootPath } };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);
        
        try
        {
            var result = await service.CreateFolderAsync(outsidePath, "NewFolder");
            Assert.IsFalse(result.IsSuccess);
            Assert.Contains("Access denied", result.ErrorMessage!);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
            if (Directory.Exists(outsidePath)) Directory.Delete(outsidePath, true);
        }
    }

    [TestMethod]
    public async Task RenameAsync_ValidFile_RenamesFile()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "AuthorizedRootRename");
        Directory.CreateDirectory(rootPath);
        var filePath = Path.Combine(rootPath, "oldName.txt");
        File.WriteAllText(filePath, "test");
        
        var options = new ExplorerOptions { AuthorizedRoots = new[] { rootPath } };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);
        
        try
        {
            var result = await service.RenameAsync(filePath, "newName.txt");
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("newName.txt", result.Value!.Name);
            Assert.IsFalse(File.Exists(filePath));
            Assert.IsTrue(File.Exists(Path.Combine(rootPath, "newName.txt")));
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
        }
    }

    [TestMethod]
    public async Task DeleteAsync_ValidFile_DeletesFile()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "AuthorizedRootDelete");
        Directory.CreateDirectory(rootPath);
        var filePath = Path.Combine(rootPath, "toDelete.txt");
        File.WriteAllText(filePath, "test");
        
        var options = new ExplorerOptions { AuthorizedRoots = new[] { rootPath } };
        var optionsMock = new Mock<IOptions<ExplorerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);
        var service = new FileSystemProvider(optionsMock.Object, _loggerMock!.Object);
        
        try
        {
            var result = await service.DeleteAsync(filePath);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
        }
    }
}

