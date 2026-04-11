using System.IO.Compression;

namespace WebFileExplorer.Server.Services
{
    public interface IArchiveService
    {
        Task CreateZipAsync(IEnumerable<string> sourcePaths, string destinationZipPath, CancellationToken cancellationToken = default);
        Task ExtractZipAsync(string zipPath, string destinationFolderPath, WebFileExplorer.Shared.Models.ConflictResolution resolution, CancellationToken cancellationToken = default);
    }

    public class ArchiveService : IArchiveService
    {
        public async Task CreateZipAsync(IEnumerable<string> sourcePaths, string destinationZipPath, CancellationToken cancellationToken = default)
        {
            if (File.Exists(destinationZipPath))
                throw new IOException($"Target file '{destinationZipPath}' already exists.");

            using var fs = new FileStream(destinationZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Create);
            foreach (var path in sourcePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(path))
                {
                    await AddFileToArchiveAsync(archive, path, Path.GetFileName(path), cancellationToken);
                }
                else if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    await AddDirectoryToArchiveAsync(archive, dirInfo, dirInfo.Name, cancellationToken);
                }
            }
        }

        private async Task AddFileToArchiveAsync(ZipArchive archive, string filePath, string entryName, CancellationToken cancellationToken)
        {
            var entry = archive.CreateEntry(entryName);
            using var entryStream = entry.Open();
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            await fileStream.CopyToAsync(entryStream, cancellationToken);
        }

        private async Task AddDirectoryToArchiveAsync(ZipArchive archive, DirectoryInfo dirInfo, string entryNamePrefix, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!dirInfo.EnumerateFileSystemInfos().Any())
            {
                archive.CreateEntry($"{entryNamePrefix}/");
                return;
            }

            foreach (var file in dirInfo.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AddFileToArchiveAsync(archive, file.FullName, $"{entryNamePrefix}/{file.Name}", cancellationToken);
            }

            foreach (var subDir in dirInfo.EnumerateDirectories())
            {
                await AddDirectoryToArchiveAsync(archive, subDir, $"{entryNamePrefix}/{subDir.Name}", cancellationToken);
            }
        }

        public async Task ExtractZipAsync(string zipPath, string destinationFolderPath, WebFileExplorer.Shared.Models.ConflictResolution resolution, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"ZIP file '{zipPath}' not found.");

            if (!Directory.Exists(destinationFolderPath))
                Directory.CreateDirectory(destinationFolderPath);

            var destinationFolderWithSlash = Path.GetFullPath(destinationFolderPath);
            if (!destinationFolderWithSlash.EndsWith(Path.DirectorySeparatorChar.ToString()))
                destinationFolderWithSlash += Path.DirectorySeparatorChar;

            using (var fsCheck = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            using (var archiveCheck = new ZipArchive(fsCheck, ZipArchiveMode.Read))
            {
                if (resolution == WebFileExplorer.Shared.Models.ConflictResolution.None)
                {
                    bool conflictFound = false;
                    foreach (var entry in archiveCheck.Entries)
                    {
                        var destinationPath = Path.GetFullPath(Path.Combine(destinationFolderPath, entry.FullName));
                        if (!string.IsNullOrEmpty(entry.Name) && File.Exists(destinationPath))
                        {
                            conflictFound = true;
                            break;
                        }
                    }
                    if (conflictFound)
                    {
                        throw new IOException("One or more files already exist in the destination.");
                    }
                }
            }

            using var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var destinationPath = Path.GetFullPath(Path.Combine(destinationFolderPath, entry.FullName));
                    if (!destinationPath.StartsWith(destinationFolderWithSlash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException($"Extraction path is outside the destination folder.");
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        
                        bool fileExists = File.Exists(destinationPath);

                        if (fileExists && resolution == WebFileExplorer.Shared.Models.ConflictResolution.Skip)
                        {
                            continue;
                        }

                        // If not skipping and exists, we overwrite
                        if (fileExists && resolution != WebFileExplorer.Shared.Models.ConflictResolution.Overwrite && resolution != WebFileExplorer.Shared.Models.ConflictResolution.Merge)
                        {
                            // Should have been caught by the None check, but fallback to prevent accidental overwrite
                            throw new IOException("Destination item already exists.");
                        }

                        try
                        {
                            var fileMode = fileExists ? FileMode.Create : FileMode.CreateNew;
                            using var entryStream = entry.Open();
                            using var fileStream = new FileStream(destinationPath, fileMode, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                            await entryStream.CopyToAsync(fileStream, cancellationToken);
                        }
                        catch (IOException)
                        {
                            if (resolution == WebFileExplorer.Shared.Models.ConflictResolution.Skip && File.Exists(destinationPath))
                            {
                                continue; // Skip on conflict race condition
                            }
                            throw;
                        }
                    }
                }
                catch (PathTooLongException ex)
                {
                    throw new IOException($"Path is too long to extract: {entry.FullName}", ex);
                }
            }
        }
    }
}
