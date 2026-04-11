# Code Review: Phase 5 - Clipboard & Move/Copy (Review #2)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The phase implementation covers the backend copy and move handlers correctly, as well as dragging logic and ghosting styles on the frontend. However, the Blazor client implementation fails to compile due to a missing dependency injection registration for `ClipboardStateContainer` in the `Home.razor` component. Furthermore, essential UI controls requested by the phase for Cut, Copy, and Paste commands are missing, and no conflict resolution logic (Merge vs Overwrite) is wired up in the interface.

## Findings

### Critical
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 504 and elsewhere)
  - **Issue:** Code fails to compile with `CS0103: The name 'Clipboard' does not exist`. `ClipboardStateContainer` is referenced but never injected or defined.
  - **Fix:** Add `@inject WebFileExplorer.Client.Services.ClipboardStateContainer Clipboard` at the top of the file alongside the other injected services.

### Major
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 12)
  - **Issue:** Missing Cut, Copy, and Paste commands. The objective explicitly states "Bind a Paste command toolbar button to execute the pending clipboard state into the currently viewed path."
  - **Fix:** Add toolbar buttons for Cut, Copy, and Paste operations. Bind them to the `CutSelected`, `CopySelected`, and `PasteItems` methods respectively. Disable them when `CanCut`, `CanCopy`, and `CanPaste` evaluate to `false`.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 514, 555)
  - **Issue:** Conflict evaluation is missing. The task requests "Add basic conflict evaluation (Merge vs Overwrite) requiring front-end overwrite flags," yet `Overwrite = false` is currently hardcoded and never prompts the user.
  - **Fix:** If `PasteItems` (or the drag-and-drop `OnDrop` handler) fails due to the file already existing on the backend, prompt the user with a `DialogService.Confirm` message (e.g. "Destination item exists. Overwrite?"). If the user confirms, resubmit the request with `Overwrite = true`.
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` (Lines 422, 492)
  - **Issue:** Relying on `File.Copy` and `File.Move` when `overwrite == false` to throw a generic exception is inadequate. It is difficult for the client to differentiate between "Target exists" and "Other IO failure" unless string-matching on the exception error message.
  - **Fix:** Add a check `if (!overwrite && (File.Exists(destItemPath) || Directory.Exists(destItemPath)))` to explicitly return `Result.Failure("Destination item already exists.")` natively. This allows the frontend to explicitly catch that text string for launching its user prompt logic.

### Minor
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs`
  - **Issue:** `CopyDirectoryRecurse` method lacks explicit `cancellationToken.ThrowIfCancellationRequested()` over checks recursively spanning files and nested folders, although it's passed down.
  - **Fix:** While it passes the token properly to subsequent iterations, ensure to call `cancellationToken.ThrowIfCancellationRequested()` right before copying each file or sub-directory loop.

## Positive Notes
- The Drag and Drop event endpoints (`@ondragstart`, `@ondrop`) bind efficiently to `OnDrop` using the file system path identifiers logic successfully.
- Proper separation of states using `ClipboardState` on `.Shared` across backend boundaries.

## Changes Required
1. Inject the `ClipboardStateContainer` correctly in `Home.razor` to resolve the client build failures (`CS0103`).
2. Add the Cut, Copy, and Paste buttons with their respective properties hooked into the `.d-flex` toolbar header.
3. Enhance `CopyAsync` and `MoveAsync` in the backend provider to yield an explicit "Destination item already exists." response when `overwrite` is false natively. 
4. Implement a confirmation dialog prompt in `PasteItems` (and `OnDrop`) to resubmit operations with `Overwrite = true` explicitly if the back-end response message flags conflict resolution.