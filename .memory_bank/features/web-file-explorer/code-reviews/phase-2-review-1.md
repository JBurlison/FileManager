# Code Review: Phase 2 - Directory Browsing (Review #1)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The phase implements the directory browsing correctly overall but has a few serious UI typos and cross-platform path handling issues between the Blazor WebAssembly frontend (Unix OS context) and a .NET Server backend (Windows OS context). Furthermore, directory enumeration needs robustness against individual inaccessible system files. 

## Findings

### Critical
- **File:** [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor#L189)
  - **Issue:** Splitting backslash strings (`C:\...`) on the Blazor WebAssembly frontend using `System.IO.Path.DirectorySeparatorChar` will fail. Wasm operates as a Unix environment where `Path.DirectorySeparatorChar` is `/`. This will prevent breadcrumbs from displaying correctly for Windows paths.
  - **Fix:** Explicitly split paths by explicitly providing both slash variants: `new[] { '\\', '/' }` to correctly robustify platform-agnostic tokenization.

### Major
- **File:** [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor#L9)
  - **Issue:** Typo: `Idon` is used instead of `Icon` on `RadzenButton` and `RadzenIcon` components (`Idon="arrow_upward"`, `Idon="keyboard_return"`, `Idon="@(...)"`), breaking the icon UI rendering.
  - **Fix:** Replace all instances of `Idon` with `Icon`.
- **File:** [src/WebFileExplorer.Server/Services/FileSystemProvider.cs](src/WebFileExplorer.Server/Services/FileSystemProvider.cs#L125)
  - **Issue:** Using `EnumerateDirectories` and `EnumerateFiles` without options will throw `UnauthorizedAccessException` parsing the directory if even a single file or child folder denies access (e.g. system files). This fails the layout for the entire directory instead of skipping the bad files.
  - **Fix:** Pass `new EnumerationOptions { IgnoreInaccessible = true }` into both enumeration methods.
- **File:** [src/WebFileExplorer.Server/Controllers/FileExplorerController.cs](src/WebFileExplorer.Server/Controllers/FileExplorerController.cs#L32)
  - **Issue:** Endpoint is routed as `api/fileexplorer/directories` but the phase task 2.2 explicitly requires `api/fileexplorer/list`. 
  - **Fix:** Update the route attribute to `[HttpGet("list")]` to match requirements, and ensure [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor#L159) connects to the correct endpoint.

### Minor
- **File:** [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor#L120)
  - **Issue:** `System.IO.Directory.GetParent` is used in a WebAssembly client, passing in a Windows string path. While it shouldn't touch the Wasm disk, its path parsing might behave unexpectedly for mixed OS scenarios (e.g., getting the parent of `C:\Windows`). 
  - **Fix:** Consider extracting the parent path via explicit string parsing utilizing `\\` and `/`.

### Nits
- **File:** [src/WebFileExplorer.Server/Services/FileSystemProvider.cs](src/WebFileExplorer.Server/Services/FileSystemProvider.cs#L45)
  - **Issue:** `EnsureTrailingSlash` allocations inside a hot check loop.
  - **Fix:** While not critical, if latency is a strict priority, you could rewrite validation to use `AsSpan()` / `MemoryExtensions.StartsWith` or a regex, but this is optional since `AuthorizedRoots` is typically small.

## Positive Notes
- Very good handling of `UnauthorizedAccessException` and `DirectoryNotFoundException` overall.
- Deferred execution properly constructed until the directory layout is rendered to `.ToList()`.
- Good path escaping handling via `Uri.EscapeDataString` on the frontend API calls.

## Changes Required
1. Fix `Idon` -> `Icon` properties in [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor).
2. Fix Wasm split behavior in breadcrumbs by updating [src/WebFileExplorer.Client/Pages/Home.razor](src/WebFileExplorer.Client/Pages/Home.razor#L189) with explicit `'\\'` character splitting.
3. Protect server directory loops in [src/WebFileExplorer.Server/Services/FileSystemProvider.cs](src/WebFileExplorer.Server/Services/FileSystemProvider.cs#L125) with `EnumerationOptions { IgnoreInaccessible = true }`.
4. Update list path endpoint matching task 2.2 requirements in [src/WebFileExplorer.Server/Controllers/FileExplorerController.cs](src/WebFileExplorer.Server/Controllers/FileExplorerController.cs#L32) and client.