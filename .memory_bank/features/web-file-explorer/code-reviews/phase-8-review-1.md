# Code Review: Phase 8 - Search & Filter (Review 1)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The search feature is solidly implemented with proper cancellation tokens and REST API error handling. The use of `DirectoryInfo.EnumerateFiles` with `EnumerationOptions` handles unauthorized folder traversal well. However, there are a few important issues surrounding resource management (disposing `CancellationTokenSource`) and UI state transitions that should be addressed before final approval.

## Findings

### Critical
*(None)*

### Major
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 807)
  - **Issue:** `_searchCts` is not cancelled or disposed when the component is disposed. If a user navigates away from the page while a search is running in the background, the HTTP request will continue running and potentially leak resources.
  - **Fix:** Update the `Dispose()` method to invoke `CancelSearch()` or correctly call `_searchCts?.Cancel(); _searchCts?.Dispose();`.

- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 281-320)
  - **Issue:** Unmanaged resource leak. The `_searchCts` instantiated in `PerformSearch()` is never disposed if the search successfully completes or errors out (it is currently only disposed inside the explicit manual `CancelSearch()` invoke).
  - **Fix:** Dispose of the `_searchCts` in the `finally` block of `PerformSearch()`, ensuring you only dispose it if it hasn't already been replaced by a subsequent search call.

### Minor
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 293)
  - **Issue:** `_currentItems` is not cleared when a new search begins. As a result, the grid will improperly display the contents of the previous directory (with a suddenly added "Path" column) and an active loading spinner until the search finishes. 
  - **Fix:** Add `_currentItems = new List<FileSystemItem>();` inside `PerformSearch()` before hitting the API.

- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Lines 135, 335)
  - **Issue:** User experience gets stuck in an awkward state if a search is cancelled: the UI will remain in "search mode" (`_isSearchResults = true`) but retaining whatever items were in `_currentItems` prior to the cancellation (with `_errorMessage = "Search cancelled."`). 
  - **Fix:** Consider reverting out of the search view (e.g., repeating the `LoadDirectoryAsync(_currentPath)`) when a search is cancelled so the directory view resets correctly.

### Nits
- **File:** `src/WebFileExplorer.Server/Services/FileSystemProvider.cs` (Line 699 & 714)
  - **Issue:** Generating the collection via `Enumerable.ToList()` inside `Task.Run` calculates all search matches synchronously before releasing back up the chain. For extremely large trees this uses substantial memory. While acceptable per the project's latency prioritization, future enhancements could investigate `IAsyncEnumerable` API variants. 

## Positive Notes
- `ThrowIfCancellationRequested()` placed cleanly within the `.Select()` loop is an excellent pattern for injecting cancellation checks into otherwise-blocking file enumeration streams without over-complicating the logic.
- Utilizing `EnumerationOptions { IgnoreInaccessible = true }` expertly handles localized `UnauthorizedAccessException` folders without blowing up an entire valid search.
- Correct and robust use of HTTP Status Codes (400, 403, 499, 500) and graceful logging.

## Changes Required
1. Ensure the `CancellationTokenSource` in `Home.razor` is properly disposed inside `Dispose()`.
2. Ensure the `CancellationTokenSource` in `Home.razor` is disposed inside the `finally` block of `PerformSearch()`.