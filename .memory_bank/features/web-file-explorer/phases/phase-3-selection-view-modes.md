# Phase 3: Selection & View Modes

## Status
- **Status:** Implementation Complete
- **Implementer Iterations:** 2
- **Review Iterations:** 2

## Objective
Add single and multi-selection models consistent with Windows Explorer, and allow toggling between Details, List, and Icon views. Enable client-side sorting.

## Prerequisites
- Phase 2 completed (Files rendering in DataGrid).

## Tasks

### Task 1: View Modes
- [x] Step 1.1: Implement a layout toggle (Details vs Large Icons) switching the RadzenDataGrid out for a RadzenDataList/Card layout when Icons mode is selected.
- [x] Step 1.2: Ensure file/folder icons accurately reflect types (Folder, File, ZIP).

### Task 2: Selection Model
- [x] Step 2.1: Add single-click selection to grid and icon views without navigating.
- [x] Step 2.2: Add Multi-select support integrating Ctrl+Click (toggle) and Shift+Click (range).
- [x] Step 2.3: Maintain distinct "Focus" UI state vs. "Selected" UI state, preparing for keyboard accessibility.

### Task 3: Sorting and Visibility
- [x] Step 3.1: Configure RadzenDataGrid standard sorting for Name, Size, and Date.
- [x] Step 3.2: Add toggle for "Hidden Items" tracking preference in client state and API request parameters.

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `src/WebFileExplorer.Client/Pages/Home.razor` | Modify | Integrate view toggles, selection model |
| `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs` | Modify | Parse showHidden query flag |
| `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` | Modify | Obey hidden logic |

## Acceptance Criteria (from Spec)
- [x] AC-4.2: Switch Details, List, Large Icons.
- [x] AC-4.3: Sort by visible columns.
- [x] AC-4.4: Show/hide hidden items explicitly.
- [x] AC-5.1: Single click selects.
- [x] AC-5.3: Ctrl+Click toggles.
- [x] AC-5.4: Shift/Ctrl+A range selection.

## Testing Notes
- Ensure toggling view modes doesn't lose current directory state.
- Validate sorting with mixed numbers and letters.

## Implementation Log

| Date | Action | Details |
|------|--------|---------|
| 2026-04-05 | Implemented Phase 3 | Added View Mode toggles (Details, List, Icons) and Hidden Items toggle in Home.razor. Modified API to accept `showHidden` param. Filtered EnumerationOptions in FileSystemProvider based on hidden flag. Handled multi-select and focus states in custom view container. Custom CSS updated. |
| 2026-04-05 | Applied Review #2 Fixes | Replaced `ReadAsStringAsync` with `ReadFromJsonAsync` for memory optimization. Replaced `@onkeydown:preventDefault="true"` with a JS-based `Ctrl+A` interceptor for accessibility. Optimized `Ctrl+A` select loop utilizing `.AddRange()`. |
| 2026-04-05 | Applied Phase Review Gaps Fix | Implemented Shift+Arrow keyboard range selection in Home.razor. Added unit tests for Details view sorting and Shift+Arrow / Ctrl+A keyboard selection. |
