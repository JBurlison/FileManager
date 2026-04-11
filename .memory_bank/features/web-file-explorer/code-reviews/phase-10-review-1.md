# Code Review: Phase 10 - Recycle Bin Integration (Review #1)

## Date
2026-04-05

## Result: CHANGES NEEDED

## Summary
The phase implementation is incomplete and blocked on critical findings. The `RecycleBinService` only contains mocked methods and does not utilize the requested `Microsoft.Windows.CsWin32` shell COM API equivalents as dictated by the phase requirements. There is no integration with the DI container (`Program.cs`), `FileExplorerController`, or the UI (`Home.razor`). The test coverage is also using basic `Assert.IsTrue` stubs rather than executing actual implementation.

## Findings

### Critical
- **File:** `src/WebFileExplorer.Server/Services/RecycleBinService.cs` (Line 8)
  - **Issue:** The service relies entirely on mock methods (`GetDeletedItems`, `RestoreItem`, `EmptyBin`) returning hardcoded values. `SHFileOperation` or the necessary CsWin32 interops are never actually utilized for real Windows Recycle Bin tracking or deletions.
  - **Fix:** Implement proper calls to the Windows Shell API using the interops generated from `NativeMethods.txt` to fetch deleted items, empty the bin, and restore files.
- **File:** `src/WebFileExplorer.Server/Program.cs`
  - **Issue:** `RecycleBinService` is not added to the dependency injection container.
  - **Fix:** Register `RecycleBinService` as a transient or singleton service (e.g. `builder.Services.AddTransient<RecycleBinService>();`).
- **File:** `src/WebFileExplorer.Server/Controllers/FileExplorerController.cs`
  - **Issue:** No API endpoints are exposed for the UI to interact with the Recycle Bin (restore, empty, list deleted items). The `Delete` method does not appear to have been modified to send items to the bin versus permanently deleting them.
  - **Fix:** Add routes for Recycle Bin interactions and update the existing `Delete` method to utilize the new recycle bin service when appropriate based on user intent.
- **File:** `src/WebFileExplorer.Client/Pages/Home.razor`
  - **Issue:** The acceptance criteria demand a virtual recycle bin node in the Navigation Tree, customized `DataGrid` columns (Original Path, Deletion Time), and UI buttons for "Restore" or "Empty Bin". None of this logic is present.
  - **Fix:** Inject the virtual root node into `_roots`, modify the DataGrid to display the required recycle bin properties conditionally, and wire up the context menu/toolbar actions to backend API calls.

### Major
- **File:** `src/WebFileExplorer.Tests/Unit/Services/RecycleBinServiceTests.cs` (Line 16)
  - **Issue:** The tests consist entirely of mock assertions (`Assert.IsTrue(true)`). 
  - **Fix:** Write valid tests that verify the Recycle Bin logic (even if isolated or using abstraction/mocks at the API boundaries), ensuring actual code paths are run instead of stubs.

### Minor
- **File:** `src/WebFileExplorer.Server/Services/RecycleBinService.cs` (Line 8)
  - **Issue:** The class is missing an interface (e.g., `IRecycleBinService`), which prevents it from being mocked out easily in other dependent classes like the `FileExplorerController`.
  - **Fix:** Extract an `IRecycleBinService` interface and implement it.

## Positive Notes
- Added required COM object and Method definitions to `NativeMethods.txt`.
- Correctly updated `WebFileExplorer.Server.csproj` with the necessary `Microsoft.Windows.CsWin32` configuration and package references.

## Changes Required
1. Actually implement the Windows Shell logic in `RecycleBinService.cs` instead of returning mock values.
2. Wire up DI in `Program.cs` for the newly created service.
3. Create API endpoints corresponding to Recycle Bin operations within `FileExplorerController.cs`.
4. Update standard delete handling to send eligible items to the Recycle Bin.
5. Provide the required virtual navigation node, data grid adjustments, and toolbar/context menu UI changes in `Home.razor`.
6. Implement authentic tests instead of dummy assertions in `RecycleBinServiceTests.cs`.
