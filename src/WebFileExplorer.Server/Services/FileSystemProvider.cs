using Microsoft.Extensions.Options;
using WebFileExplorer.Server.Configuration;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Server.Services;

public class FileSystemProvider : IFileSystemProvider
{
    private const int ERROR_SHARING_VIOLATION = 32;
    private const int ERROR_LOCK_VIOLATION = 33;

    private readonly ExplorerOptions _options;
    private readonly ILogger<FileSystemProvider> _logger;

    public FileSystemProvider(IOptions<ExplorerOptions> options, ILogger<FileSystemProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private static string EnsureTrailingSlash(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Ensures that the specified path is within one of the authorized root directories.
    /// Provides access control to prevent returning sensitive system files.
    /// </summary>
    /// <param name="path">The file or folder path to validate.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the path is not within any configured authorized root.</exception>
    public Task<bool> IsAuthorizedPathAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureAuthorizedPath(path);
            return Task.FromResult(true);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    private void EnsureAuthorizedPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        
        if (_options.AuthorizedRoots == null || _options.AuthorizedRoots.Length == 0)
        {
            throw new UnauthorizedAccessException("No authorized roots are configured.");
        }

        bool authorized = false;
        var pathWithSlash = EnsureTrailingSlash(fullPath);
        
        foreach (var root in _options.AuthorizedRoots)
        {
            var fullRoot = Path.GetFullPath(root);
            
            // Ensure trailing slash for exact prefix matching
            var rootWithSlash = EnsureTrailingSlash(fullRoot);
            
            if (pathWithSlash.StartsWith(rootWithSlash, StringComparison.OrdinalIgnoreCase))
            {
                authorized = true;
                break;
            }
        }

        if (!authorized)
        {
            _logger.LogWarning("Access denied to unauthorized path: {Path}", fullPath);
            throw new UnauthorizedAccessException($"Access to the path '{fullPath}' is denied.");
        }
    }

    /// <summary>
    /// Retrieves a collection of authorized root directories configured for the application.
    /// Offloads synchronous filesystem operations to a background thread.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>An enumerable collection of <see cref="DriveItem"/> representing the accessible roots.</returns>
    public Task<IEnumerable<DriveItem>> GetAuthorizedRootsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IEnumerable<DriveItem>>(() =>
        {
            var rootsLength = _options.AuthorizedRoots?.Length ?? 0;
            
            if (_options.AuthorizedRoots == null)
            {
                return Enumerable.Empty<DriveItem>();
            }

            var result = new List<DriveItem>(rootsLength);
            var drives = DriveInfo.GetDrives();
            
            foreach (var root in _options.AuthorizedRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var fullRoot = Path.GetFullPath(root);
                    
                    if (Directory.Exists(fullRoot))
                    {
                        DriveInfo? match = drives
                            .FirstOrDefault(d => fullRoot.StartsWith(d.Name, StringComparison.OrdinalIgnoreCase));
                            
                        if (match != null && match.IsReady)
                        {
                            var name = fullRoot == match.Name ? match.Name : new DirectoryInfo(fullRoot).Name;
                            result.Add(new DriveItem(name, fullRoot, match.AvailableFreeSpace, match.TotalSize));
                        }
                        else
                        {
                            var dirInfo = new DirectoryInfo(fullRoot);
                            result.Add(new DriveItem(dirInfo.Name, fullRoot, 0, 0));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get drive info for root: {Root}", root);
                }
            }

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// Lists all files and subdirectories within the specified authoritative path.
    /// Safely skips inaccessible items and ensures the target directory falls under authorized roots.
    /// </summary>
    /// <param name="path">The target directory path to enumerate.</param>
    /// <param name="showHidden">Whether to include hidden items.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the work.</param>
    /// <returns>An enumerable collection of <see cref="FileSystemItem"/> representing files and folders.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the requested path is not within authorized roots or access is otherwise denied.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if the path does not exist on the filesystem.</exception>
    public Task<WebFileExplorer.Shared.Models.PagedResult<FileSystemItem>> ListDirectoriesAsync(string path, bool showHidden = false, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        EnsureAuthorizedPath(fullPath);

        return Task.Run<WebFileExplorer.Shared.Models.PagedResult<FileSystemItem>>(() =>
        {
            var directoryInfo = new DirectoryInfo(fullPath);
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory '{fullPath}' not found.");
            }

            try
            {
                var options = new EnumerationOptions { IgnoreInaccessible = true };
                
                var dirs = directoryInfo.EnumerateDirectories("*", options)
                    .Where(dir => showHidden || (dir.Attributes & FileAttributes.Hidden) == 0)
                    .Select(dir => 
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return new FileSystemItem(
                            dir.Name,
                            dir.FullName,
                            FileSystemItemType.Folder,
                            0,
                            dir.LastWriteTimeUtc,
                            (dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
                        );
                    });

                var files = directoryInfo.EnumerateFiles("*", options)
                    .Where(file => showHidden || (file.Attributes & FileAttributes.Hidden) == 0)
                    .Select(file => 
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var isArchive = file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
                        return new FileSystemItem(
                            file.Name,
                            file.FullName,
                            isArchive ? FileSystemItemType.Archive : FileSystemItemType.File,
                            file.Length,
                            file.LastWriteTimeUtc,
                            (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
                        );
                    });

                
var sorted = System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderBy(dirs.Concat(files), i => i.Type != FileSystemItemType.Folder).ThenBy(i => i.Name));
int total = sorted.Count;
var paged = System.Linq.Enumerable.Skip(sorted, skip ?? 0);
if (take.HasValue) paged = System.Linq.Enumerable.Take(paged, take.Value);
return new WebFileExplorer.Shared.Models.PagedResult<FileSystemItem> { Items = System.Linq.Enumerable.ToList(paged), TotalCount = total };

            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied when enumerating directory: {Path}", fullPath);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<Result<FileSystemItem>> CreateFolderAsync(string parentPath, string folderName, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullParentPath = Path.GetFullPath(parentPath);
            EnsureAuthorizedPath(fullParentPath);

            var newFolderPath = Path.Combine(fullParentPath, folderName);
            var fullNewPath = Path.GetFullPath(newFolderPath);

            // Ensure the new folder path is also within an authorized root (prevent directory traversal)
            EnsureAuthorizedPath(fullNewPath);

            // Await Task.Run for actual IO
            var dirInfo = await Task.Run(() => 
            {
                if (Directory.Exists(fullNewPath) || File.Exists(fullNewPath))
                {
                    return null;
                }
                return Directory.CreateDirectory(fullNewPath);
            }, cancellationToken);

            if (dirInfo == null)
            {
                return Result<FileSystemItem>.Failure($"An item with the name '{folderName}' already exists.");
            }

            var item = new FileSystemItem(
                dirInfo.Name,
                dirInfo.FullName,
                FileSystemItemType.Folder,
                0,
                dirInfo.LastWriteTimeUtc,
                (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
            );
            return Result<FileSystemItem>.Success(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied creating folder {FolderName} in {ParentPath}.", folderName, parentPath);
            return Result<FileSystemItem>.Failure("Access denied. You do not have permission to create this folder.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid name supplied creating folder {FolderName}.", folderName);
            return Result<FileSystemItem>.Failure("The provided folder name is invalid.");
        }
        catch (PathTooLongException ex)
        {
            _logger.LogWarning(ex, "Path too long creating folder {FolderName} in {ParentPath}.", folderName, parentPath);
            return Result<FileSystemItem>.Failure("The folder name or path is too long.");
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == ERROR_SHARING_VIOLATION || (ex.HResult & 0xFFFF) == ERROR_LOCK_VIOLATION)
        {
            _logger.LogWarning(ex, "File lock error creating folder {FolderName} in {ParentPath}.", folderName, parentPath);
            return Result<FileSystemItem>.Failure("The location is currently locked or in use by another process.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error creating folder {FolderName} in {ParentPath}.", folderName, parentPath);
            return Result<FileSystemItem>.Failure("An I/O error occurred while creating the folder.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder {FolderName} in {ParentPath}.", folderName, parentPath);
            return Result<FileSystemItem>.Failure("An unexpected error occurred while creating the folder.");
        }
    }

    public async Task<Result<FileSystemItem>> RenameAsync(string path, string newName, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            EnsureAuthorizedPath(fullPath);
            
            var parentDir = Path.GetDirectoryName(fullPath);
            if (parentDir == null)
            {
                return Result<FileSystemItem>.Failure("Cannot rename root directory.");
            }

            var newPath = Path.Combine(parentDir, newName);
            var fullNewPath = Path.GetFullPath(newPath);
            EnsureAuthorizedPath(fullNewPath);

            return await Task.Run<Result<FileSystemItem>>(() =>
            {
                if (Directory.Exists(fullNewPath) || File.Exists(fullNewPath))
                {
                    return Result<FileSystemItem>.Failure($"An item with the name '{newName}' already exists.");
                }

                if (Directory.Exists(fullPath))
                {
                    Directory.Move(fullPath, fullNewPath);
                    var dirInfo = new DirectoryInfo(fullNewPath);
                    var item = new FileSystemItem(
                        dirInfo.Name,
                        dirInfo.FullName,
                        FileSystemItemType.Folder,
                        0,
                        dirInfo.LastWriteTimeUtc,
                        (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
                    );
                    return Result<FileSystemItem>.Success(item);
                }
                else if (File.Exists(fullPath))
                {
                    File.Move(fullPath, fullNewPath);
                    var fileInfo = new FileInfo(fullNewPath);
                    var isArchive = fileInfo.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
                    var item = new FileSystemItem(
                        fileInfo.Name,
                        fileInfo.FullName,
                        isArchive ? FileSystemItemType.Archive : FileSystemItemType.File,
                        fileInfo.Length,
                        fileInfo.LastWriteTimeUtc,
                        (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
                    );
                    return Result<FileSystemItem>.Success(item);
                }
                
                return Result<FileSystemItem>.Failure("Source item does not exist.");
            }, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied renaming {Path} to {NewName}.", path, newName);
            return Result<FileSystemItem>.Failure("Access denied. You do not have permission to rename this item.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid name renaming {Path} to {NewName}.", path, newName);
            return Result<FileSystemItem>.Failure("The provided name or path is invalid.");
        }
        catch (PathTooLongException ex)
        {
            _logger.LogWarning(ex, "Path too long renaming {Path} to {NewName}.", path, newName);
            return Result<FileSystemItem>.Failure("The name or path is too long.");
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == ERROR_SHARING_VIOLATION || (ex.HResult & 0xFFFF) == ERROR_LOCK_VIOLATION)
        {
            _logger.LogWarning(ex, "File lock error renaming {Path} to {NewName}.", path, newName);
            return Result<FileSystemItem>.Failure("The item is currently locked or in use by another process.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error renaming {Path} to {NewName}.", path, newName);
            return Result<FileSystemItem>.Failure("An I/O error occurred while renaming the item.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename {Path} to {NewName}.", path, newName);
            return Result<FileSystemItem>.Failure("An unexpected error occurred while renaming the item.");
        }
    }

    public async Task<Result> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            EnsureAuthorizedPath(fullPath);

            return await Task.Run<Result>(() =>
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, recursive: true);
                    return Result.Success();
                }
                else if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return Result.Success();
                }

                return Result.Failure("Item does not exist.");
            }, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied deleting {Path}.", path);
            return Result.Failure("Access denied. You do not have permission to delete this item.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid path deleting {Path}.", path);
            return Result.Failure("The provided path is invalid.");
        }
        catch (PathTooLongException ex)
        {
            _logger.LogWarning(ex, "Path too long deleting {Path}.", path);
            return Result.Failure("The path is too long.");
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == ERROR_SHARING_VIOLATION || (ex.HResult & 0xFFFF) == ERROR_LOCK_VIOLATION)
        {
            _logger.LogWarning(ex, "File lock error deleting {Path}.", path);
            return Result.Failure("The item is currently locked or in use by another process.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error deleting {Path}.", path);
            return Result.Failure("An I/O error occurred while deleting the item.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {Path}.", path);
            return Result.Failure("An unexpected error occurred while deleting the item.");
        }
    }

    public async Task<Result> CopyAsync(IEnumerable<string> items, string destinationPath, ConflictResolution resolution = ConflictResolution.None, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var fullDestPath = Path.GetFullPath(destinationPath);
            EnsureAuthorizedPath(fullDestPath);
            var sources = items.ToList();

            var validationResult = await Task.Run<Result>(() =>
            {
                if (!Directory.Exists(fullDestPath)) return Result.Failure("Destination directory does not exist.");

                foreach (var path in sources)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var fullSrcPath = Path.GetFullPath(path);
                    EnsureAuthorizedPath(fullSrcPath);

                    if (Directory.Exists(fullSrcPath))
                    {
                        var pathWithSlash = EnsureTrailingSlash(fullSrcPath);
                        var destWithSlash = EnsureTrailingSlash(fullDestPath);
                        if (destWithSlash.StartsWith(pathWithSlash, StringComparison.OrdinalIgnoreCase))
                        {
                            return Result.Failure($"Cannot copy a folder into itself or its descendant: {fullSrcPath}");
                        }
                    }

                    var itemName = Path.GetFileName(fullSrcPath.TrimEnd(Path.DirectorySeparatorChar));
                    var destItemPath = Path.Combine(fullDestPath, itemName);

                    bool exists = File.Exists(destItemPath) || Directory.Exists(destItemPath);
                    if (exists)
                    {
                        if (resolution == ConflictResolution.None)
                        {
                            return Result.Failure("Destination item already exists.");
                        }
                    }
                }
                return Result.Success();
            }, cancellationToken);

            if (!validationResult.IsSuccess) return validationResult;

            foreach (var path in sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullSrcPath = Path.GetFullPath(path);
                var itemName = Path.GetFileName(fullSrcPath.TrimEnd(Path.DirectorySeparatorChar));
                var destItemPath = Path.Combine(fullDestPath, itemName);
                
                bool exists = File.Exists(destItemPath) || Directory.Exists(destItemPath);
                if (exists && resolution == ConflictResolution.Skip)
                {
                    continue;
                }

                if (File.Exists(fullSrcPath))
                {
                    await CopyFileAsync(fullSrcPath, destItemPath, resolution, cancellationToken);
                }
                else if (Directory.Exists(fullSrcPath))
                {
                    if (exists && resolution == ConflictResolution.Overwrite)
                    {
                        Directory.Delete(destItemPath, true);
                    }
                    await CopyDirectoryRecurseAsync(new DirectoryInfo(fullSrcPath), new DirectoryInfo(destItemPath), resolution, cancellationToken);
                }
            }
            return Result.Success();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied copying to {Dest}.", destinationPath);
            return Result.Failure("Access denied.");
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == ERROR_SHARING_VIOLATION || (ex.HResult & 0xFFFF) == ERROR_LOCK_VIOLATION)
        {
            _logger.LogWarning(ex, "File lock error copying to {Dest}.", destinationPath);
            return Result.Failure("File lock error occurred.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy items to {Dest}.", destinationPath);
            return Result.Failure($"Failed to copy items. {ex.Message}");
        }
    }

    private async Task CopyFileAsync(string sourceFile, string destFile, ConflictResolution resolution, System.Threading.CancellationToken cancellationToken)
    {
        if (File.Exists(destFile) && resolution == ConflictResolution.Skip) return;
        
        var fileMode = (File.Exists(destFile) && (resolution == ConflictResolution.Overwrite || resolution == ConflictResolution.Merge)) ? FileMode.Create : FileMode.CreateNew;
        using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var destStream = new FileStream(destFile, fileMode, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await sourceStream.CopyToAsync(destStream, cancellationToken);
    }

    private async Task CopyDirectoryRecurseAsync(DirectoryInfo source, DirectoryInfo target, ConflictResolution resolution, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(target.FullName);

        foreach (var file in source.GetFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await CopyFileAsync(file.FullName, Path.Combine(target.FullName, file.Name), resolution, cancellationToken);
        }

        foreach (var subDir in source.GetDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetSubDir = new DirectoryInfo(Path.Combine(target.FullName, subDir.Name));
            if (targetSubDir.Exists && resolution == ConflictResolution.Skip) continue;
            
            if (targetSubDir.Exists && resolution == ConflictResolution.Overwrite)
            {
                targetSubDir.Delete(true);
            }
            
            await CopyDirectoryRecurseAsync(subDir, targetSubDir, resolution, cancellationToken);
        }
    }

    public async Task<Result> MoveAsync(IEnumerable<string> items, string destinationPath, ConflictResolution resolution = ConflictResolution.None, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var fullDestPath = Path.GetFullPath(destinationPath);
            EnsureAuthorizedPath(fullDestPath);
            var sources = items.ToList();

            var validationResult = await Task.Run<Result>(() =>
            {
                if (!Directory.Exists(fullDestPath)) return Result.Failure("Destination directory does not exist.");

                foreach (var path in sources)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var fullSrcPath = Path.GetFullPath(path);
                    EnsureAuthorizedPath(fullSrcPath);

                    if (Directory.Exists(fullSrcPath))
                    {
                        var pathWithSlash = EnsureTrailingSlash(fullSrcPath);
                        var destWithSlash = EnsureTrailingSlash(fullDestPath);
                        if (destWithSlash.StartsWith(pathWithSlash, StringComparison.OrdinalIgnoreCase))
                        {
                            return Result.Failure($"Cannot copy a folder into itself or its descendant: {fullSrcPath}");
                        }
                    }

                    var itemName = Path.GetFileName(fullSrcPath.TrimEnd(Path.DirectorySeparatorChar));
                    var destItemPath = Path.Combine(fullDestPath, itemName);

                    bool exists = File.Exists(destItemPath) || Directory.Exists(destItemPath);
                    if (exists)
                    {
                        if (resolution == ConflictResolution.None)
                        {
                            return Result.Failure("Destination item already exists.");
                        }
                    }
                }
                return Result.Success();
            }, cancellationToken);

            if (!validationResult.IsSuccess) return validationResult;

            foreach (var path in sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullSrcPath = Path.GetFullPath(path);
                var itemName = Path.GetFileName(fullSrcPath.TrimEnd(Path.DirectorySeparatorChar));
                var destItemPath = Path.Combine(fullDestPath, itemName);

                bool exists = File.Exists(destItemPath) || Directory.Exists(destItemPath);
                if (exists && resolution == ConflictResolution.Skip)
                {
                    continue;
                }

                if (File.Exists(fullSrcPath))
                {
                    if (exists && (resolution == ConflictResolution.Overwrite || resolution == ConflictResolution.Merge))
                    {
                        File.Delete(destItemPath);
                    }
                    File.Move(fullSrcPath, destItemPath);
                }
                else if (Directory.Exists(fullSrcPath))
                {
                    bool sameDrive = Path.GetPathRoot(fullSrcPath)?.Equals(Path.GetPathRoot(destItemPath), StringComparison.OrdinalIgnoreCase) == true;
                    
                    if (exists && resolution == ConflictResolution.Overwrite)
                    {
                        Directory.Delete(destItemPath, true);
                        if (sameDrive) {
                            Directory.Move(fullSrcPath, destItemPath);
                        } else {
                            await CopyDirectoryRecurseAsync(new DirectoryInfo(fullSrcPath), new DirectoryInfo(destItemPath), resolution, cancellationToken);
                            Directory.Delete(fullSrcPath, true);
                        }
                    }
                    else if (exists && resolution == ConflictResolution.Merge)
                    {
                        await CopyDirectoryRecurseAsync(new DirectoryInfo(fullSrcPath), new DirectoryInfo(destItemPath), resolution, cancellationToken);
                        Directory.Delete(fullSrcPath, true);
                    }
                    else
                    {
                        if (sameDrive)
                        {
                            Directory.Move(fullSrcPath, destItemPath);
                        }
                        else
                        {
                            await CopyDirectoryRecurseAsync(new DirectoryInfo(fullSrcPath), new DirectoryInfo(destItemPath), resolution, cancellationToken);
                            Directory.Delete(fullSrcPath, true);
                        }
                    }
                }
            }
            return Result.Success();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied moving to {Dest}.", destinationPath);
            return Result.Failure("Access denied.");
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == ERROR_SHARING_VIOLATION || (ex.HResult & 0xFFFF) == ERROR_LOCK_VIOLATION)
        {
            _logger.LogWarning(ex, "File lock error moving to {Dest}.", destinationPath);
            return Result.Failure("File lock error occurred.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move items to {Dest}.", destinationPath);
            return Result.Failure($"Failed to move items. {ex.Message}");
        }
    }

    public Task<Result<Stream>> GetFileStreamAsync(string path, System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            EnsureAuthorizedPath(fullPath);

            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
            {
                return Task.FromResult(Result<Stream>.Failure("File not found."));
            }

            // Open stream with FileShare.ReadWrite to allow reading even if another process is writing
            Stream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
            return Task.FromResult(Result<Stream>.Success(fs));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied reading file {Path}.", path);
            return Task.FromResult(Result<Stream>.Failure("Access denied to the requested file."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {Path}.", path);
            return Task.FromResult(Result<Stream>.Failure("An error occurred while accessing the file."));
        }
    }

    public Task<WebFileExplorer.Shared.Models.PagedResult<FileSystemItem>> SearchAsync(string path, string query, bool showHidden = false, bool recurse = true, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        EnsureAuthorizedPath(fullPath);

        return Task.Run<WebFileExplorer.Shared.Models.PagedResult<FileSystemItem>>(() =>
        {
            var directoryInfo = new DirectoryInfo(fullPath);
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory '{fullPath}' not found.");
            }

            try
            {
                var options = new EnumerationOptions 
                { 
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = recurse,
                    MatchType = MatchType.Simple,
                    MatchCasing = MatchCasing.CaseInsensitive
                };
                
                string searchPattern = $"*{query}*";

                var dirs = directoryInfo.EnumerateDirectories(searchPattern, options)
                    .Where(dir => showHidden || (dir.Attributes & FileAttributes.Hidden) == 0)
                    .Select(dir => 
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return new FileSystemItem(
                            dir.Name,
                            dir.FullName,
                            FileSystemItemType.Folder,
                            0,
                            dir.LastWriteTimeUtc,
                            (dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
                        );
                    });

                var files = directoryInfo.EnumerateFiles(searchPattern, options)
                    .Where(file => showHidden || (file.Attributes & FileAttributes.Hidden) == 0)
                    .Select(file => 
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var isArchive = file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
                        return new FileSystemItem(
                            file.Name,
                            file.FullName,
                            isArchive ? FileSystemItemType.Archive : FileSystemItemType.File,
                            file.Length,
                            file.LastWriteTimeUtc,
                            (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
                        );
                    });

                
var sorted = System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderBy(dirs.Concat(files), i => i.Type != FileSystemItemType.Folder).ThenBy(i => i.Name));
int total = sorted.Count;
var paged = System.Linq.Enumerable.Skip(sorted, skip ?? 0);
if (take.HasValue) paged = System.Linq.Enumerable.Take(paged, take.Value);
return new WebFileExplorer.Shared.Models.PagedResult<FileSystemItem> { Items = System.Linq.Enumerable.ToList(paged), TotalCount = total };

            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied when searching directory: {Path}", fullPath);
                throw;
            }
        }, cancellationToken);
    }

    public Task<FileProperties> GetPropertiesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        var pathsList = paths?.ToList();
        if (pathsList == null || pathsList.Count == 0)
        {
            throw new ArgumentException("At least one path must be provided.", nameof(paths));
        }

        var fullPaths = pathsList.Select(Path.GetFullPath).ToList();
        foreach (var fp in fullPaths) EnsureAuthorizedPath(fp);

        return Task.Run(() =>
        {
            long size = 0;
            int fileCount = 0;
            int folderCount = 0;
            
            bool? isHidden = null;
            bool? isReadOnly = null;
            bool? isSystem = null;
            bool? isArchiveProp = null;
            
            DateTimeOffset? created = null;
            DateTimeOffset? modified = null;
            DateTimeOffset? accessed = null;

            bool first = true;

            foreach (var fp in fullPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(fp))
                {
                    var fileInfo = new FileInfo(fp);
                    size += fileInfo.Length;
                    fileCount++;

                    var attrs = fileInfo.Attributes;
                    bool h = (attrs & FileAttributes.Hidden) == FileAttributes.Hidden;
                    bool r = (attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                    bool s = (attrs & FileAttributes.System) == FileAttributes.System;
                    bool a = (attrs & FileAttributes.Archive) == FileAttributes.Archive;

                    if (first)
                    {
                        isHidden = h; isReadOnly = r; isSystem = s; isArchiveProp = a;
                        created = fileInfo.CreationTimeUtc; modified = fileInfo.LastWriteTimeUtc; accessed = fileInfo.LastAccessTimeUtc;
                    }
                    else
                    {
                        if (isHidden != h) isHidden = null;
                        if (isReadOnly != r) isReadOnly = null;
                        if (isSystem != s) isSystem = null;
                        if (isArchiveProp != a) isArchiveProp = null;
                        created = null; modified = null; accessed = null;
                    }
                }
                else if (Directory.Exists(fp))
                {
                    var dirInfo = new DirectoryInfo(fp);
                    folderCount++;

                    try
                    {
                        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
                        var files = dirInfo.EnumerateFiles("*", options).ToList();
                        size += files.Sum(f => f.Length);
                        fileCount += files.Count;
                        folderCount += dirInfo.EnumerateDirectories("*", options).Count();
                    }
                    catch { }

                    var attrs = dirInfo.Attributes;
                    bool h = (attrs & FileAttributes.Hidden) == FileAttributes.Hidden;
                    bool r = (attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                    bool s = (attrs & FileAttributes.System) == FileAttributes.System;
                    bool a = (attrs & FileAttributes.Archive) == FileAttributes.Archive;

                    if (first)
                    {
                        isHidden = h; isReadOnly = r; isSystem = s; isArchiveProp = a;
                        created = dirInfo.CreationTimeUtc; modified = dirInfo.LastWriteTimeUtc; accessed = dirInfo.LastAccessTimeUtc;
                    }
                    else
                    {
                        if (isHidden != h) isHidden = null;
                        if (isReadOnly != r) isReadOnly = null;
                        if (isSystem != s) isSystem = null;
                        if (isArchiveProp != a) isArchiveProp = null;
                        created = null; modified = null; accessed = null;
                    }
                }
                first = false;
            }

            string name;
            string location;
            string type;

            if (fullPaths.Count == 1)
            {
                var fp = fullPaths[0];
                name = Path.GetFileName(fp);
                if (string.IsNullOrEmpty(name)) name = fp; // drive root
                location = Path.GetDirectoryName(fp) ?? fp;
                
                if (File.Exists(fp))
                {
                    var ext = Path.GetExtension(fp);
                    type = ext.Equals(".zip", StringComparison.OrdinalIgnoreCase) ? "Compressed (zipped) Folder" : $"{ext.ToUpperInvariant()} File";
                    if (string.IsNullOrEmpty(ext)) type = "File";
                }
                else
                {
                    type = "File Folder";
                }
            }
            else
            {
                name = $"{fullPaths.Count} Items Selected";
                location = Path.GetDirectoryName(fullPaths[0]) ?? "";
                type = "Multiple Types";
            }

            return new FileProperties(
                name,
                location,
                type,
                size,
                fileCount,
                folderCount,
                created,
                modified,
                accessed,
                isHidden,
                isReadOnly,
                isSystem,
                isArchiveProp
            );

        }, cancellationToken);
    }
}



