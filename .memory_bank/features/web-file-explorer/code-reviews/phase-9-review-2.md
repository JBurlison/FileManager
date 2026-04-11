# Code Review: Phase 9 - ZIP Archive Workflows (Review #2)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The implementer attempted to address the findings from Review 1, but the critical security vulnerability (ZipSlip) remains unfixed, and the `WebFileExplorer.Tests` project still fails to build due to the same missing dependencies in older test phases and obsolete BUnit API usage.

## Findings

### Critical
- **File:** `src/WebFileExplorer.Server/Services/ArchiveService.cs` (Line 81)
  - **Issue:** ZipSlip vulnerability remains in `ExtractZipAsync`. Using `StartsWith` for path validation without appending a trailing directory separator still allows a malicious ZIP file with an entry like `..\destination_attack\evil.txt` to extract successfully when the target is `C:\destination`. `("C:\destination_attack\evil.txt".StartsWith("C:\destination")` evaluates to `true`).
  - **Fix:** Append `Path.DirectorySeparatorChar` to `destinationFolderPath` before validation. For example:
    ```csharp
    var destinationFolderWithSlash = Path.GetFullPath(destinationFolderPath);
    if (!destinationFolderWithSlash.EndsWith(Path.DirectorySeparatorChar.ToString()))
        destinationFolderWithSlash += Path.DirectorySeparatorChar;
    
    if (!destinationPath.StartsWith(destinationFolderWithSlash, StringComparison.OrdinalIgnoreCase))
    ```

### Major
- **File:** `src/WebFileExplorer.Tests/Unit/Controllers/*.cs` (Multiple files)
  - **Issue:** Build failures. `FileExplorerControllerPhase4Tests`, `FileExplorerControllerPhase5Tests`, `FileExplorerControllerPhase7Tests`, `FileExplorerControllerPhase8Tests`, and `FileExplorerControllerTests` all fail to compile because they are still missing the `ILogger<FileExplorerController>` and `IArchiveService` constructor parameters during initialization.
  - **Fix:** Mock `ILogger<FileExplorerController>` and pass it to all instances of `FileExplorerController` in the test projects.
- **File:** `src/WebFileExplorer.Tests/Unit/Client/Pages/HomeZipWorkflowsTests.cs`
  - **Issue:** Build failures. `BunitContext.RenderComponent<TComponent>()` and `TestContext` throw `CS0618` and `CS0619` errors for using obsolete API patterns that broke the build.
  - **Fix:** Replace obsolete calls. Replace `TestContext` with `BunitContext`, and replace `RenderComponent<Home>()` with `Render<Home>()`.

### Minor
- **File:** `src/WebFileExplorer.Server/Services/ArchiveService.cs`
  - **Issue:** Synchronous file I/O operations (`archive.CreateEntryFromFile`, `entry.ExtractToFile`) execute inside a `Task.Run` delegate. While this offloads the synchronous work from the request thread, it blocks thread pool threads. 
  - **Fix:** Given that `System.IO.Compression.ZipFileExtensions` methods are synchronous, consider opening the entries with `Open()` and using `CopyToAsync` from/to a `FileStream` with `FileOptions.Asynchronous`. (Note: `Task.Run` is acceptable here but could be improved for optimal performance).

### Nits
*(None)*

## Positive Notes
- The validation against `IFileSystemProvider.GetAuthorizedRootsAsync()` in the controller was successfully implemented to prevent arbitrary reads/writes outside authorized bounds.

## Changes Required 
1. Fix the critical ZipSlip vulnerability in `ArchiveService.cs` by ensuring `destinationFolderPath` validation contains a strict boundary check with directory separators.
2. Fix the test build failures in `WebFileExplorer.Tests` by updating the setup blocks of all remaining controller tests to pass mocked arguments (`IArchiveService`, `ILogger<FileExplorerController>`).
3. Fix the `CS0619`/`CS0618` obsolete compile errors in `HomeZipWorkflowsTests`.