# Phase 5: Clipboard & Move/Copy

## Status
- **Status:** Implementation Complete
- **Implementer Iterations:** 1
- **Review Iterations:** 0

## Objective
Enable cutting, copying, and pasting files across directories using an application-level clipboard state, followed by server-side move/copy execution.

## Prerequisites
- Phase 4 completed (CRUD APIs and selection state integration).

## Tasks

### Task 1: Client Clipboard State
- [x] Step 1.1: Implement a client-side state singleton or container tracking `PendingAction` (Cut vs Copy) and selected items (`List<string>`).
- [x] Step 1.2: Add Ghosting UI effects to icons that are in a "Cut" state in the current view.

### Task 2: Backend Move/Copy Execution
- [x] Step 2.1: Implement explicit `CopyAsync` and `MoveAsync` endpoints parsing multiple source items and a target destination boundary.
- [x] Step 2.2: Add basic conflict evaluation (Merge vs Overwrite) requiring front-end overwrite flags. File locking handling.

### Task 3: Paste and Drag-Drop
- [x] Step 3.1: Bind a Paste command toolbar button to execute the pending clipboard state into the currently viewed path.
- [x] Step 3.2: Add HTML5 Drag-and-Drop handlers mapped to the Move operation when a selection is dragged into a folder node.

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `src/WebFileExplorer.Shared/Models/ClipboardState.cs` | Create | DTO / State class |
| `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` | Modify | Copy and Move implementations |
| `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs` | Modify | Operations |
| `src/WebFileExplorer.Client/Pages/Home.razor` | Modify | DragDrop hooks, ghosting styles |

## Acceptance Criteria (from Spec)
- [x] AC-7.3: Copy, cut, paste items across folders.
- [x] AC-7.6: Conflict resolution handling.
- [x] AC-8.1: Persistent pending clipboard state.
- [x] AC-8.2: Paste to destination executes action.
- [x] AC-8.3: Drag-drop Move/Copy UI behavior.

## Testing Notes
- Ensure copying large folders doesn't block UI entirely (check timeouts).
- Moving deeply nested paths checks prefix correctly against AuthorizedRoots.

## Implementation Log

| Date | Action | Details |
|------|--------|---------|
| 2026-04-05 | Implement ClipboardState | Created the Shared ClipboardState class. |
| 2026-04-05 | Implement Clipboard | Implemented C# endpoints, drag and drop, clipboard state, and backend move/copy |


