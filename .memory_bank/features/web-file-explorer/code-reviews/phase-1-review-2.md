# Code Review: Phase 1 - Configuration & Roots (Review 2)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The phase one code adds the core controller, service, and layout to list configured roots correctly. Code is well formatted, input boundary validation is implemented (`EnsureAuthorizedPath`), and UI integrates with the new endpoints. However, the system currently places blocking synchronous file system I/O on async code paths without executing on background threads or enabling cancellation, which will limit scalability. 

## Findings

### Critical
None.

### Major
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` (Line 42, 85)
  - **Issue:** Method signatures are `Task`-returning, but execute synchronous blocking I/O (`DirectoryInfo.GetDirectories()`, `Directory.Exists`, etc.) directly on the calling thread, wrapping the final variable in `Task.FromResult`. This blocks the ASP.NET Core ThreadPool.
  - **Fix:** Wrap the synchronous disk interaction code blocks in `Task.Run(() => { ... })` and await them, since standard `System.IO` tree-walking does not have async variants.

- **File:** `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs` (Line 16, 22)
  - **Issue:** Missing `CancellationToken` support on async endpoints. Async/await patterns and controllers should pass down tokens to allow request abortion. 
  - **Fix:** Add `CancellationToken` parameter to `GetAuthorizedRoots`, `ListDirectories`, and the `IFileSystemProvider` interface methods, passing them into any loops or `Task.Run` delegates.

### Minor
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` (Line 95, 107)
  - **Issue:** `GetDirectories()` and `GetFiles()` allocate full arrays of file metadata into memory. To prioritize latency and lower GC overhead, these should be streamed.
  - **Fix:** Replace with `EnumerateDirectories()` and `EnumerateFiles()` respectively.

### Nits
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` (Line 88)
  - **Issue:** The local results list is instantiated as `var result = new List<FileSystemItem>();` without capacity set.
  - **Fix:** While traversing directories makes knowing the exact count hard beforehand, returning an `IEnumerable<FileSystemItem>` via `yield return` or simply `EnumerateDirectories().Select(...)` within `Task.Run` would remove the need for list allocations entirely.

## Positive Notes
- `EnsureAuthorizedPath` boundary check is well implemented and acts as a strong security guard for directory traversal.
- Handling of `DriveInfo.GetDrives()` combined with directory-based roots covers all edge cases seamlessly.

## Changes Required
1. Wrap synchronous I/O operations in `Task.Run` in `FileSystemProvider.cs` to prevent blocking the async code path.
2. Add and propagate `CancellationToken` properly across the controller and provider interface.
3. Switch `.GetDirectories()` and `.GetFiles()` calls to `.EnumerateDirectories()` and `.EnumerateFiles()` to decrease memory allocations.