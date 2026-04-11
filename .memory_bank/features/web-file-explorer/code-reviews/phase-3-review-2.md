# Code Review: Phase 3 - Selection & View Modes (Review #2)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The implementer successfully addressed the performance and allocation issues from the first review. The `O(N log N)` render-loop sorting was correctly moved to the data-load boundary, string allocations in `FormatSize` were eliminated, and fire-and-forget tasks were converted to proper async event handlers. However, the fix applied for the `Ctrl+A` browser selection override introduces a new regression by statically blocking all keyboard default actions for the main UI container, taking away native scrolling and accessibility. 

## Findings

### Critical
*(None)*

### Major
- **File:** `[src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor#L55)`
  - **Issue:** Setting `@onkeydown:preventDefault="true"` statically on the main `div` container blocks *all* default keyboard behaviors, including native scrolling (Page Up/Down, Arrows) and focus traversal (Tab). While it fixes the `Ctrl+A` text-selection issue, it severely breaks general accessibility and scrolling.
  - **Fix:** Remove the static `@onkeydown:preventDefault="true"` attribute. Implement a targeted JS interop function that intercepts only `Ctrl+A` on this container to call `e.preventDefault()`, allowing all other keystrokes to function normally. (Blazor does not support dynamic prevent-default evaluation evaluated from the same keystroke context without JS).

### Minor
- **File:** `[src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor#L130)`
  - **Issue:** Using `await response.Content.ReadAsStringAsync()` followed by `JsonSerializer.Deserialize` creates a massive string allocation on the Large Object Heap (LOH) for directories with thousands of files. This causes Garbage Collection pressure, which degrades UI latency.
  - **Fix:** Use `await response.Content.ReadFromJsonAsync<IEnumerable<FileSystemItem>>(options)` to deserialize directly from the network stream without double-buffering the entire JSON payload into a massive string.

### Nits
- **File:** `[src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor#L225)`
  - **Issue:** Inside the `Ctrl+A` logic, `_selectedItems.Add(item)` is called in a `foreach` loop. Since `_selectedItems` is statically instantiated as a `List<FileSystemItem>`, using `((List<FileSystemItem>)_selectedItems).AddRange(_currentItems)` is slightly more efficient than looping.
  - **Fix:** Replace the `foreach` loop with `.AddRange()` if the underlying type guarantees it's a `List<T>`.

## Positive Notes
- `EnsureAuthorizedPath` and the `foreach` loop logic in `FileSystemProvider` look clean, explicitly addressing the previous allocation issue.
- Async event handling updates (`OnShowHiddenChanged`, `OnAddressKeyUp`) were effectively implemented.
- `Path.GetDirectoryName` fallback logic in `NavigateUp` is robust.

## Changes Required
1. Fix the accessibility regression by removing `@onkeydown:preventDefault="true"` and applying a JS-based selective `Ctrl+A` override.