# Phase 6: Keyboard & Context Menus

## Status
- **Status:** Implementation Complete
- **Implementer Iterations:** 2
- **Review Iterations:** 1

## Objective
Enforce native-feeling interaction through comprehensive Context Menus and keyboard shortcuts mapping directly to operations.

## Prerequisites
- Phase 5 completed (All baseline commands now exist to be bound).

## Tasks

### Task 1: Explorer Context Menu
- [x] Step 1.1: Use `RadzenContextMenu` or custom overlay. Intercept right-clicks on grid items, background, and tree nodes.
- [x] Step 1.2: Render standard actions (Copy, Cut, Rename, Delete, Create) dynamically applying disabled states if unsupported.

### Task 2: Keyboard Shortcut Interceptors
- [x] Step 2.1: Implement global JSInterop or native Blazor keyboard listeners capturing typical shortcuts (F2, F5, Del, Shift+Del, Ctrl+C/V/X).
- [x] Step 2.2: Route keyboard triggers directly to the same internal command logic invoked by the toolbar/menus.
- [x] Step 2.3: Suppress actions automatically when an input/form control holds focus (preventing accidental deletions when typing a filename).

### Task 3: Arrow Key Focus Management
- [x] Step 3.1: Expand focus handling so Arrow Keys, Home, End shift selection/focus.
- [x] Step 3.2: Support enter-to-activate (matching double-click).

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| `src/WebFileExplorer.Client/wwwroot/index.html` | Modify | JS window hooks |
| `src/WebFileExplorer.Client/Pages/Home.razor` | Modify | Wire context menus and handlers |

## Acceptance Criteria (from Spec)
- [x] AC-6.1: Supports expected hotkeys globally.
- [x] AC-6.2: Supports navigation list keyboard (arrows/enter).
- [x] AC-6.3: Safe behavior when form elements are focused.
- [x] AC-6.4: Documentation for unsupported/browser-reserved shortcuts.
- [x] AC-10.1: Native-style Context Menu overrides browser menu.
- [x] AC-10.2: Invalid actions disabled.

## Testing Notes
- Validate that F5 doesn't fully reload page if intercepted reliably, or explicitly handles internal reload.
- Verify text box interactions don't trigger Hotkeys.

## Implementation Log

| Date | Action | Details |
|------|--------|---------|
| 2026-04-05 | Implemented | Context Menus via `RadzenContextMenu` & `ContextMenuService`. Implemented KeyDown overrides in `Home.razor` alongside global JS interceptor that captures & suppresses browser actions. |
| 2026-04-05 | Test Gap Fix | Added F5, Enter, Ctrl+C/X/V, and Alt-Navigation tests to `HomePhase6Tests.cs` and a new `keyboard-shortcuts.md` doc tracking AC-6.4 reserved-shortcut conflicts. |
