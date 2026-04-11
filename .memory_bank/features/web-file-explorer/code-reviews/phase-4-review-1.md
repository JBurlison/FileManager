# Code Review: Phase 4 - File CRUD Operations (Review #1)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The backend CRUD operations were implemented correctly via `FileSystemProvider` and the UI incorporates the API efficiently with good use of Radzen components. Security constraints around directory traversal are notably well-handled. However, there are a few severe misses related to synchronous operations inside async paths and missing keyboard features specified in the requirements.

## Findings

### Critical
None. Security path validation is effectively handled.

### Major
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` (Lines 142 & 172)
  - **Issue:** Synchronous file system checks (`Directory.Exists` and `File.Exists`) are performed outside of `Task.Run` inside `CreateFolderAsync` and `RenameAsync`. This causes blocking I/O calls on the thread pool.
  - **Fix:** Move these existence validation checks inside their respective `await Task.Run(...)` blocks.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 500+)
  - **Issue:** Missing `Shift+Delete` key binding and logic. The AC and phase documentation explicitly request distinguishing `Shift+Delete` from regular `Delete` operations. The `OnKeyDown` handler currently only processes Ctrl+A and arrow bindings.
  - **Fix:** Capture the `Delete` key inside `OnKeyDown`, differentiate if `args.ShiftKey` is active, and trigger `DeleteSelected()` accordingly. Ensure standard `Delete` has a separate UI flow or a stub for Recycle Bin in the future.

### Minor
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs`
  - **Issue:** Catching bare `Exception ex` and returning `ex.Message` in `CreateFolderAsync`, `RenameAsync`, and `DeleteAsync`. This exposes internal .NET exception messages and potentially absolute disk paths directly to the API consumer.
  - **Fix:** Return a more generic failure message to the client (e.g. "An error occurred while creating the folder.") while continuing to log the detailed exception server-side.
- **File:** `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs` (Lines 67, 81)
  - **Issue:** `Rename` and `Delete` endpoints are using `[HttpPost]`. Standard REST API design favors `[HttpPut]` or `[HttpPatch]` for updates and `[HttpDelete]` for deletions.
  - **Fix:** Consider changing the HTTP verbs to better align with REST conventions, which would affect both the Controller and the HTTP client calls in `Home.razor`.

### Nits
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line ~570)
  - **Issue:** Magic numbers used for mapping view modes to column counts (`6`, `4`, `3`).
  - **Fix:** Extract these to constants for readability.

## Positive Notes
- Excellent job enforcing `EnsureAuthorizedPath` across all CRUD boundaries. Providing this single source of truth for authorized validation prevents dangerous directory traversal vulnerabilities.
- Successful use of the structured `Result<T>` pattern for failures.

## Changes Required
1. Move the `Directory.Exists()` and `File.Exists()` operations inside `Task.Run` in `FileSystemProvider.cs`.
2. Add a listener for the `Delete` key inside `Home.razor`'s `OnKeyDown` event and pass the state of the Shift modifier to support AC-7.5.
3. Mask internal exception `.Message` properties from the UI by returning generic client-safe strings inside the `FileSystemProvider` catch blocks.