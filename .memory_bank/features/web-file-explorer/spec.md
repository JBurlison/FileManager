# Feature: Web File Explorer

## Status
- **Created:** 2026-04-04
- **Last Updated:** 2026-04-05
- **Status:** Approved

## Overview
Build a C# Blazor web application that reproduces the expected day-to-day behavior of Windows File Explorer in a browser for a Windows 11 host. The feature must provide Explorer-style navigation, keyboard-centric file management, Windows-like selection and context behavior, and common Explorer workflows such as rename, copy, move, delete, ZIP handling, properties, and Recycle Bin interactions, while listening only on local `10.0.0.x` addresses.

## Problem Statement
Users need a browser-accessible file manager for a Windows machine that feels functionally familiar to Windows File Explorer without requiring RDP, remote shell access, or direct use of the desktop shell. The current application is only a starter Blazor shell. The new feature must define the expected Explorer-parity experience, the server-side filesystem boundaries needed to support it safely, and the strict local-network hosting rules already present in the codebase.

## Goals
- [ ] Deliver an Explorer-like shell with tree navigation, address bar, breadcrumbs, command surface, status bar, and file list.
- [ ] Support the Windows Explorer keyboard and mouse workflows users expect for browsing and file management.
- [ ] Support file and folder creation, rename, copy, cut, paste, move, delete, restore, compress, and extract flows.
- [ ] Restrict listening and request access to local `10.0.0.x` addresses only.
- [ ] Optimize for low-latency navigation, selection, and large-folder rendering.

## Non-Goals
- Replacing the Windows shell, taskbar, desktop, shell namespace extensions, or shell plug-in model.
- Uploading files from a remote client machine's filesystem to the server (browser-to-host file uploads) is out of scope for the core shell features.
- Supporting internet exposure, wildcard binding, or non-`10.0.0.x` hosting.
- Supporting Linux or macOS hosts.
- Guaranteeing parity for browser-reserved shortcuts or native shell behaviors that a browser cannot safely intercept.
- Executing arbitrary desktop shell extensions, preview handlers, or COM handlers.
- Managing remote UNC shares, mapped network drives, or cloud-backed providers unless they appear as ordinary local filesystem paths under an authorized root.

## Functional Requirements

### FR-1: Explorer Shell Layout
- **Description:** The application must present a Windows File Explorer-inspired shell with a persistent navigation pane, address bar, breadcrumb path display, command surface, content pane, and status area.
- **Acceptance Criteria:**
  - [ ] AC-1.1: The default landing experience shows a persistent left navigation area and a primary content pane without requiring a page reload between folder transitions.
  - [ ] AC-1.2: The shell displays the current location, selection summary, and visible item count.
  - [ ] AC-1.3: The layout remains usable at common desktop browser widths down to 1280 pixels without hiding critical navigation controls.
  - [ ] AC-1.4: The shell preserves current navigation context when non-destructive dialogs or side panels are opened and closed.
- **Priority:** Must Have

### FR-2: Path Navigation and History
- **Description:** The application must support Explorer-style navigation by tree selection, double-click activation, breadcrumbs, typed paths, and history traversal.
- **Acceptance Criteria:**
  - [ ] AC-2.1: Users can navigate to an available drive or authorized root from the navigation tree.
  - [ ] AC-2.2: Users can navigate into folders by double-clicking or pressing `Enter` on a focused folder.
  - [ ] AC-2.3: Users can navigate using Back, Forward, and Up actions, with history preserving the previously visited locations in order.
  - [ ] AC-2.4: Users can type a fully qualified Windows path into the address bar and navigate if the path is valid and authorized.
  - [ ] AC-2.5: Invalid, unauthorized, or unavailable paths produce a user-visible error instead of leaving the view in an ambiguous state.
- **Priority:** Must Have

### FR-3: Navigation Tree and Common Locations
- **Description:** The application must expose Explorer-like navigation roots relevant to the configured host filesystem scope.
- **Acceptance Criteria:**
  - [ ] AC-3.1: The navigation tree shows a configured allowlist of authorized filesystem roots (e.g. `["D:\Shares", "C:\Data"]`).
  - [ ] AC-3.2: Tree nodes expand and collapse without a full page reload.
  - [ ] AC-3.3: The current folder is reflected in the navigation tree selection state when that folder exists in the tree.
- **Priority:** Should Have

### FR-4: Directory Listing and View Modes
- **Description:** The content pane must display folder contents with Windows Explorer-like metadata, sorting, and view modes.
- **Acceptance Criteria:**
  - [ ] AC-4.1: The details view displays at least Name, Date Modified, Type, and Size columns where the values are applicable.
  - [ ] AC-4.2: Users can switch between at least Details, List, Large Icons, and Small/Medium icon-oriented views.
  - [ ] AC-4.3: Users can sort by any visible supported column in ascending or descending order.
  - [ ] AC-4.4: Hidden items can be shown or hidden through an explicit toggle.
  - [ ] AC-4.5: Folder and file icons visibly distinguish folders, files, and common special cases such as ZIP archives.
- **Priority:** Must Have

### FR-5: Selection, Focus, and Activation Model
- **Description:** The application must mimic Explorer selection and focus behavior for single-select, multi-select, range-select, keyboard focus, and activation.
- **Acceptance Criteria:**
  - [ ] AC-5.1: Single-click selects an item without activating it.
  - [ ] AC-5.2: Double-click activates the focused item using the implemented Explorer action for that item type.
  - [ ] AC-5.3: `Ctrl+Click` toggles individual item selection state.
  - [ ] AC-5.4: `Shift+Click`, `Shift+Arrow`, and `Ctrl+A` work with Windows Explorer-like range and bulk selection behavior.
  - [ ] AC-5.5: Focused-item styling is visually distinct from selected-item styling when the interaction model requires both states.
- **Priority:** Must Have

### FR-6: Keyboard Shortcuts and Hotkeys
- **Description:** The application must support practical Windows Explorer hotkeys and keyboard navigation while respecting browser security boundaries.
- **Acceptance Criteria:**
  - [ ] AC-6.1: The file manager supports `F2`, `Delete`, `Shift+Delete`, `Ctrl+C`, `Ctrl+X`, `Ctrl+V`, `Ctrl+A`, `Ctrl+Shift+N`, `Alt+Left`, `Alt+Right`, `Alt+Up`, `Backspace`, `Enter`, and `F5` when focus is inside the file manager.
  - [ ] AC-6.2: The file manager supports arrow keys, `Home`, `End`, `Page Up`, `Page Down`, and type-to-focus navigation within the current list.
  - [ ] AC-6.3: Shortcut handlers do not fire destructive file-manager actions while focus is in a text entry control.
  - [ ] AC-6.4: Unsupported or browser-reserved shortcuts are documented and fail safely without triggering the wrong Explorer action.
- **Priority:** Must Have

### FR-7: File and Folder Lifecycle Operations
- **Description:** The application must support the core create, rename, copy, move, delete, and restore workflows users expect from Explorer.
- **Acceptance Criteria:**
  - [ ] AC-7.1: Users can create a new folder in the current directory from the command surface, context menu, or hotkey.
  - [ ] AC-7.2: Users can rename a single file or folder inline or through a focused rename dialog.
  - [ ] AC-7.3: Users can copy, cut, and paste one or more selected items across authorized folders.
  - [ ] AC-7.4: Standard Delete sends eligible items to the Windows Recycle Bin when supported by the underlying location.
  - [ ] AC-7.5: `Shift+Delete` permanently deletes eligible items only after explicit confirmation.
  - [ ] AC-7.6: Overwrite, merge, and naming conflicts require explicit user choice before destructive resolution.
  - [ ] AC-7.7: Operation results report success, partial success, or failure at the item level.
- **Priority:** Must Have

### FR-8: Clipboard, Drag-and-Drop, and Move Semantics
- **Description:** The application must preserve Explorer-style pending clipboard state and support practical drag-and-drop workflows where the browser permits them.
- **Acceptance Criteria:**
  - [ ] AC-8.1: Cut and copy operations create a pending clipboard state that remains visible until paste completion or cancellation.
  - [ ] AC-8.2: Pasting into a valid target folder applies the pending cut or copy action with correct post-operation selection behavior.
  - [ ] AC-8.3: Dragging selected items to another folder within the explorer UI initiates a move or copy flow consistent with the documented application rules.
  - [ ] AC-8.4: If browser limitations prevent a specific drag-and-drop gesture from behaving exactly like desktop Explorer, the UI provides an alternative command path for the same action.
- **Priority:** Should Have

### FR-9: Search, Filter, Sort, and Refresh
- **Description:** The application must provide Explorer-style search and refresh capabilities within the current scope.
- **Acceptance Criteria:**
  - [ ] AC-9.1: Users can search the current folder and optionally include subfolders.
  - [ ] AC-9.2: Search results display item names, parent paths, and enough metadata to distinguish similarly named results.
  - [ ] AC-9.3: Users can cancel a running search.
  - [ ] AC-9.4: Users can refresh the current view manually without losing the active location.
  - [ ] AC-9.5: The UI indicates whether search results are complete, still loading, canceled, or failed.
- **Priority:** Should Have

### FR-10: Context Menus, Properties, and Explorer Commands
- **Description:** The application must expose implemented actions through Windows-like context menus and properties surfaces.
- **Acceptance Criteria:**
  - [ ] AC-10.1: Users can open a context menu with the mouse and keyboard for the current selection.
  - [ ] AC-10.2: The context menu shows only actions valid for the current selection and disables unsupported actions visibly.
  - [ ] AC-10.3: A properties surface shows path, type, size, timestamps, and file attributes for the selected item or aggregate selection.
  - [ ] AC-10.4: The command surface and context menu expose consistent action names, icons, and enabled states.
- **Priority:** Must Have

### FR-11: File Open, Preview, and Download Behavior
- **Description:** The application must define safe behavior for opening folders, opening files, previewing supported content, and downloading items to the browser client.
- **Acceptance Criteria:**
  - [ ] AC-11.1: Opening a folder always navigates within the explorer UI.
  - [ ] AC-11.2: Opening a file defaults to browser download or safe preview; host-side launch must require explicitly invoking an approved action (e.g. context menu) to prevent accidental execution.
  - [ ] AC-11.3: The application provides browser preview for safe, supported file types such as text, images, and PDF when that preview is explicitly enabled.
  - [ ] AC-11.4: Unsupported file types and failed open attempts surface a clear fallback action instead of silently doing nothing.
  - [ ] AC-11.5: The browser never auto-executes arbitrary file content without an explicit user action.
- **Priority:** Should Have

### FR-12: ZIP Archive Workflows
- **Description:** The application must support Explorer-like ZIP creation and extraction flows.
- **Acceptance Criteria:**
  - [ ] AC-12.1: Users can create a ZIP archive from one or more selected items in the current location.
  - [ ] AC-12.2: Users can extract a ZIP archive into the current folder or a user-selected destination.
  - [ ] AC-12.3: ZIP extraction conflicts require explicit resolution before overwriting files.
  - [ ] AC-12.4: Unsupported archive content or invalid ZIP files surface actionable errors without crashing the session.
- **Priority:** Must Have

### FR-13: Recycle Bin Browsing and Restore
- **Description:** The application must provide Recycle Bin visibility and recovery workflows comparable to everyday Explorer usage.
- **Acceptance Criteria:**
  - [ ] AC-13.1: Users can open a Recycle Bin view from the navigation surface.
  - [ ] AC-13.2: The Recycle Bin view shows deleted item name, original location, deletion time, and size when the Windows APIs make those fields available.
  - [ ] AC-13.3: Users can restore one or more selected Recycle Bin items to their original locations.
  - [ ] AC-13.4: Users can permanently delete selected Recycle Bin items after explicit confirmation.
  - [ ] AC-13.5: If Recycle Bin access is unsupported or unavailable for a volume, the UI shows a clear unsupported state.
- **Priority:** Should Have

### FR-14: Status, Progress, and Error Transparency
- **Description:** The application must surface progress, completion, and failure details for filesystem operations in a way that feels predictable and Explorer-like.
- **Acceptance Criteria:**
  - [ ] AC-14.1: Long-running operations such as copy, move, delete, search, compress, extract, and restore display progress or a durable in-progress indicator within 100 ms of user initiation.
  - [ ] AC-14.2: Errors such as access denied, file locked, path too long, not found, invalid name, conflict, and unsupported archive are presented in user-readable terms.
  - [ ] AC-14.3: Failed operations leave the UI in a recoverable state and allow refresh without requiring a full application restart.
  - [ ] AC-14.4: Server logs capture enough detail to diagnose failures without exposing unnecessary host details to the browser.
- **Priority:** Must Have

### FR-15: Local-Network-Only Hosting Enforcement
- **Description:** The application must listen only on local `10.0.0.x` addresses, reject disallowed source addresses, and preserve the trusted-local-network deployment model.
- **Acceptance Criteria:**
  - [ ] AC-15.1: Production hosting binds only to explicitly configured local IPv4 addresses matching `10.0.0.x` and never binds to wildcard, public, IPv6, or loopback addresses.
  - [ ] AC-15.2: Startup fails with a clear configuration error when no matching `10.0.0.x` interface exists.
  - [ ] AC-15.3: Requests whose remote address does not match the configured `10.0.0.` prefix are rejected with HTTP 403 and logged.
  - [ ] AC-15.4: The application does not require interactive sign-in for trusted requests originating from the allowed `10.0.0.x` subnet.
  - [ ] AC-15.5: Deployment guidance documents how to configure the allowed prefix, verify the effective bind address, and validate that disallowed requests are rejected.
- **Priority:** Must Have

### FR-16: Session State and Explorer Continuity
- **Description:** The application should preserve the user’s working context across normal browser interactions.
- **Acceptance Criteria:**
  - [ ] AC-16.1: Refreshing the page restores the current location, view mode, sort order, and hidden-item preference when feasible.
  - [ ] AC-16.2: Navigating away from the current folder and returning through history restores the last known scroll position and selection state when feasible.
  - [ ] AC-16.3: A new browser tab or window starts a new logical session and does not corrupt another session’s selection or clipboard state.
- **Priority:** Should Have

## Non-Functional Requirements

### NFR-1: Performance
- The first usable render for warm-cache navigation to a folder containing up to 1,000 visible items must complete within 300 ms on target hardware.
- Selection changes, focus movement, and hotkey acknowledgement must produce visible UI feedback within 100 ms of user input.
- File operations must acknowledge initiation within 100 ms, even when the underlying work continues asynchronously.
- Large directories must use virtualization, paging, or incremental rendering so the client does not attempt to render the entire folder at once.
- Search, compression, extraction, delete, copy, and move workflows must support cancellation or safe abandonment where the underlying operation semantics allow it.

### NFR-2: Security
- The solution must run on Windows and operate only within explicitly configured `AuthorizedRoots` mapped in `appsettings.json`.
- All incoming paths must be normalized using `Path.GetFullPath()` and validated using explicit prefix matching against the `AuthorizedRoots` configuration array before any filesystem action executes, strictly rejecting any relative traversals (`..`).
- The host must not listen on any non-`10.0.0.x` interface in production.
- The application may assume trusted users on the local subnet and therefore does not require sign-in, but destructive operations must still require explicit user initiation.
- File preview and download flows must use content handling that does not allow arbitrary execution in the browser.

### NFR-3: Scalability
- The backend must support multiple concurrent local users without sharing mutable per-user navigation, selection, or clipboard state across sessions.
- One user’s long-running operation must not block UI responsiveness for another user.
- The API design must support incremental loading for deep folder trees and large search result sets.

### NFR-4: Reliability
- Unhandled exceptions during filesystem operations must be logged and translated into safe error responses rather than crashing the application.
- Partial failures in batch operations must report which items succeeded and which failed.
- The UI must recover cleanly from transient HTTP failures by allowing retry or refresh without losing the entire application shell.

### NFR-5: Accessibility and Usability
- Keyboard-only users must be able to browse, select, and execute all Must Have workflows without relying on pointer-only interactions.
- Focus order, selected-state annunciation, and dialog interactions must remain understandable to assistive technologies where Blazor and the chosen UI library support it.
- Explorer-inspired visuals must not rely solely on color to communicate selected, focused, disabled, or error states.

## Technical Constraints
- The feature must be implemented within the existing hosted Blazor WebAssembly plus ASP.NET Core server architecture in this repository.
- The current repository already uses .NET 10, Blazor WebAssembly, ASP.NET Core hosting, MSTest, and Radzen components; the implementation should extend those choices unless there is a documented reason to replace them.
- Because browser code cannot directly access the Windows filesystem, all filesystem operations must execute through server-side services on the Windows host.
- The target host is Windows 11 and must honor Windows path semantics, drive enumeration, file attributes, ZIP support, and Recycle Bin behavior.
- The server must remain restricted to local `10.0.0.x` binding and request filtering, building on the existing network-binding and IP-middleware patterns already present in the codebase.
- The application must not depend on internet connectivity or cloud services for core Explorer workflows.
- Browser and OS shortcut reservations may prevent perfect native parity for some key combinations. Only Chromium-based browsers (Edge/Chrome) are in scope for primary hotkey parity mapping.
- Native shell extension execution is out of scope; only explicitly implemented actions are allowed.

## Dependencies
- ASP.NET Core and Blazor hosted application infrastructure.
- Windows filesystem APIs available through .NET.
- System.IO.Compression or an equivalent Windows-safe ZIP library.
- A Windows-compatible approach for Recycle Bin enumeration, restore, and permanent delete.
- UI component capabilities for tree view, virtualization, dialogs, menus, icons, and keyboard interaction.
- Configuration for allowed roots, allowed `10.0.0.` bind prefix, and listening port.

## Data Model Changes
- Add configuration models for authorized filesystem roots, allowed network prefix, port, preview policy, and Explorer behavior flags.
- Add shared DTOs for directory items, drives, tree nodes, breadcrumbs, properties metadata, and Recycle Bin entries.
- Add client session-state models for current path, history stack, sort order, view mode, hidden-item visibility, selection set, and pending clipboard state.
- Add operation-tracking models for long-running copy, move, delete, search, compress, extract, and restore workflows.

## API Changes
- Add endpoints or equivalent handlers for drive and tree enumeration.
- Add endpoints or equivalent handlers for listing directory contents, retrieving breadcrumbs, and resolving typed paths.
- Add endpoints or equivalent handlers for create, rename, copy, move, delete, restore, compress, extract, and refresh actions.
- Add endpoints or equivalent handlers for search, metadata retrieval, properties, preview, download, and host-open requests.
- Extend the server configuration surface for authorized roots and strict `10.0.0.` binding behavior.

## UI/UX Changes
- Replace the placeholder home page with an Explorer-style shell that includes navigation tree, address bar, toolbar or command bar, content view, and status region.
- Add details, list, and icon-based presentation modes with Windows-like iconography and selection behavior.
- Add keyboard-first workflows, context menus, and dialogs for rename, delete confirmation, conflict resolution, properties, and progress reporting.
- Add a Recycle Bin entry point and ZIP workflow affordances that feel consistent with Explorer.
- Preserve a familiar Windows information hierarchy while remaining practical inside a desktop browser viewport.

## Edge Cases
- Authorized roots that contain tens of thousands of files or deeply nested folders.
- Locked files, access-denied paths, files deleted externally during viewing, and paths that become unavailable mid-operation.
- Symbolic links, junctions, reparse points, and cycles that could cause recursive traversal issues.
- Recycle Bin items whose original locations no longer exist.
- File names with invalid target characters, reserved names, or path lengths near Windows limits.
- Concurrent modifications from another user or another process while the browser session is open.
- Browser refresh, tab close, or network interruption during long-running operations.
- Browser-reserved shortcuts that cannot be intercepted before the browser handles them.
- Host-side open requests that fail because no association exists or the host execution context disallows the action.

## Revision History
| Date | Author | Changes |
|------|--------|---------|
| 2026-04-04 | GitHub Copilot | Initial draft |
| 2026-04-04 | GitHub Copilot | Expanded scope to cover Recycle Bin, ZIP workflows, trusted-local-network access, and host-side open behavior. |
| 2026-04-05 | GitHub Copilot | Reworked the specification to align with the spec template, tightened the requirement to local `10.0.0.x` binding, and expanded Explorer-parity requirements, performance targets, and API/data model expectations. |
| 2026-04-05 | GitHub Copilot | Applied Due Diligence findings: Resolved open questions, codified `AuthorizedRoots` restrictions, removed Quick Access for MVP, and confirmed browser parity scope. |