# Code Review: Phase 5 - Clipboard & Move/Copy (Review #6)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The implementation technically hits the Acceptance Criteria, but there are some fundamental architecture and flow issues prioritizing stability and correctness. Drag-and-drop incorrectly mutates global state, the move/copy conflict resolution is not transactional, and synchronous file I/O within a Threadpool task violates the async conventions. 

## Findings

### Critical
(None)

### Major
- **File:** src/WebFileExplorer.Server/Services/FileSystemProvider.cs (Lines 289, 325)
  - **Issue:** Conflict checks within CopyAsync and MoveAsync are evaluated inside the loop and are not transactional. If an overwrite conflicts on the Nth item of the selected properties, the first N-1 items have already been moved. The subsequent frontend retry with overwrite=true relies on the silent skip of missing source paths, putting the application in an indeterminate state.
  - **Fix:** Iterate and validate all sourcePaths against their destination counterpart sequentially to ensure no conflict exists (if !overwrite) BEFORE performing any underlying I/O.
- **File:** src/WebFileExplorer.Server/Services/FileSystemProvider.cs (Line 291)
  - **Issue:** Uses synchronous File.Copy inside an wait Task.Run. Although this offloads the thread from the request, it still blocks a ThreadPool thread completely until IO is complete, risking thread starvation during large file transfers. Violates the "Async I/O for all I/O operations" checklist standard.
  - **Fix:** Refactor file copying logic to use async I/O streams: wait sourceStream.CopyToAsync(destStream, cancellationToken).
- **File:** src/WebFileExplorer.Client/Pages/Home.razor (Line 414)
  - **Issue:** The OnDragStart function sets the application-wide ClipboardStateContainer to Cut state. If the user drops the item on an invalid target or presses Escape to cancel the drag context, the item permanently remains in the cut clipboard state with ghosted UI effects.
  - **Fix:** Do not modify the global clipboard state during transient UI interactions like drag. Implement a separated _draggedItems state specifically to handle drag effects and the internal PerformPasteItems mapping payload.

### Minor
- **File:** src/WebFileExplorer.Client/Pages/Home.razor (Line 368)
  - **Issue:** Tracking cut paths internally uses Clipboard.Items.Contains(item.FullPath). Contains performs a default case-sensitive operation, risking ghosting failures if runtime OS casings do not universally align. 
  - **Fix:** Compare target strings with .Any(p => string.Equals(p, item.FullPath, StringComparison.OrdinalIgnoreCase)).

### Nits
- **File:** src/WebFileExplorer.Shared/Models/ClipboardOperationRequest.cs
  - **Issue:** The parameter model name SourcePaths functions well, but frontend global state naming uses Items.
  - **Fix:** Unifying to Items or SourcePaths system-wide creates better contextual continuity.

## Positive Notes
- Recursive CopyDirectory design handles complex hierarchy structures successfully.
- Handling ghosting directly within the razor iteration is clean and distinct.
- Overall abstraction with Controller passing down cleanly to a FileSystem interface represents the desired csharp-conventions well.

## Changes Required
1. Validate filesystem availability and write permissions for all sources in CopyAsync/MoveAsync before committing modifying operations. 
2. Replace static File.Copy instances with Stream.CopyToAsync yielding await paths. 
3. Remove global Clipboard.SetState from drag behaviors mapped within Home.razor.
