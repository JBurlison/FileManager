# Code Review: Phase 5 - Clipboard & Move/Copy (Review 5)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The implementation successfully achieves the required Acceptance Criteria for moving, copying, dragging and dropping files using an integrated clipboard state. The ghosting effect operates well. However, there is still code duplication, magic hardware numbers, and other code quality improvements required before this phase can be fully approved.

## Findings

### Critical
None.

### Major
- **File:** [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor) (Lines 461-489)
  - **Issue:** Code duplication between `PerformPasteItems` and `PerformPasteItemsItemDrop`. The latter is an almost exact copy of the former but hardcodes the `"api/fileexplorer/move"` endpoint behavior.
  - **Fix:** Remove `PerformPasteItemsItemDrop` completely and update the `OnDrop` method to call `PerformPasteItems(targetItem.FullPath, true, Clipboard.Items, false)` directly.

### Minor
- **File:** [src/WebFileExplorer.Server/Services/FileSystemProvider.cs](src/WebFileExplorer.Server/Services/FileSystemProvider.cs) (Lines 197 & 312 & 334 & 410)
  - **Issue:** Magic numbers `32` and `33` are hardcoded for HResults representing sharing violations and locking.
  - **Fix:** Extract these into integer constants at the class level (e.g., `private const int ERROR_SHARING_VIOLATION = 32; private const int ERROR_LOCK_VIOLATION = 33;`).

### Nits
- **File:** [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor) (Lines 578-580)
  - **Issue:** Magic color codes inside `GetIconStyle` method (`"color: #ffca28;"`, etc.).
  - **Fix:** Move into CSS classes inside `Home.razor.css` and use the `item.Type` mapping to assign class names.

- **File:** [src/WebFileExplorer.Server/Controllers/FileExplorerController.cs](src/WebFileExplorer.Server/Controllers/FileExplorerController.cs) (Line 72 & 85)
  - **Issue:** Controller actions copy & move duplicate argument validation (`request == null || !request.SourcePaths.Any() || string.IsNullOrWhiteSpace(request.DestinationPath)`).
  - **Fix:** Consider utilizing `[Required]` data annotations on the `ClipboardOperationRequest` properties.

## Positive Notes
- Good use of `IsGhosted` flag to immediately reflect state changes on the front-end clipboard wrapper!
- Beautifully utilizes continuous overwrite and merge conflict evaluation via Blazor DialogService.
- Threading `CancellationToken` consistently into `IFileSystemProvider` backend actions is well done.

## Changes Required
1. Remove `PerformPasteItemsItemDrop` and reuse `PerformPasteItems` mapping `isCut` as `true` inside [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor).
2. Extract magic HResult numbers 32 and 33 into named constants inside [src/WebFileExplorer.Server/Services/FileSystemProvider.cs](src/WebFileExplorer.Server/Services/FileSystemProvider.cs).