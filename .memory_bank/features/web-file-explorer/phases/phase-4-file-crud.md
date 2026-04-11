# Phase 4: File CRUD Operations

## Status
- **Status:** Implementation Complete
- **Implementer Iterations:** 2
- **Review Iterations:** 1

## Objective
Implement underlying file operations (Create Folder, Rename, Delete) with API backing and basic UI triggers (Command Bar/Dialogs).

## Prerequisites
- Phase 3 completed (Selection model established).

## Tasks

### Task 1: Backend CRUD APIs
- [x] Step 1.1: Implement `CreateFolderAsync`, `RenameAsync`, and `DeleteAsync` in `FileSystemProvider` using standard `System.IO` boundaries.
- [x] Step 1.2: Expose these endpoints in `FileExplorerController`. Include OS error handling returning structured `Result` wrappers.

### Task 2: UI Dialogs for Input
- [x] Step 2.1: Create basic Modals/Dialogs for taking Rename input and Folder Name input natively using Radzen Dialogs.
- [x] Step 2.2: Add Delete confirmation dialog. Ensure `Shift+Delete` styling/logic is distinct from Recycle Bin styling.

### Task 3: Command Actions
- [x] Step 3.1: Wire the Command surface (Toolbar area) buttons for Create, Rename, and Delete based on the current selection count (disabling Rename if 2+ selected).
- [x] Step 3.2: Execute API call. Auto-refresh the current view on success payload.

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` | Modify | Create/Rename/Delete impls |
| `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs` | Modify | CRUD endpoints |
| `src/WebFileExplorer.Client/Pages/Home.razor` | Modify | Append Command Bar and Actions |

## Acceptance Criteria (from Spec)
- [x] AC-7.1: Create new folder.
- [x] AC-7.2: Rename file/folder.
- [x] AC-7.5: Shift+Delete permanently deletes.
- [x] AC-14.2: Errors like file locked, invalid name are readable.
- [x] AC-14.3: Failed ops leave UI in recoverable state.

## Testing Notes
- Try renaming to an invalid name (e.g., `con` or `name?`).
- Try deleting a file that is in-use by another process.

## Implementation Log

| Date | Action | Details |
|------|--------|---------|
| 2026-04-05 | Implement CRUD Operations | Implemented Task 1 to 3 including CreateFolder, Rename, Delete using Radzen dialogs and updated Server API. |
| 2026-04-05 | Fix Phase 4 Review Gaps | Implemented command-bar delete execution logic, improved user-readable error messages for CRUD failures, and replaced placeholder UI/Provider tests with real BUnit and Moq implementations. |
