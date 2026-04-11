# Code Review: Phase 7 - File Downloads & Previews (Review #1)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The phase implements the core requirements for file downloads and previews successfully. Good use of standard controller actions and JS interop for downloads. However, there are significant security and latency/memory issues related to handling large files and unsanitized inline rendering that must be resolved before approval.

## Findings

### Critical
- **File:** `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs` (Line 95)
  - **Issue:** Cross-Site Scripting (XSS) vulnerability. The `inline` block mitigates only specific content types (`"text/html"`, `"application/javascript"`, and `"text/xml"`), allowing other scriptable types (like `image/svg+xml` or `application/pdf`) to be served directly to the browser.
  - **Fix:** Use a safe whitelist for `inline` requests. Explicitly force `text/plain` for all files that are not known-safe static images (`image/jpeg`, `image/png`, `image/gif`, `image/webp`), OR add a restrictive HTTP `Content-Security-Policy` to the response.

### Major
- **File:** `src/WebFileExplorer.Client/Components/PreviewDialog.razor` (Lines 48-61)
  - **Issue:** High memory saturation and latency risk. Reading up to 5MB of a text file via `await response.Content.ReadAsStringAsync()` and assigning it directly to a Radzen `<textarea>` could violently lock the Blazor WASM thread and crash the browser. A 5.5MB file is bluntly blocked instead of fulfilling the "preview cutoff" capability defined in the testing requirements.
  - **Fix:** Instead of loading files up to 5MB entirely, enforce a strict preview cutoff (e.g., read only the first 50KB or 100KB of the file stream) and display the truncated preview with an indicator message if the file exceeds this buffer length.
- **File:** `src/WebFileExplorer.Tests/Unit/UI/HomePhase7Tests.cs`
  - **Issue:** Missing tests for the "preview cutoff" logic specified in the Testing Notes.
  - **Fix:** Add a test verifying that when a large text file is previewed, the component correctly limits the memory usage and gracefully displays truncated text.

### Minor
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 742)
  - **Issue:** An inline `HashSet<string> previewExtensions` is being re-instantiated and populated on every invocation of `PreviewOrDownload`.
  - **Fix:** Move `previewExtensions` to a `private static readonly HashSet<string>` field at the class level to avoid recurrent heap allocations on a hot path.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Lines 761-765)
  - **Issue:** `DownloadFile` invokes JS without an `await`, leaving a fire-and-forget `ValueTask` that may swallow unhandled exceptions or produce compiler warnings. It also misses the corresponding `Async` suffix.
  - **Fix:** Change `private void DownloadFile` to `private async Task DownloadFileAsync`, and `await JS.InvokeVoidAsync("open", uri, "_blank");`. Make sure callers await it.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 754)
  - **Issue:** `ShowPreviewDialog` executes async code but lacks the convention-based suffix.
  - **Fix:** Rename `ShowPreviewDialog` to `ShowPreviewDialogAsync`.

### Nits
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Lines 836-855, 914-929)
  - **Issue:** Lambda passed to `ContextMenuService.Open` uses `async (MenuItemEventArgs e) => ...` which compiles down to an `async void` delegate if `Action` is expected, which cannot be awaited and masks uncaught exceptions.
  - **Fix:** Wrap the lambda's execution in a local `try/catch` and log errors to `ILogger`, or extract it to a designated task-based method.

## Positive Notes
- Breadcrumb navigation effectively uses `StringBuilder`, following high-throughput performance guidelines.
- The default integration between double-click (or Enter key) bindings and the newly introduced download/preview controller behavior makes for a great user experience out-of-the-box.
- Well-organized extension abstractions.

## Changes Required
1. Remediate XSS risk in `FileExplorerController.cs` by ensuring unapproved inline files become `text/plain`.
2. Rewrite `PreviewDialog.razor` content ingestion to genuinely slice long log file payloads (e.g., ~100KB max stream read) rather than bulk-buffering up to 5MB.
3. Write unit tests inside `HomePhase7Tests.cs` (or equivalent) evaluating the cutoff truncation boundary on large text uploads.
4. Move `previewExtensions` out of the client procedure path into a generic static readonly block to trim allocations.
