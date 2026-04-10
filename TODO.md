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

- [ ] **Env var save duplication** — Edit-panel save and bulk transfer have near-identical query+upsert logic; extract shared helper
- [x] **MissingComponentsForm opens modelessly** — User can interact with main plugin while it holds a potentially stale `_sourceService`
- [x] **"Fix" button silently skips** unresolvable component types with no user feedback
- [x] **`solutionUrlBase` can be null** — Some connection types (on-prem without IFD) return null `WebApplicationUrl`
- [x] **Publish skipped for managed imports** — Setting says "Publish Customizations" but silently does nothing for managed

## Low

- [x] **`settings` null on close without connecting** — `ClosingPlugin` calls `settings.Save()` which throws if user never connected
- [x] **`SetColors()` version comparison includes prefix** — `(M)`/`(U)` prefix means target vs source never matches after target removal
- [x] **Toast notification PNGs** written to `%TEMP%` every construction, never cleaned up
