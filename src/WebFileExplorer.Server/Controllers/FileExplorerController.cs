using Microsoft.AspNetCore.Mvc;
using WebFileExplorer.Server.Services;
using WebFileExplorer.Shared.Models;

namespace WebFileExplorer.Server.Controllers;

[ApiController]
[Route("api/fileexplorer")]
public class FileExplorerController : ControllerBase
{
    private readonly IFileSystemProvider _provider;
    private readonly IArchiveService _archiveService;
        private readonly IRecycleBinService _recycleBinService;
        private readonly ILogger<FileExplorerController> _logger;
        private static readonly Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider _contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

        public FileExplorerController(IFileSystemProvider provider, IArchiveService archiveService, IRecycleBinService recycleBinService, ILogger<FileExplorerController> logger)
        {
            _provider = provider;
            _archiveService = archiveService;
            _recycleBinService = recycleBinService;
            _logger = logger;
        }
    [HttpPost("properties")]
    public async Task<ActionResult<Result<FileProperties>>> GetAggregateProperties([FromBody] string[] paths, CancellationToken cancellationToken = default)
    {
        if (paths == null || paths.Length == 0)
        {
            return BadRequest(Result<FileProperties>.Failure("Paths are required."));
        }

        try
        {
            var item = await _provider.GetPropertiesAsync(paths, cancellationToken);
            if (item == null) return NotFound(Result<FileProperties>.Failure("File or folder not found."));
            return Ok(Result<FileProperties>.Success(item));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, Result<FileProperties>.Failure("Access denied."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aggregate properties for paths.");
            return StatusCode(500, Result<FileProperties>.Failure("An unexpected error occurred."));
        }
    }

    [HttpGet("properties")]
    public async Task<ActionResult<Result<FileProperties>>> GetProperties([FromQuery] string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest(Result<FileProperties>.Failure("Path is required."));
        }

        try
        {
            var item = await _provider.GetPropertiesAsync(new[] { path }, cancellationToken);
            if (item == null) return NotFound(Result<FileProperties>.Failure("File or folder not found."));
            return Ok(Result<FileProperties>.Success(item));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, Result<FileProperties>.Failure("Access denied."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties for {Path}", path);
            return StatusCode(500, Result<FileProperties>.Failure("An unexpected error occurred."));
        }
    }

    [HttpGet("roots")]
    public async Task<ActionResult<IEnumerable<DriveItem>>> GetAuthorizedRoots(CancellationToken cancellationToken)
    {
        var roots = await _provider.GetAuthorizedRootsAsync(cancellationToken);
        return Ok(roots);
    }

    [HttpGet("list")]
    public async Task<ActionResult<WebFileExplorer.Shared.Models.PagedResult<FileSystemItem>>> ListDirectories([FromQuery] string path, [FromQuery] bool showHidden = false, [FromQuery] int? skip = null, [FromQuery] int? take = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("Path is required.");
        }

        try
        {
            var items = await _provider.ListDirectoriesAsync(path, showHidden, skip, take, cancellationToken);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, "Access to the requested path is denied.");
        }
        catch (System.IO.DirectoryNotFoundException)
        {
            return NotFound("The specified directory does not exist.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directories for path: {Path}", path);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("create-folder")]
    public async Task<ActionResult<Result<FileSystemItem>>> CreateFolder([FromQuery] string parentPath, [FromQuery] string folderName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(folderName))
        {
            return BadRequest(Result<FileSystemItem>.Failure("Parent path and folder name are required."));
        }

        var result = await _provider.CreateFolderAsync(parentPath, folderName, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("rename")]
    public async Task<ActionResult<Result<FileSystemItem>>> Rename([FromQuery] string path, [FromQuery] string newName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(newName))
        {
            return BadRequest(Result<FileSystemItem>.Failure("Path and new name are required."));
        }

        var result = await _provider.RenameAsync(path, newName, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult<Result>> Delete([FromQuery] string path, [FromQuery] bool permanent = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest(Result.Failure("Path is required."));
        }

        if (permanent)
        {
            var result = await _provider.DeleteAsync(path, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        else
        {
            if (!_recycleBinService.IsSupported)
            {
                return StatusCode(501, Result.Failure("Recycle Bin is unsupported or unavailable on this system."));
            }

            var success = _recycleBinService.MoveToRecycleBin(path);
            if (!success)
            {
                return BadRequest(Result.Failure("Failed to move item to recycle bin."));
            }
            return Ok(Result.Success());
        }
    }

    [HttpGet("recyclebin/status")]
    public ActionResult<Result<bool>> GetRecycleBinStatus()
    {
        return Ok(Result<bool>.Success(_recycleBinService.IsSupported));
    }

    [HttpGet("recyclebin")]
    public ActionResult<IEnumerable<RecycleBinItem>> GetRecycleBin()
    {
        if (!_recycleBinService.IsSupported)
            return StatusCode(501, "Recycle Bin is unsupported or unavailable on this system.");

        var items = _recycleBinService.GetDeletedItems();
        return Ok(items);
    }

    [HttpPost("recyclebin/restore")]
    public ActionResult<Result> RestoreRecycleBinItem([FromQuery] string id)
    {
        if (!_recycleBinService.IsSupported)
            return StatusCode(501, Result.Failure("Recycle Bin is unsupported or unavailable on this system."));

        var success = _recycleBinService.RestoreItem(id);
        if (success) return Ok(Result.Success());
        return BadRequest(Result.Failure("Failed to restore item."));
    }

    [HttpPost("recyclebin/empty")]
    public ActionResult<Result> EmptyRecycleBin()
    {
        if (!_recycleBinService.IsSupported)
            return StatusCode(501, Result.Failure("Recycle Bin is unsupported or unavailable on this system."));

        var success = _recycleBinService.EmptyBin();
        if (success) return Ok(Result.Success());
        return BadRequest(Result.Failure("Failed to empty recycle bin."));
    }

    [HttpPost("recyclebin/delete")]
    public ActionResult<Result> DeleteRecycleBinItem([FromQuery] string id)
    {
        if (!_recycleBinService.IsSupported)
            return StatusCode(501, Result.Failure("Recycle Bin is unsupported or unavailable on this system."));

        var success = _recycleBinService.DeleteItem(id);
        if (success) return Ok(Result.Success());
        return BadRequest(Result.Failure("Failed to delete recycle bin item."));
    }

    [HttpPost("copy")]
    public async Task<ActionResult<Result>> Copy([FromBody] ClipboardOperationRequest request, CancellationToken cancellationToken)
    {
        var result = await _provider.CopyAsync(request.Items, request.DestinationPath, request.Resolution, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("move")]
    public async Task<ActionResult<Result>> Move([FromBody] ClipboardOperationRequest request, CancellationToken cancellationToken)
    {
        var result = await _provider.MoveAsync(request.Items, request.DestinationPath, request.Resolution, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string path, [FromQuery] bool inline = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("Path is required.");
        }

        var result = await _provider.GetFileStreamAsync(path, cancellationToken);
        if (!result.IsSuccess || result.Value == null)
        {
            return NotFound(result.ErrorMessage ?? "File not found.");
        }

        var stream = result.Value;
        var fileName = Path.GetFileName(path);
        
        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        // Use a safe whitelist for inline requests to mitigate XSS
        if (inline)
        {
            var isSafeImage = contentType is "image/jpeg" or "image/png" or "image/gif" or "image/webp";
            if (!isSafeImage && !contentType.StartsWith("text/"))
            {
                contentType = "text/plain";
            }
            else if (contentType == "text/html" || contentType == "application/javascript" || contentType == "text/xml" || contentType == "image/svg+xml")
            {
                contentType = "text/plain";
            }
            
            return File(stream, contentType);
        }

        // For download, set FileDownloadName
        return File(stream, contentType, fileName);
    }

    [HttpGet("search")]
    public async Task<ActionResult<WebFileExplorer.Shared.Models.PagedResult<FileSystemItem>>> Search([FromQuery] string path, [FromQuery] string q, [FromQuery] bool showHidden = false, [FromQuery] bool recurse = true, [FromQuery] int? skip = null, [FromQuery] int? take = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("Path is required.");
        }
        
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search query is required.");
        }

        try
        {
            var items = await _provider.SearchAsync(path, q, showHidden, recurse, skip, take, cancellationToken);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, "Access to the requested path is denied.");
        }
        catch (System.IO.DirectoryNotFoundException)
        {
            return NotFound("The specified directory does not exist.");
        }
        catch (OperationCanceledException)
        {
            // If the request was cancelled by the client, return 499 Client Closed Request (standard informally) or just empty
            return StatusCode(499, "Search operation was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching directories for path: {Path} and query: {Query}", path, q);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("compress")]
    public async Task<ActionResult<Result>> Compress([FromBody] CompressRequest request, CancellationToken cancellationToken = default)
    {
        if (request?.SourcePaths == null || !request.SourcePaths.Any() || string.IsNullOrWhiteSpace(request.DestinationZipPath))
        {
            return BadRequest(Result.Failure("Source paths and destination zip path are required."));
        }

        try
        {
            if (!await _provider.IsAuthorizedPathAsync(request.DestinationZipPath, cancellationToken))
            {
                return StatusCode(403, Result.Failure("Destination path is not authorized."));
            }

            foreach (var sp in request.SourcePaths)
            {
                if (!await _provider.IsAuthorizedPathAsync(sp, cancellationToken))
                {
                    return StatusCode(403, Result.Failure($"Source path '{sp}' is not authorized."));
                }
            }

            await _archiveService.CreateZipAsync(request.SourcePaths, request.DestinationZipPath, cancellationToken);
            return Ok(Result.Success());
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, Result.Failure("Compression was cancelled."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing files.");
            return StatusCode(500, Result.Failure("Failed to compress files."));
        }
    }

    [HttpPost("extract")]
    public async Task<ActionResult<Result>> Extract([FromBody] ExtractRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ZipPath) || string.IsNullOrWhiteSpace(request.DestinationFolderPath))
        {
            return BadRequest(Result.Failure("Zip path and destination folder path are required."));
        }

        try
        {
            if (!await _provider.IsAuthorizedPathAsync(request.ZipPath, cancellationToken))
            {
                return StatusCode(403, Result.Failure("Zip path is not authorized."));
            }

            if (!await _provider.IsAuthorizedPathAsync(request.DestinationFolderPath, cancellationToken))
            {
                return StatusCode(403, Result.Failure("Destination folder path is not authorized."));
            }

            await _archiveService.ExtractZipAsync(request.ZipPath, request.DestinationFolderPath, request.Resolution, cancellationToken);
            return Ok(Result.Success());
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, Result.Failure("Extraction was cancelled."));
        }
        catch (System.IO.InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Invalid ZIP file format.");
            return BadRequest(Result.Failure("The specified file is not a valid ZIP archive."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting zip file.");
            return StatusCode(500, Result.Failure("Failed to extract zip file."));
        }
    }
}

