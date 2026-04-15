# TODO — Solution Management (err403)

## Critical

- [x] **BUG: Enter-key URL broken** — `MainForm.cs` `lstSourceSolutions_KeyDown` uses `Entity.ToString()` instead of `.Id` for the solution URL
- [x] **BUG: Timer fires on ThreadPool thread** — `System.Timers.Timer` in `SolutionTransferTool.cs` causes race conditions on `cancelPending`, `toProcessList`, `progressItems`. Switch to `System.Windows.Forms.Timer` or set `SynchronizingObject`
- [x] **BUG: Timer never disposed** — Closing the plugin mid-transfer leaves the timer firing on a disposed control → `ObjectDisposedException`
- [x] **BUG: Crash on reorder with no selection** — `SolutionOrderDialog.cs` and `PreImportSummaryForm.cs` access `SelectedItems[0]` without checking count
- [x] **BUG: NullReferenceException in MissingComponentsForm** — Deep XML child-node traversal with zero null-checks

## High

- [x] **Memory leak: DockContent forms never disposed** — `mForm`, `evForm`, `cfForm`, `pForm`, `evEditPanel`, `sForm` are never disposed on plugin close
- [x] **Memory leak: SaveFileDialog not disposed** — `ProgressItem.cs` creates `new SaveFileDialog` without `using`
- [x] **Infinite loop risk** — Date-based version increment `while (newVersion <= version)` has no max-iteration guard
- [x] **CSV injection** — `MissingComponentsControl.cs` writes user data to CSV without escaping formula-injection characters

## Medium — Dead Code

- [x] **Delete `EditEnvironmentVariableForm`** — Never instantiated, replaced by `EnvVarEditPanel`
- [x] **Delete `TargetOrganizations.cs`** — `SolutionInfo`, `TargetOrganization`, `TransfertSettings` are never referenced
- [x] **Delete unused `SolutionHelper.CheckForNewConnectionReferences` overload** — The `(string, IOrganizationService, IOrganizationService)` signature is never called
- [x] **Delete `UpdateVersionEnumConverter`** — Exact duplicate of `VersionTypeConverter`

## Medium — Logic / UX

- [x] **Version skip ignored** — `SolutionActionItem` was missing `NewVersion` property; JSON deserialisation silently dropped the value; fixed
- [x] **`activeImportsDetected` silent failure** — C# called `window.bridge.activeImportsDetected()` but the method was absent from `bridgeApi`; fixed
- [x] **`CfForm_ImportFromFileRequested` synchronous** — blocking, no progress, ignored settings; replaced with async+polling
- [x] **`CfForm_RemoveFromTargetsRequested` no feedback** — silently worked or failed; now shows progress and refreshes
- [x] **`CfForm_FindMissingDepsRequested` TODO stub** — unimplemented; now fully wired end-to-end
- [x] **Missing Dependencies check** — "Missing Deps" toolbar button now calls `RetrieveMissingComponents` per target and shows grouped results dialog
- [ ] **Env var save duplication** — Edit-panel save and bulk transfer have near-identical query+upsert logic; extract shared helper
- [x] **MissingComponentsForm opens modelessly** — User can interact with main plugin while it holds a potentially stale `_sourceService`
- [x] **"Fix" button silently skips** unresolvable component types with no user feedback
- [x] **`solutionUrlBase` can be null** — Some connection types (on-prem without IFD) return null `WebApplicationUrl`
- [x] **Publish skipped for managed imports** — Setting says "Publish Customizations" but silently does nothing for managed

## Low

- [x] **`settings` null on close without connecting** — `ClosingPlugin` calls `settings.Save()` which throws if user never connected
- [x] **`SetColors()` version comparison includes prefix** — `(M)`/`(U)` prefix means target vs source never matches after target removal
- [x] **Toast notification PNGs** written to `%TEMP%` every construction, never cleaned up

## Testing

- [x] **Vitest setup** — `npm run test` runs 39 TypeScript unit tests (bumpVersion, parseError)
- [x] **xUnit setup** — `dotnet test` runs 37 C# unit tests (BumpVersion, ParseImportErrorMessage, GetComponentTypeName)
- [x] **Combined PDF report** — `npm run report:test` generates branded A4 PDF + Markdown at `/reports/`
- [ ] **React component tests** — TransferConfirmDialog, MissingDepsDialog render tests
- [ ] **Additional C# tests** — `SaveSolutionToDisk`, `UpdateSolutionVersion`, `MissingDependencyService`
- [ ] **CI pipeline** — add test step to `azure-pipelines.yml`

## Compiler Warnings

- [x] **CS0649: `lastTargetService` never assigned** — Now set from `itp.Detail.GetCrmServiceClient()` on import success (was a real bug: "Find Missing Dependencies" always passed null)
- [x] **CS0649: `lastConnectionName` never assigned** — Now set from `itp.Detail.ConnectionName` on import success
- [x] **CS0067: `TargetOrganizationRequested` never used** — Removed dead event from MainForm + subscription + handler

## Progress Tracking Stale Issue

- [x] **Re-entrancy guard** — Added `isPolling` flag so overlapping timer ticks don't spawn pile-ups of `WorkAsync` calls
- [x] **AsyncOperationId race condition** — Export and import polling now skip when `AsyncOperationId == Guid.Empty` (before WorkAsync has set it)
- [x] **Silent error swallowing** — All polling callbacks now check `evt.Error` before processing `evt.Result`
- [x] **Premature timer.Stop()** — Removed 5 places where a single item failure killed the entire timer; only stops at final `toProcessList.All(IsProcessed)` check
- [x] **Publish failed-import path** — Now marks publish as `IsProcessed` instead of just stopping the timer
- [x] **Refresh Status button** — New toolbar button visible during transfers to manually re-kick progress checking

## View Message / Import Log

- [x] **View Message dialog** — New error-mode form with 3 tabs: Message (plain text), Missing Dependencies (parsed ListView), Raw Content
- [x] **Missing Dependencies parsing** — Parses `<MissingDependency>` XML into columns: Required Component, Schema Name, Solution, Dependent, Resolvable
- [x] **Async error message capture** — `asyncoperation.message` stored on `ProgressItem.AsyncErrorMessage` on import failure
- [x] **Download log file split** — Now saves `_message.txt` (text portion) and `_message.xml` (pretty-printed XML) alongside the SDK log
