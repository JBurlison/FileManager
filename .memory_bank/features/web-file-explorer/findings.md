# Due Diligence Findings: Web File Explorer

## Date
2026-04-05

## Codebase Analysis

### Affected Files & Modules
| File/Module | Impact | Notes |
|-------------|--------|-------|
| WebFileExplorer.Server/Program.cs | Modified | Needs API controllers and file system service registrations |
| WebFileExplorer.Server/appsettings.json | Modified | Add AuthorizedRoots array and PreviewPolicy settings |
| WebFileExplorer.Client/wwwroot/index.html | Modified | Include/verify Radzen theme and Explorer CSS layout |
| WebFileExplorer.Client/Pages/Home.razor | Replaced | Replace placeholder with the main Explorer shell layout component |
| WebFileExplorer.Shared/ | Modified | Require new DTOs for Filesystem entries (IDirectoryItem, DriveInfo, etc.) |

### Existing Patterns Identified
- Pattern 1: **Network Binding Restrictions.** Handled by AllowedIPMiddleware and NetworkBindingExtensions. It enforces 10.0.0.x in production (and allows loopback only in Development). Any new endpoints will automatically be protected by this middleware.
- Pattern 2: **Hosted Blazor WebAssembly with Radzen.** The UI is WASM using Radzen.Blazor. Radzen components (like RadzenDataGrid or RadzenTree) should be used to support high-performance virtualization (critical NFR).

### Integration Points
- **ASP.NET API Controllers:** The Blazor WASM client must call REST/endpoints to enumerate and mutate the host file system.
- **System.IO & System.IO.Compression:** Direct BCL classes at the Server layer for executing shell operations.
- **Windows API / COM:** Required on the server for Recycle Bin support.

## Risk Assessment

### High Risk
- **Risk:** Path Traversal and Command Injection
  - **Impact:** Malicious clients on the 10.0.0.x network could break out of authorized directories to access or delete critical system files (e.g., C:\Windows\System32).
  - **Mitigation:** Implement strict path normalization using Path.GetFullPath() and enforce an exact prefix match against configured AuthorizedRoots (validating directory boundaries). Reject requests containing relative traversals (..).
- **Risk:** High Latency on Large Directories (1000+ files)
  - **Impact:** Client UI freezing, failing performance NFR-1 (300 ms initial render).
  - **Mitigation:** Use RadzenDataGrid with UI virtualization. Map the server response to lightweight DTOs instead of full FileInfo trees. Use server-side pagination if needed.

### Medium Risk
- **Risk:** File Locking and OS Permission Errors
  - **Impact:** Windows frequently locks files/folders or restricts access via ACLs (UnauthorizedAccessException). Unhandled exceptions crash the request.
  - **Mitigation:** Wrap System.IO routines securely. Translate all IO exceptions into structured HTTP 403 / 409 error responses with actionable UI text.
- **Risk:** Drag-and-drop Semantics Misalignment
  - **Impact:** Feature creep or unpredictable behavior if "drag and drop" implies uploading from client OS vs intra-UI moving.
  - **Mitigation:** Distinguish internal HTML5 drag-and-drop operations from external file drops. Document browser-to-host file uploads as an explicit Non-Goal for Phase 1.

### Low Risk
- **Risk:** Large ZIP Archive Handling Memory/Timeout
  - **Impact:** Generating/extracting large ZIPs might exhaust ASP.NET thread pool or exceed HTTP timeout limits.
  - **Mitigation:** For large operations, consider background task execution, or stream directly to the response over HTTP instead of buffering in memory.

## Dependency Analysis

### NuGet Packages
| Package | Current Version | Action Needed |
|---------|----------------|---------------|
| Radzen.Blazor | 10.2.0 | Present in Client; utilize for trees/grids/context menus |
| System.IO.Compression | Built-in | Used for ZIP flows |
| Microsoft.Windows.CsWin32 | N/A | Add to Server project for Windows Shell/Recycle Bin COM APIs |

### External Services
- None. Explicitly forbidden by NFR-2.

## Spec Gaps Found
- [x] **Gap 1: Ambiguity on standard file uploads.** The spec extensively mentions "drag-and-drop" representing internal move semantics but doesn't clarify File Uploads from the client's desktop.
  - **Resolution:** Added to Non-Goals: "Uploading files from a remote client machine's filesystem to the server is out of scope for the core shell features."
- [x] **Gap 2: Path boundary implementation architecture.** Path validation is mentioned, but the configuration mechanism is missing.
  - **Resolution:** Added NFR and FR constraint for an AuthorizedRoots configuration array to explicitly define exactly which roots are permitted.
- [x] **Gap 3: Open Questions in Spec.** Four open questions remain in the spec regarding browser scope, initial roots, quick access, and default file open behavior.
  - **Resolution:** Answered them directly and integrated into the Acceptance Criteria.

## Assumptions
- The application executes under a host identity (e.g., standard local user) that provides natural OS-level permission restrictions. It cannot override OS constraints.
- "Explorer-parity" aims to address Chromium-based browsers (Edge/Chrome) for predictable hotkey fidelity.

## Recommendations
1. Configure specific AuthorizedRoots allowlists (e.g. ["D:\Shares", "C:\Data"]) instead of automatically exposing C:\ to limit the blast radius.
2. Delay "Quick Access" pinning to a future milestone to keep initial implementation tightly focused on tree navigation predictability.
3. Establish default policy for opened files as 'Browser Download' or 'Browser Preview'. Host execution must be disabled or require explicit right-click action due to remote-trigger safety concerns.

## Spec Updates Made
- Removed "Open Questions" section entirely and integrated the answers into the Acceptance Criteria and NFRs.
- Clarified that cross-network Client-to-Server file "uploading" is Out-Of-Scope.
- Mandated Path.GetFullPath matching in NFR-2.
- Updated status to "Approved".
