# Code Review: Phase 6 - Keyboard Context (Review #5)

## Date
April 5, 2026

## Result: CHANGES NEEDED

## Summary
The phase 6 keyboard context feature successfully resolved the thread dispatching crashes, but tests are still failing because the keyboard events aren't invoking the correct UI container, and the internal grid selection state in the tests doesn't update all required component fields (like `_focusedItem` which drives `CanRename` and `CanDelete`).

## Findings

### Critical
- **File:** `src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs` (Lines 123, 167)
  - **Issue:** The test tries to fetch the container wrapping the grid using `FirstOrDefault(d => d.HasAttribute("tabindex"))`. Because `RadzenDataGrid` injects its own focusable elements, the test is selecting the wrong `div` to send the `KeyDown` event. It does not hit the `file-container` div that has `@onkeydown="OnKeyDown"`.
  - **Fix:** Update the DOM query from `d.HasAttribute("tabindex")` to `d.Id == "file-container"` to ensure the keyboard event is correctly routed to your event handler.

- **File:** `src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs` (Lines 116, 160)
  - **Issue:** Programmatically calling `gridComponent.Instance.SelectRow(item)` does not automatically fire the `RowSelect` event in tests, so `_focusedItem` (which `CanRename` and `CanDelete` depend on) never gets populated. Additionally, `SelectRow` is an async method that was not being awaited inside `InvokeAsync`.
  - **Fix:** Update the component interaction to explicitly await the row selection and manually fire the `RowSelect` callback. Change `await comp.InvokeAsync(() => gridComponent.Instance.SelectRow(item));` to:
    ```csharp
    await comp.InvokeAsync(async () => 
    {
        await gridComponent.Instance.SelectRow(item);
        await gridComponent.Instance.RowSelect.InvokeAsync(item);
    });
    ```

### Major
- *None*

### Minor
- *None*

### Nits
- *None*

## Positive Notes
- Radzen thread context errors issues were successfully fixed via `InvokeAsync`.

## Changes Required
1. In `src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs`, update the code fetching `mainContainer` to use `d.Id == "file-container"` instead of checking `HasAttribute("tabindex")`.
2. In `src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs`, await `SelectRow(item)` and trigger `RowSelect.InvokeAsync(item)` within a `Func<Task>` wrapper inside `comp.InvokeAsync`.