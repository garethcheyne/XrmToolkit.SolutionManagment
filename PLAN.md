# Plan: Port Engine to React + Slim C# Services

## Context

SolutionManagement.cs is 3000+ lines mixing SDK operations with WinForms dialogs. The goal: React handles ALL UI. C# becomes a set of tiny, focused service files — pure execution code with no forms. Each service hooks into React via the WebView2 bridge.

## Target Architecture

```
err403.SolutionManagment/
├── Plugin.cs                          ← XrmToolBox entry point
├── SolutionManagement.cs              ← SLIM: constructor + connection wiring (~200 lines)
├── SolutionManagement.Designer.cs     ← SLIM: just WebView2 host control
├── Services/                          ← One file per SDK operation (no UI)
│   ├── SolutionTransferService.cs     ← Export + Import + Publish + Polling
│   ├── FlowActivationService.cs       ← SetStateRequest for flows
│   ├── SolutionRemovalService.cs      ← Delete solutions from targets
│   ├── MissingDependencyService.cs    ← RetrieveMissingComponents
│   └── TokenService.cs                ← Extract/refresh auth tokens
├── Forms/
│   └── WebUIHost.cs                   ← WebView2 bridge (only C# form)
├── Types/
│   └── Requests.cs                    ← BaseToProcess, ExportToProcess, etc.
├── AppCode/
│   ├── Settings.cs                    ← Plugin settings
│   ├── Enumerations.cs                ← Component types
│   └── EnvironmentIdResolver.cs       ← Env ID resolution
├── WebUI/                             ← Vite + React + Fluent UI
│   └── src/
│       ├── tabs/                      ← Data grids (fetch via Web API)
│       ├── dialogs/                   ← React modals (replace WinForms dialogs)
│       │   ├── SettingsDialog.tsx
│       │   ├── PreImportSummary.tsx
│       │   ├── UpdateVersionDialog.tsx
│       │   ├── SolutionOrderDialog.tsx
│       │   ├── FlowResultsDialog.tsx
│       │   ├── ImportLogViewer.tsx
│       │   ├── TransferEnvVarSummary.tsx
│       │   └── MissingDepsDialog.tsx
│       ├── components/
│       └── panels/
├── Resources/
│   ├── WebUI.html                     ← Built React app
│   └── Icon.png                       ← Plugin icon
└── Archive/                           ← Old files (reference only)
```

## Principle

**C# Services = pure execution, no UI.** Each service:
- Takes parameters (solution IDs, connection details, settings)
- Calls Dataverse SDK
- Returns results via bridge to React
- No MessageBox, no ShowDialog, no WinForms references

**React = all UI decisions.** React handles:
- Confirmation dialogs before operations
- Settings input forms
- Results display after operations
- Progress visualization
- Error display

## C# Services

### `Services/SolutionTransferService.cs`
Execution: ExportSolutionRequest → ImportSolutionRequest → PublishAllXmlRequest
Input from React: `{ solutions, targets, settings (managed, import mode, etc.) }`
Output to React: progress updates `{ id, status, percentage, elapsed }`, completion `{ success, errors }`
Async polling: Timer-based asyncoperation monitoring stays in C#

### `Services/FlowActivationService.cs`
Execution: SetStateRequest per flow per target
Input from React: `{ flows, targets, activate: bool }`
Output to React: `[{ flowName, target, success, error }]`

### `Services/SolutionRemovalService.cs`
Execution: Delete("solution", id) per solution per target
Input from React: `{ solutions, targets }`
Output to React: `[{ solution, target, success, error }]`

### `Services/MissingDependencyService.cs`
Execution: RetrieveMissingComponentsRequest
Input from React: `{ importJobId }`
Output to React: `[{ requiredComponent, schemaName, solution, dependent }]`

### `Services/TokenService.cs`
Execution: Extract CurrentAccessToken from CrmServiceClient
Input: ConnectionDetail
Output to React: `{ orgUrl, token, environmentId }`

## React Dialogs (replace WinForms modals)

| Dialog | Purpose | Interaction |
|--------|---------|-------------|
| SettingsDialog | Configure import/export options | User fills form → sends settings with transfer request |
| PreImportSummary | Confirm solutions to transfer | Shows list → user confirms → triggers C# service |
| UpdateVersionDialog | Prompt for version number | Input field → sends version with transfer |
| SolutionOrderDialog | Reorder solutions | Drag-drop → sends ordered list |
| FlowResultsDialog | Show activation results | Receives results from C# → displays table |
| ImportLogViewer | Show import errors | Receives error data from C# → 3-tab display |
| TransferEnvVarSummary | Confirm env var transfer | Checkbox list → confirms → React does Web API calls |
| MissingDepsDialog | Show missing components | Receives data from C# service → displays table |

## Bridge Protocol

### React → C# (SDK operations only)
```typescript
{ action: 'startTransfer', solutions, settings }
{ action: 'activateFlows', flows, targets }
{ action: 'deactivateFlows', flows, targets }
{ action: 'removeSolutions', solutions, targets }
{ action: 'findMissingDeps', importJobId }
{ action: 'exportToFile', solutions }
{ action: 'importFromFile' }
{ action: 'refreshToken' }
{ action: 'addTarget' }
{ action: 'removeTarget', connectionName }
```

### C# → React (results + progress)
```typescript
window.bridge.transferProgress({ id, status, percentage, elapsed })
window.bridge.transferComplete({ success, errors, importJobId })
window.bridge.flowResults([{ flowName, target, success, error }])
window.bridge.missingDeps([{ component, schemaName, solution }])
window.bridge.exportComplete({ filePath })
```

## Files to Delete (move to Archive)
- All Forms/ except WebUIHost.cs
- MissingComponentsControl.* and MissingComponentsForm.*
- SolutionOrderDialog.*
- All Resources/ PNGs except Icon.png and WebUI.html
- Types/ — FlowTypes.cs, EnvVarTypes.cs, SettingsTypes.cs, ListViewItemComparer.cs, ProgressItem.*
- AppCode/ — DownloadLogEventArgs.cs, TargetOrganizationsEventArgs.cs, VersionTypeConverter.cs, ConnectionReferenceInfo.cs, SolutionHelper.cs

## Implementation Order

### Step 1: Services/ + TokenService
### Step 2: FlowActivationService + React FlowResultsDialog
### Step 3: SolutionRemovalService
### Step 4: SolutionTransferService + React dialogs (SettingsDialog, PreImportSummary, etc.)
### Step 5: MissingDependencyService + React MissingDepsDialog
### Step 6: Slim SolutionManagement.cs to ~200 lines
### Step 7: Delete old files, clean Resources/

## Verification
1. `cd WebUI && npm run build` + Build in VS
2. Transfer solution → React dialogs for config/confirm → C# executes → React shows progress
3. Activate flows → C# SetState → React shows results
4. Export → C# saves ZIP → React shows completion
5. All old WinForms dialogs gone

---

## Status — April 2026

### ✅ Completed (this port)
- All 5 wiring/implementation gaps fixed (activeImportsDetected, ImportFromFile async, RemoveFromTargets feedback, FindMissingDeps full implementation, version-skip bug)
- TeachingPopovers on all key settings with MS Learn links
- MissingDepsDialog created and wired
- React builds: 2452 modules, 1,165 KB, 3.3s
- C# builds: Exit 0 via VS MSBuild

### ✅ Test Infrastructure Added
- Vitest 3 + jsdom for React (39 tests)
- xUnit 2.9 for C# / net48 (37 tests)
- Combined PDF+Markdown report at `/reports/` via `npm run report:test`
- 76/76 tests passing

### ⬜ Remaining
- React component render tests (TransferConfirmDialog, MissingDepsDialog)
- Additional C# service tests (MissingDependencyService, SolutionRemovalService)
- CI pipeline test step in azure-pipelines.yml
- Env var save deduplication helper
- Slim SolutionManagement.cs further
