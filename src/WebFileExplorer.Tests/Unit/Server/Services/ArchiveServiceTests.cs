using System.IO.Compression;
using WebFileExplorer.Server.Services;

namespace WebFileExplorer.Tests.Unit.Server.Services;

[TestClass]
public class ArchiveServiceTests
{
    private string _tempDir = null!;
    private ArchiveService _archiveService = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _archiveService = new ArchiveService();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [TestMethod]
    public async Task CreateZipAsync_ValidFiles_CreatesZipSuccessfully()
    {
        var file1 = Path.Combine(_tempDir, "file1.txt");
        var file2 = Path.Combine(_tempDir, "file2.txt");
        await File.WriteAllTextAsync(file1, "Content1");
        await File.WriteAllTextAsync(file2, "Content2");

        var destinationZip = Path.Combine(_tempDir, "output.zip");

        await _archiveService.CreateZipAsync(new[] { file1, file2 }, destinationZip);

        Assert.IsTrue(File.Exists(destinationZip));
        using var archive = ZipFile.OpenRead(destinationZip);
        // Using assert collection
        Assert.HasCount(2, archive.Entries);
        Assert.IsTrue(archive.Entries.Any(e => e.Name == "file1.txt"));
    }

    [TestMethod]
    public async Task ExtractZipAsync_ValidZip_ExtractsSuccessfully()
    {
        var zipPath = Path.Combine(_tempDir, "test.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("extracted.txt");
            using var sw = new StreamWriter(entry.Open());
            await sw.WriteAsync("ExtractedContent");
        }

        var extractDir = Path.Combine(_tempDir, "ExtractDest");

        await _archiveService.ExtractZipAsync(zipPath, extractDir, WebFileExplorer.Shared.Models.ConflictResolution.None);

        var extractedFile = Path.Combine(extractDir, "extracted.txt");
        Assert.IsTrue(File.Exists(extractedFile));
        var content = await File.ReadAllTextAsync(extractedFile);
        Assert.AreEqual("ExtractedContent", content);
    }

    [TestMethod]
    public async Task ExtractZipAsync_WithOverwriteTrue_OverwritesExistingFile()
    {
        var extractDir = Path.Combine(_tempDir, "ExtractDest");
        Directory.CreateDirectory(extractDir);
        var extractFile = Path.Combine(extractDir, "conflict.txt");
        await File.WriteAllTextAsync(extractFile, "OldContent");

        var zipPath = Path.Combine(_tempDir, "test.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("conflict.txt");
            using var sw = new StreamWriter(entry.Open());
            await sw.WriteAsync("NewContent");
        }

        await _archiveService.ExtractZipAsync(zipPath, extractDir, WebFileExplorer.Shared.Models.ConflictResolution.Overwrite);

        var content = await File.ReadAllTextAsync(extractFile);
        Assert.AreEqual("NewContent", content);
    }

    [TestMethod]
    public async Task ExtractZipAsync_WithSkip_DoesNotOverwrite()
    {
        var extractDir = Path.Combine(_tempDir, "ExtractDest");
        Directory.CreateDirectory(extractDir);
        var extractFile = Path.Combine(extractDir, "conflict.txt");
        await File.WriteAllTextAsync(extractFile, "OldContent");

        var zipPath = Path.Combine(_tempDir, "test.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("conflict.txt");
            using var sw = new StreamWriter(entry.Open());
            await sw.WriteAsync("NewContent");
        }

        await _archiveService.ExtractZipAsync(zipPath, extractDir, WebFileExplorer.Shared.Models.ConflictResolution.Skip);

        var content = await File.ReadAllTextAsync(extractFile);
        Assert.AreEqual("OldContent", content);
    }

    [TestMethod]
    public async Task ExtractZipAsync_WithNone_ThrowsIOExceptionOnConflict()
    {
        var extractDir = Path.Combine(_tempDir, "ExtractDest");
        Directory.CreateDirectory(extractDir);
        var extractFile = Path.Combine(extractDir, "conflict.txt");
        await File.WriteAllTextAsync(extractFile, "OldContent");

        var zipPath = Path.Combine(_tempDir, "test.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("conflict.txt");
            using var sw = new StreamWriter(entry.Open());
            await sw.WriteAsync("NewContent");
        }

        bool exceptionThrown = false;
        try
        {
            await _archiveService.ExtractZipAsync(zipPath, extractDir, WebFileExplorer.Shared.Models.ConflictResolution.None);
        }
        catch (IOException ex) when (ex.Message.Contains("already exist"))
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown, "Expected IOException on conflict to be thrown.");
    }

    [TestMethod]
    public async Task ExtractZipAsync_InvalidZipFile_ThrowsInvalidDataException()
    {
        var invalidZipPath = Path.Combine(_tempDir, "invalid.zip");
        await File.WriteAllTextAsync(invalidZipPath, "This is not a valid zip file content");

        var extractDir = Path.Combine(_tempDir, "ExtractDestInvalid");
        bool exceptionThrown = false;
        try
        {
            await _archiveService.ExtractZipAsync(invalidZipPath, extractDir, WebFileExplorer.Shared.Models.ConflictResolution.None);
        }
        catch (InvalidDataException)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown, "Expected InvalidDataException to be thrown.");
    }
}
