# Phase 7: File Downloads & Previews

## Status
- **Status:** Implementation Complete
- **Implementer Iterations:** 1
- **Review Iterations:** 0

## Objective
Allow users to safely retrieve or view files via download endpoints or secure text/image preview Modals.

## Prerequisites
- Phase 6 completed (Interaction shell finalized).

## Tasks

### Task 1: Download Endpoint
- [x] Step 1.1: Implement `GET /api/fileexplorer/download?path=...` using standard `FileResult` mapping appropriate MIME types.
- [x] Step 1.2: Bind default double-click (or Enter) on file type items to navigate to/trigger the download endpoint via `window.location` or `IJSObjectReference`.

### Task 2: Minimal Safe Preview
- [x] Step 2.1: Create a generic Dialog or Side-Panel identifying if a file is an image (`.jpg`, `.png`) or text (`.txt`, `.json`, `.cs`).
- [x] Step 2.2: Use raw file streaming to display the image or read the text to display in a code/text block, avoiding raw HTML execution.
- [x] Step 2.3: Bind Preview to an explicit Context Menu Action or configurable double-click default.

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs` | Modify | Add Download/Preview delivery |
| `src/WebFileExplorer.Client/Pages/Home.razor` | Modify | Open action handling / JSInterop |
| `src/WebFileExplorer.Client/Components/PreviewDialog.razor` | Create | Simple safe renderer |

## Acceptance Criteria (from Spec)
- [x] AC-11.2: Opening defaults to download or safe preview.
- [x] AC-11.3: Browser preview for images/text.
- [x] AC-11.5: No auto-exécution in browser.

## Testing Notes
- Ensure huge log files handle preview cutoff well without memory saturation.
- Test download filename escapes and encoding logic.

## Implementation Log

| Date | Action | Details |
|------|--------|---------|
| 2026-04-05 | Implemented Phase 7 | Added Download endpoint in controller, Preview dialog, bound double-click, and ctx menu actions. |
