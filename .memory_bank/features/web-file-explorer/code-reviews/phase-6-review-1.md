# Code Review: Phase 6 - Keyboard & Context Menus (Review #1)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The phase successfully implements the required context menus and keyboard shortcut hooks, properly preventing default browser behaviors and mapping shortcuts to existing commands. However, the MSTest unit tests are completely broken due to a bUnit service container locking issue, and there are unnecessary memory allocations in hot paths (keyboard events) that violate the "Latency prioritized over memory usage" requirement. 

## Findings

### Critical
- **File:** `src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs` (Line 49-51)
  - **Issue:** The unit tests fail with an `InvalidOperationException`. You are calling `Services.GetRequiredService<...>` which locks the bUnit ServiceProvider, and then immediately following it with `Services.AddSingleton(httpClient)`. 
  - **Fix:** Move `Services.AddSingleton(httpClient)` before any calls to `Services.GetRequiredService`.
- **File:** `src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs` (Line 60)
  - **Issue:** All three new tests fail or throw exceptions when executed, with `ContextMenu_OnRow_OpensContextMenuWithValidItems` throwing a `NullReferenceException` and the other two failing due to the aforementioned `InvalidOperationException`.
  - **Fix:** Ensure tests execute and pass successfully.

### Major
- **File:** `src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs` (Line 97)
  - **Issue:** `_dialogService.OnOpen` tests against `if (title == "Rename Item")`, but `Home.razor` opens the dialog with the title `"Rename"`. This mismatched assertion means even if the DI container is fixed, the test will fail.
  - **Fix:** Update the test assertion to expect `"Rename"` to match implementation.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 806)
  - **Issue:** Unnecessary memory allocation on a hot path. `var key = args.Key.ToLower();` executes on every keystroke, which violates the zero-allocation hot paths guideline prioritizing latency.
  - **Fix:** Compare directly using `string.Equals(args.Key, "f5", StringComparison.OrdinalIgnoreCase)` or use pattern matching without allocating new strings.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor` (Line 866)
  - **Issue:** High allocation in hot path. `var navKeys = new[] { "ArrowUp", "ArrowDown", ... };` instantiates a new string array into memory on *every* single keystroke from the user.
  - **Fix:** Extract `navKeys` to a static readonly array, or preferably use pattern matching (`args.Key is "ArrowUp" or "ArrowDown"...`).

### Minor
- **File:** `src/WebFileExplorer.Client/wwwroot/index.html` (Line 25)
  - **Issue:** The scope of intercepted keys misses `Del` and `Shift+Del`. While these don't always trigger negative browser defaults on non-inputs, the AC explicitly stated interception/capturing of `Del` and `Shift+Del` was expected alongside other keys. 
  - **Fix:** Consider whether `delete` key needs to be captured inside `window.keyboardInterceptor` or if `Home.razor` handling without preventDefault is sufficient.

### Nits
- **File:** `src/WebFileExplorer.Client/wwwroot/index.html` (Line 29)
  - **Issue:** `window.keyboardInterceptor` is mutating a global function variable. While the event listener is unregistered first, this doesn't support multiple grids/interceptors well. It's fine for the moment but could cause subtle bugs if `registerKeyboardInterceptor` is attached concurrently.

## Positive Notes
- Excellent job reusing the dialog service routines from previous phases rather than rewriting rename/delete/create logic.
- Well-handled edge cases in JSInterop where input boxes and text areas skip the keydown prevention (`e.target.tagName === 'INPUT'`).

## Changes Required
1. Fix test dependency injection order in `HomePhase6Tests.cs` to resolve `InvalidOperationException`.
2. Fix mismatched Rename dialog title assertions in tests.
3. Remove string and array allocations (`args.Key.ToLower()` & `new[] { ... }`) on keystroke events inside `Home.razor` to adhere to latency/allocation standards.
4. Ensure newly written UI unit tests pass successfully.