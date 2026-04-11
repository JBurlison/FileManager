# Implementation Plan: Web File Explorer

## Overview
This plan implements a C# Blazor web application that provides a Windows File Explorer-like experience. The implementation extends the existing Blazor WebAssembly and ASP.NET Core project structure. It prioritizes latency and native-feeling keyboard/mouse workflows. Operations execute strictly on authorized filesystem roots, validated on the server. Non-destructive layout options, selection paradigms, basic CRUD, clipboard operations, searching, ZIP support, and Recycle Bin integration will be built iteratively.

## Phases

| Phase | Name | Description | Est. Complexity |
|-------|------|-------------|-----------------|
| 1 | Configuration & Roots | Server-side boundaries, options, base DTOs, and API to fetch authorized roots. Initial tree UI. | Low |
| 2 | Directory Browsing | Reading directory contents, normalizing paths, breadcrumbs, and basic Details view UI. | Medium |
| 3 | Selection & View Modes | Multi-select (Ctrl/Shift), focused vs selected states, and switching between Details/List/Icon views. | Medium |
| 4 | File CRUD Operations | APIs and UI dialogs for folder creation, file/folder rename, and standard deletion. | Medium |
| 5 | Clipboard & Move/Copy | Internal clipboard state management (Cut/Copy) and server API for Move/Copy/Paste operations. | Medium |
| 6 | Keyboard & Context Menus | Global keyboard shortcuts and context menus mapping to implemented operations. | Medium |
| 7 | File Downloads & Previews | Safe file downloading and simple browser-based preview for text/images. | Low |
| 8 | Search & Filter | Recursive search within subfolders and cancelable search UI state. | Medium |
| 9 | ZIP Archive Workflows | Compressing selections and extracting ZIP archives natively via System.IO.Compression. | Medium |
| 10 | Recycle Bin Integration | CsWin32 COM integration for Recycle Bin enumeration, restoration, and permanent delete. | High |

## Phase Order Rationale
- **Phase 1 & 2** establish the foundational architecture, enforcing security (`AuthorizedRoots`) and proving the end-to-end data flow for navigation.
- **Phase 3** solidifies the UI interaction model (selection and views) before complex operations depend on what is selected.
- **Phases 4 through 6** sequentially build out the interaction tools: building the operations, then the clipboard patterns to group operations, and finally binding them to native-feeling shortcuts and menus.
- **Phases 7, 8, 9, 10** add isolated but high-value feature clusters (Downloads, Search, ZIP, Recycle Bin) that rely on the established selection and tree navigation patterns.

## Shared Considerations
- **Pattern:** Use `Path.GetFullPath()` and prefix-matching for strict boundary enforcement across all filesystem APIs.
- **Performance:** Use `ValueTask`, `IAsyncEnumerable`, or pagination when enumerating large directories. Prioritize speed/latency.
- **UI Components:** Use `Radzen.Blazor` components (Tree, DataGrid, ContextMenu) as specified by NFR-1 and Due Diligence.
- **Result Pattern:** Use explicit `Result<T>` records for operations that fail gracefully due to OS-level locking or permission errors.
- **Network Boundaries:** `AllowedIPMiddleware` enforces `10.0.0.x`; do not inadvertently bypass this in endpoint configurations.

## Definition of Done
- [ ] All phases complete
- [ ] All tests passing
- [ ] All code reviews approved
- [ ] All phase reviews complete
- [ ] Final spec review approved
