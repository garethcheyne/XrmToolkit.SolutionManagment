Solution Management (err403) — Changelog
==========================================

## [Unreleased]

### Added
- **Test infrastructure** — Full unit test suite across both React/TypeScript and C# layers:
  - Vitest 3 with jsdom environment for React (`npm run test`)
  - xUnit 2.9 for C# targeting net48 (`dotnet test`)
  - Combined report generator (`npm run report:test`) producing branded A4 PDF + Markdown at `/reports/`  
  - Tests cover: `bumpVersion` (all 6 policies, edge cases, date counter), `parseError` (multi-dep, optional fields, null returns), `ParseImportErrorMessage` (XML extraction, prefix stripping, type code mapping), `GetComponentTypeName` (all known type codes)
  - 39 TypeScript tests + 37 C# tests = 76 total
- **Missing Dependencies check** — "Missing Deps" toolbar button checks selected solutions against all connected targets using the `RetrieveMissingComponents` Dataverse message; results shown in grouped dialog by target → solution → dependency cards with MS Learn links
- **Version skip fix** — "Skip version update" in the Transfer Confirm dialog now correctly passes `newVersion: undefined` to C#; `SolutionActionItem` deserialises the field and only bumps when a value is present
- **Teaching popovers** — Inline guidance via Fluent UI `TeachingPopover`/`Popover` throughout:
  - Import Mode (Update / Upgrade / Stage for Upgrade) with MS Learn link
  - Check Dependencies, Overwrite Unmanaged, Convert to Managed, Skip Product Dependencies
  - Both Settings panel and Transfer Confirm dialog
- **Import from file async** — `CfForm_ImportFromFileRequested` is now fully async with progress polling and plugin settings applied (was previously synchronous)
- **Remove from targets feedback** — Shows progress when removing solutions from targets and refreshes the target solution list on success
- **`activeImportsDetected` bridge fix** — C# was calling `window.bridge.activeImportsDetected()` but the function was missing from the `bridgeApi` object; added

### Previously unreleased (now captured)

### Added
- **Refresh Status button** — toolbar button visible during transfers to manually re-kick progress polling when progress appears stale
- **View Message dialog** — "View message" link on import progress items opens a modal with 3 tabs:
  - Message tab: plain-text error message
  - Missing Dependencies tab: parsed `<MissingDependency>` XML into a sortable ListView (Required Component, Schema Name, Solution, Dependent, Resolvable)
  - Raw Content tab: full message with pretty-printed XML
- **Async error message capture** — import failures now store the `asyncoperation.message` field for immediate viewing without a server round-trip
- **Download log split** — "Download log file" now saves up to 3 files: SDK formatted log (`.xml`), error message text (`_message.txt`), and parsed error XML (`_message.xml`)

### Fixed
- **Progress tracking stale during large imports** — 5 root causes resolved:
  - Added re-entrancy guard (`isPolling` flag) preventing overlapping timer tick pile-ups
  - Guarded `AsyncOperationId` race condition — polling skips when ID not yet assigned
  - Added `evt.Error` null-checks in all 3 polling callbacks (export async, import async, importjob) — previously silently swallowed network errors
  - Removed 5 premature `timer.Stop()` calls where a single failure killed polling for all items
  - Publish failed-import path now marks publish as processed instead of just stopping the timer
- **CS0649: `lastTargetService` / `lastConnectionName` never assigned** — now set on import success; fixes "Find Missing Dependencies" always passing null
- **CS0067: `TargetOrganizationRequested` event never raised** — removed dead event, subscription, and handler from MainForm

---

## [1.2026.4.10] — 2026-04-10

### Added
- **Cloud Flows Tab** — Cloud flow management across environments
  - List all cloud flows from source (category 5, type 1 definitions)
  - Display flow name, type, status, solution, owner, and modified date
  - Colour-coded status cells (green = On, grey = Off, yellow = Suspended)
  - Filter by active-only toggle, solution dropdown, and free-text search
  - Compare flow activation status on connected target environments
  - Activate or deactivate selected flows on targets in bulk
  - Results dialog with success/failure summary and colour-coded rows
  - "Open Flow in Browser" button to navigate to a flow in the target environment
  - Connection reference error detection with highlighted rows
- **Environment Variables Tab** — Full environment variable management
  - View all environment variables from source with current values
  - Compare source and target environment variable values side by side
  - Edit individual environment variable values (double-click)
  - JSON pretty-printing and validation for JSON-type variables
  - Number and Boolean validation on edit
  - Bulk transfer selected environment variables to target environments
  - Transfer confirmation dialog with per-variable checkboxes
  - Toggle visibility of Schema Name and Default Value columns
  - Column sorting and tooltip support
- **Managed/Unmanaged safety warnings** on solution transfer
  - Warns when importing managed over existing unmanaged
  - Warns when importing unmanaged over existing managed

### Changed
- Docked tab layout using WeifenLuo DockPanel Suite
- Context-sensitive toolbar: buttons swap between Solutions,
  Environment Variables, and Cloud Flows tabs
- Source/Target environment bar shared across all tabs
- Updated plugin icon
- About dialog with embedded changelog
- Renamed from DamSim.SolutionTransferTool to err403.SolutionManagment
- Updated NuGet package metadata and assembly info

### Fixed
- NullReferenceException on plugin load
- Index out of range when displaying target environment values

### Performance
- Lazy loading of environment variables (only fetched on first tab visit)

---

## Notes

### Solution Version Prefixes
When viewing target environment solution versions, each version is prefixed
with a letter indicating how the solution is installed on that target:

- **(M)** — Managed: the solution was imported as a managed solution
- **(U)** — Unmanaged: the solution was imported as an unmanaged solution
