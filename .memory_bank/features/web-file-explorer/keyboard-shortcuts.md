# Web File Explorer: Keyboard Shortcuts Support

This document serves as an addendum to the Web File Explorer specification (specifically addressing AC-6.4), detailing the expected behaviors, supported shortcuts, and handling of browser-reserved conflicts.

## Supported Global Shortcuts

When the file explorer container has focus and no input/form element is active, the following shortcuts map directly to core explorer operations:

| Shortcut | Action |
|----------|--------|
| `F5` | Refresh current directory |
| `F2` | Rename selected item |
| `Delete` | Move selected item to recycle bin / Delete |
| `Shift+Delete`| Delete selected item permanently |
| `Ctrl+C` | Copy selected item to internal clipboard |
| `Ctrl+X` | Cut selected item to internal clipboard |
| `Ctrl+V` | Paste item from internal clipboard to current directory |
| `Ctrl+Shift+N`| Create new folder |
| `Ctrl+A` | Select all items in current view |

## Interaction and Navigation

| Shortcut | Action |
|----------|--------|
| `Arrow Keys` | Move focus/selection (Up, Down, Left, Right) |
| `Enter` | Open or activate the currently selected item |
| `Backspace` | Navigate back up the history or parent directory |
| `Alt+Left` | Navigate backward |
| `Alt+Right` | Navigate forward |
| `Alt+Up` | Navigate to parent directory |
| `Home` / `End`| Jump to first or last item in list |
| `PageUp/Down` | Scroll list page by page |
| `Shift+F10` | Open context menu on selected item |

## Browser-Reserved & Unsupported Shortcuts

Due to browser security and native constraints, some traditional OS-level shortcuts cannot be overridden or behave fundamentally differently in a web context.

### Reserved by the Browser

- **Ctrl+N**: Usually reserved for "New Browser Window". We utilize **Ctrl+Shift+N** for "New Folder" to avoid masking native window creation functionality.
- **Ctrl+T**: Reserved for "New Tab". No mapping in our application.
- **Ctrl+W**: Reserved for "Close Tab". No mapping in our application.
- **Ctrl+Shift+T**: Reserved for "Reopen Closed Tab". No mapping in our application.
- **Alt+F4**: Reserved by OS to close window. Will exit browser.

### Expected Fail-Safe Behavior

When a user triggers a reserved shortcut (e.g., `Ctrl+N`):
1. **No internal interference**: The application does not call `e.preventDefault()` for strictly browser-level reserved keys that aren't gracefully overrideable across all modern browsers.
2. **Native action executes**: The browser's native action will occur (like opening a new tab or window).
3. **Form elements take precedence**: If an input text box is focused (e.g., renaming a file, or typing in the address bar), standard shortcuts (`Ctrl+C`, `Ctrl+X`, `Ctrl+V`, `Delete`, `Shift+Arrow`) fall back to text-editing behavior. Web File Explorer explicitly stops interpreting these as file operations so the user can accurately edit text without unintentionally deleting a file or triggering a global route change.
