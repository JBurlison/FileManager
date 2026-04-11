using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Server.Services;

/// <summary>
/// Defines the core capabilities for reading file system items and authorized roots.
/// </summary>
public interface IFileSystemProvider
{
    /// <summary>
    /// Gets the list of configured, authorized root directories or drives.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, returning an enumerable of available roots.</returns>
    Task<IEnumerable<DriveItem>> GetAuthorizedRootsAsync(System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of directories and files at the specified path, ensuring the path is within authorized roots.
    /// </summary>
    /// <param name="path">The full path to list directories and files for.</param>
    /// <param name="showHidden">Whether to include hidden items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, returning an enumerable of file system items.</returns>
    Task<PagedResult<FileSystemItem>> ListDirectoriesAsync(string path, bool showHidden = false, int? skip = null, int? take = null, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new directory at the specified path.
    /// </summary>
    Task<Result<FileSystemItem>> CreateFolderAsync(string parentPath, string folderName, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a file or directory.
    /// </summary>
    Task<Result<FileSystemItem>> RenameAsync(string path, string newName, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file or directory.
    /// </summary>
    Task<Result> DeleteAsync(string path, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies multiple items to a destination folder.
    /// </summary>
    Task<Result> CopyAsync(IEnumerable<string> items, string destinationPath, ConflictResolution resolution = ConflictResolution.None, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves multiple items to a destination folder.
    /// </summary>
    Task<Result> MoveAsync(IEnumerable<string> items, string destinationPath, ConflictResolution resolution = ConflictResolution.None, System.Threading.CancellationToken cancellationToken = default);

    Task<FileProperties> GetPropertiesAsync(IEnumerable<string> paths, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stream for reading a file.
    /// </summary>
    Task<Result<Stream>> GetFileStreamAsync(string path, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for items matching the specified query recursively starting from the given path.
    /// </summary>
    Task<PagedResult<FileSystemItem>> SearchAsync(string path, string query, bool showHidden = false, bool recurse = true, int? skip = null, int? take = null, System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a given path is authorized by the configured roots.
    /// </summary>
    Task<bool> IsAuthorizedPathAsync(string path, System.Threading.CancellationToken cancellationToken = default);
}
