# Code Review: Phase 7 - File Downloads & Previews (Review #2)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The implementer successfully addressed the XSS vulnerabilities, memory saturation risks (5MB reads), missing async suffixes, and allocation loops identified in Review #1. However, the implementation of the 100KB preview cutoff introduced a stream reading bug, and there is a synchronous I/O blocker in the underlying file stream provider that violates our C# conventions.

## Findings

### Critical
*(None)*

### Major
- **File:** `src/WebFileExplorer.Client/Components/PreviewDialog.razor` (Line 50)
  - **Issue:** Bug. `await reader.ReadAsync(buffer, 0, maxPreviewSize)` does not guarantee that the buffer is filled to `maxPreviewSize`, even if the stream has more data available. It often returns a smaller chunk (e.g., 4KB or 8KB) from the underlying `HttpResponseMessage` network stream, which will cause the preview to prematurely truncate logs and mistakenly display the "Preview truncated" warning.
  - **Fix:** Change `ReadAsync` to `ReadBlockAsync` to ensure it reads until either the requested count is met or the end of the stream is reached.
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs`
  - **Issue:** Performance issue (Synchronous I/O). `GetFileStreamAsync` opens `FileStream` without specifying `FileOptions.Asynchronous`. When the controller returns `File(stream)`, ASP.NET Core will call `ReadAsync` on it, which will execute synchronously and block ThreadPool threads, directly violating the guideline against blocking calls on async paths.
  - **Fix:** Add `FileOptions.Asynchronous` to the `new FileStream(...)` constructor options (e.g., `new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous)`).

### Minor
- **File:** `src/WebFileExplorer.Client/Components/PreviewDialog.razor` (Line 49)
  - **Issue:** Minor performance issue. `var buffer = new char[maxPreviewSize];` where `maxPreviewSize` = 100K allocates a ~200KB array (C# chars are 2 bytes). This is allocated on the Large Object Heap (LOH > 85,000 bytes) on every preview request, which can lead to LOH fragmentation over time.
  - **Fix:** Use `System.Buffers.ArrayPool<char>.Shared.Rent(maxPreviewSize)` and return it with `.Return()` in a `finally` block, or read and append in smaller chunks into a `StringBuilder`.
- **File:** `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs` (Line 101)
  - **Issue:** `new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider()` is instantiated per request. It compiles a very large internal dictionary of MIME types on instantiation.
  - **Fix:** Make `_contentTypeProvider` a `private static readonly` field in the controller, or inject it via DI as a singleton.

### Nits
- **File:** `src/WebFileExplorer.Client/Components/PreviewDialog.razor` (Line 41)
  - **Issue:** The API Base URL `"api/fileexplorer/download?path=..."` is missing a leading slash. Depending on how `HttpClient.BaseAddress` handles trailing slashes, this relative URI could resolve incorrectly.
  - **Fix:** Consider prepending a forward slash (`"/api/fileexplorer/..."`).

## Positive Notes
- The mitigation for inline XSS threats using the whitelist/fallback-to-text approach is well implemented.
- Lambda exceptions in `ContextMenuService.Open` are now properly caught and logged.
- The `HomePhase7Tests` skip instructions from constraints were correctly followed, respecting the IDE testing boundaries.

## Changes Required
1. Update `PreviewDialog.razor` to use `ReadBlockAsync` instead of `ReadAsync` to avoid premature stream truncation.
2. Update `GetFileStreamAsync` in `FileSystemProvider.cs` to supply `FileOptions.Asynchronous` when creating the `FileStream`.