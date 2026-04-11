# Code Review: Phase 6 - Keyboard & Context Menus (Review 3)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The implementer's second round of changes successfully resolved the compile-time errors around ContextMenuService and addressed the high-frequency memory allocations inside the KeyDown event in Home.razor. However, the three HomePhase6Tests unit tests are still failing during execution.

## Findings

### Critical
- **File:** src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs (Line 61)
  - **Issue:** A NullReferenceException is thrown when attaching the OnOpen event handler because the _contextMenuService variable is null. It isn't resolved from the service provider until SetupAndRender() is called starting on line 63.
  - **Fix:** Retrieve _contextMenuService from the DI container (or initialize it properly in Setup()) *before* attempting to attach the OnOpen event handler.

- **File:** src/WebFileExplorer.Tests/Unit/UI/HomePhase6Tests.cs (Lines 125, 164)
  - **Issue:** Both keyboard tests (Keyboard_F2_TriggersRenameAction and Keyboard_Delete_TriggersDeleteAction) fail their assertions on dialogOpened. This happens because CanRename and CanDelete evaluate to alse when the keyboard shortcut is invoked. The 	d.Click() bUnit simulation does not effectively trigger a row selection in RadzenDataGrid, meaning _selectedItems remains empty.
  - **Fix:** Properly simulate item selection in bUnit so that _selectedItems is populated prior to invoking the keyboard event. This might require explicitly invoking SelectRow on the datagrid component rather than simulating a bare 	d click.

## Changes Required
1. Fix the NullReferenceException inside ContextMenu_OnRow_OpensContextMenuWithValidItems test in HomePhase6Tests.cs.
2. Correct the row selection simulation in Keyboard_F2_TriggersRenameAction and Keyboard_Delete_TriggersDeleteAction so that CanRename and CanDelete resolve 	rue and allow the action to proceed.
3. Ensure dotnet test passes with all tests yielding a successful outcome.
