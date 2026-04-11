# Code Review: Phase 3 - Selection & View Modes (Review #1)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The implementation fulfills the requirements for phase 3 successfully, with view transitions, hidden files logic, and multi-selection operational. However, there are significant performance concerns regarding LINQ queries in the rendering path and memory allocations that violate the explicit latency prioritization requirement. Additionally, a Blazor-specific event handling bug exists in the keyboard multiple selection logic.

## Findings

### Critical
*(None)*

### Major
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 96 and Line 367)
  - **Issue:** The `_currentItems.OrderBy(i => i.Type != FileSystemItemType.Folder).ThenBy(i => i.Name)` LINQ expression executes directly in the `@foreach` loop markup and inside `OnItemClick`. This triggers an `O(N log N)` sort operation on every single UI render or click event, severely degrading latency for directories with many files.
  - **Fix:** Sort `_currentItems` exactly once inside `LoadDirectoryAsync` immediately after JSON deserialization. Store it as a materialized `List<FileSystemItem>` and iterate over the pre-sorted list in the markup.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 324)
  - **Issue:** `string[] sizes = { "B", "KB", "MB", "GB", "TB" };` is defined locally inside `FormatSize(long bytes)`. Because `FormatSize` is invoked for every element in the RadzenDataGrid, this allocates a new string array per file on every render.
  - **Fix:** Move `sizes` to a `private static readonly string[]` field to prevent runaway allocation.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 388)
  - **Issue:** The `@onkeydown:preventDefault="_preventDefaultKeyboard"` directive cannot dynamically prevent default actions (like `Ctrl+A` text selection) by altering `_preventDefaultKeyboard` *inside* the `OnKeyDown` event handler. Blazor evaluates modifier properties before the event runs.
  - **Fix:** Bind `@onkeydown:preventDefault` to a static true value on the list container and handle all inner key presses manually, or use a specialized JS Interop call for dynamic key blocking.

### Minor
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` (Line 41)
  - **Issue:** `var pathWithSlash = EnsureTrailingSlash(fullPath);` is calculated inside the `foreach` loop over `AuthorizedRoots` but does not depend on the loop variable.
  - **Fix:** Hoist the variable outside of the `foreach` loop to eliminate unnecessary allocations.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 198)
  - **Issue:** Manual string operations in `NavigateUp` (`lastSlash`, `TrimEnd`) can be fragile on edge cases.
  - **Fix:** Consider leveraging `System.IO.Path.GetDirectoryName(_currentPath)` if compatible with the target pathing format instead of manual iteration.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Lines 185, 336)
  - **Issue:** `_ = NavigateToAddress()` and `_ = LoadDirectoryAsync(...)` use fire-and-forget on tasks, hiding exceptions from Blazor's task dispatcher.
  - **Fix:** Change the UI event handlers (`OnAddressKeyUp`, `OnShowHiddenChanged`) to return `async Task` directly, which Blazor intrinsically supports.

### Nits
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` (Line 27)
  - **Issue:** `EnsureAuthorizedPath` lacks XML doc comments.
  - **Fix:** Add descriptive summaries like other methods in this class class.

## Positive Notes
- Very good handling of `UnauthorizedAccessException` mapping to HTTP 403 on the controller.
- The multi-select boundary logic using `_lastClickedItem` handles complex shift-range scenarios cleanly.
- Background offloading (`Task.Run`) for synchronous `System.IO` I/O calls maintains responsiveness correctly.

## Changes Required
1. Remove dynamic `OrderBy` from rendering loops and sort collections upon consumption in `Home.razor` to fix O(N log N) render delays.
2. Fix `string[] sizes` allocations in `FormatSize` by extracting it as a static readonly field.
3. Rework the dynamic `preventDefault` functionality for `Ctrl+A` to correctly block browser selection.