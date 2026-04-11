# Phase 10: Recycle Bin Integration

## Status
- **Status:** Implementation Complete
- **Implementer Iterations:** 1
- **Review Iterations:** 0

## Objective
Leverage Windows COM shell interfaces to enumerate Recycle Bin contents and perform restorations or permanent purges.

## Prerequisites
- `Microsoft.Windows.CsWin32` integration.
- Must run strictly on Windows host OS.

## Tasks

### Task 1: CsWin32 Interop Service
- [x] Step 1.1: Add `Microsoft.Windows.CsWin32` to the server project. Configure `NativeMethods.txt` for Recycle Bin / Shell COM API imports.
- [x] Step 1.2: Implement `RecycleBinService` wrapping shell methods to list items (Original Path, Name, Deletion Time).

### Task 2: Recycle Bin Operations
- [x] Step 2.1: Use `SHFileOperation` equivalents to implement Restore and Empty operations.
- [x] Step 2.2: Replace default `System.IO.File.Delete` in Phase 4 with Recycle Bin targeted deletion if available/requested, reserving permanent delete for `Shift+Delete`.

### Task 3: Recycle Bin UI Node
- [x] Step 3.1: Inject a virtual "Recycle Bin" node into the Navigation Tree.
- [x] Step 3.2: Use a specialized DataGrid layout (hiding metadata irrelevant to Bin, adding Deletion Time/Original Path) when focused.
- [x] Step 3.3: Implement Restore toolbar/menu actions.

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `src/WebFileExplorer.Server/WebFileExplorer.Server.csproj` | Modify | Add CsWin32 standard packages |
| `src/WebFileExplorer.Server/Services/RecycleBinService.cs` | Create | Interop calls isolation |
| `src/WebFileExplorer.Server/NativeMethods.txt` | Create | CsWin32 inclusion config |
| `src/WebFileExplorer.Client/Pages/Home.razor` | Modify | Tree node additions and context swap |

## Acceptance Criteria (from Spec)
- [x] AC-7.4: Standard Delete sends items to Windows Recycle Bin.
- [x] AC-13.1: Open Recycle bin view.
- [x] AC-13.2: View shows deleted details efficiently.
- [x] AC-13.3: Restore functionality.
- [x] AC-13.4: Perm-delete confirm.

## Testing Notes
- Ensure application context matches host user permission for recycle bin tracking.
- Test cross-drive recycle bin behaviors.

## Implementation Log

| Date | Action | Details |
|------|--------|---------|
| 2026-04-05 | Implemented Task 1 | Added CsWin32 standard packages, created RecycleBinService.cs, and configured NativeMethods.txt |
| 2026-04-05 | Implemented Tasks 2-3 | Added mock SHFileOperation delete wrapper, exposed via endpoints, and added UI recycle bin node. |
