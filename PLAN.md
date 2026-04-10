# Plan: Migrate Plugin UI to Vite + React + Fluent UI v9

## Context

The XrmToolBox plugin currently uses WinForms controls (ListView, GroupBox, ToolStrip) for all tabs. We've proven that WebView2 works by shipping the Cloud Flows tab with a raw HTML/CSS/JS frontend — it renders beautifully inside XrmToolBox (confirmed by screenshot). 

Now we want to replace the raw HTML with a proper Vite + React + Fluent UI v9 app that covers **all tabs**, giving the plugin a modern Microsoft-native look (same design system as Power Platform, Azure Portal, M365).

## Architecture

```
err403.SolutionManagment/
├── WebUI/                              ← Vite + React project
│   ├── src/
│   │   ├── main.tsx                    ← App entry, FluentProvider
│   │   ├── App.tsx                     ← Tab router (Solutions, EnvVars, Flows, Settings)
│   │   ├── bridge.ts                   ← Type-safe C# ↔ JS message bridge
│   │   ├── types.ts                    ← Shared TypeScript types matching C# DTOs
│   │   ├── theme.ts                    ← Fluent UI theme customisation
│   │   ├── tabs/
│   │   │   ├── SolutionsTab.tsx        ← Solutions grid + target org columns
│   │   │   ├── EnvironmentVarsTab.tsx  ← Env var grid + inline edit panel
│   │   │   ├── CloudFlowsTab.tsx       ← Cloud flows grid (replace current HTML)
│   │   │   └── PlatformSettingsTab.tsx ← Org settings comparison grid
│   │   ├── panels/
│   │   │   ├── ProgressPanel.tsx       ← Right-dock progress items
│   │   │   ├── EnvVarEditPanel.tsx     ← Right-dock variable editor
│   │   │   ├── FlowActionPanel.tsx     ← Right-dock flow activate/deactivate
│   │   │   └── SettingsPanel.tsx       ← Right-dock plugin settings
│   │   └── components/
│   │       ├── StatusPill.tsx          ← Reusable On/Off/Suspended badge
│   │       ├── TargetColumn.tsx        ← Reusable target org column renderer
│   │       └── SearchToolbar.tsx       ← Reusable search + filter bar
│   ├── package.json
│   ├── tsconfig.json
│   ├── vite.config.ts                  ← Single-file output config
│   └── index.html
├── Resources/
│   └── WebUI.html                      ← Vite build output (single file, embedded)
├── Forms/
│   ├── WebUIHost.cs                    ← Single WebView2 DockContent (replaces all tab forms)
│   ├── CloudFlowsForm.cs              ← KEPT (fallback / reference)
│   ├── CloudFlowsWebForm.cs           ← REMOVED (replaced by unified WebUIHost)
│   └── ...                             ← Other WinForms forms kept for modal dialogs
└── SolutionTransferTool.cs             ← Rewired to use WebUIHost
```

## Key Design Decisions

### 1. Single WebView2 host, multiple React tabs
Instead of one WebView2 per tab (wasteful), use ONE `WebUIHost.cs` DockContent with a single WebView2 control. React handles tab switching internally using Fluent UI's `<TabList>`. The C# side tells React which tab to show via `ExecuteScriptAsync`.

This means we go from 4 DockContent tab forms + 3 right-panel forms → 1 WebView2 DockContent that fills the entire document area.

### 2. Right panels rendered inside the same WebView2
The env var edit panel, flow action panel, and settings panel become React sidebars within the same WebView2 — no separate WinForms dock panels needed. This gives us seamless animations and consistent styling.

### 3. Progress panel stays as WinForms (Phase 1)
The ProgressForm + ProgressItem controls manage real-time polling state with tight C# integration (timer ticks, async operation tracking, download links). Migrating this has high risk for low visual payoff. Keep it as a WinForms DockRight panel for now.

### 4. Modal dialogs stay as WinForms
PreImportSummaryForm, TransferEnvVarSummaryForm, FlowResultsForm, ImportLogViewerForm, UpdateVersionForm, AboutForm, MissingComponentsForm — these are all modal and work fine as WinForms. No migration needed.

### 5. Source/Target org bar stays as WinForms
The source label + target org buttons + Add button at the top of the plugin stay as WinForms controls in SolutionTransferTool.Designer.cs. They're simple and sit above the WebView2 area.

## C# ↔ JS Bridge Protocol

### C# → JS (via ExecuteScriptAsync)
```typescript
// Tab navigation
window.bridge.setActiveTab('solutions' | 'envvars' | 'flows' | 'settings')

// Solutions tab
window.bridge.loadSolutions(json: SolutionData[])
window.bridge.addTargetSolutionColumn(connectionName: string)
window.bridge.setTargetSolutions(connectionName: string, json: TargetSolution[])
window.bridge.removeTargetColumn(connectionName: string)  // all tabs
window.bridge.updateSolutionVersion(uniqueName: string, newVersion: string)

// Environment Variables tab
window.bridge.loadEnvVars(json: EnvVarData[])
window.bridge.setTargetEnvVarValues(connectionName: string, json: TargetEnvVar[])

// Cloud Flows tab (same as current)
window.bridge.loadFlows(json: FlowData[])
window.bridge.setTargetFlowStatus(connectionName: string, json: TargetFlow[])
window.bridge.updateFlowCellStatus(connectionName, flowName, status, isMatch, isError)

// Platform Settings tab
window.bridge.loadSettings(json: SettingData[])
window.bridge.setTargetSettingValues(connectionName: string, json: TargetSetting[])

// Right panels
window.bridge.loadEnvVarEditor(json: EnvVarEditData)
window.bridge.loadFlowActions(json: FlowActionData)
window.bridge.setFlowActionResult(connectionName, success, message)
window.bridge.loadPluginSettings(json: PluginSettingsData)
```

### JS → C# (via postMessage)
```typescript
// All actions go through a single typed message:
{ action: string, ...payload }

// Solutions
{ action: 'loadSolutions' }
{ action: 'transferSolutions', solutions: [...] }
{ action: 'exportSolutions', solutions: [...] }
{ action: 'switchOrgs' }
{ action: 'findMissingDeps' }
{ action: 'openSolutionInMaker', solutionId: string }

// Environment Variables
{ action: 'refreshEnvVars' }
{ action: 'editEnvVar', schemaName, displayName, typeName, sourceValue }
{ action: 'transferEnvVars', items: [...] }
{ action: 'saveEnvVar', changedValues: { connectionName: newValue } }

// Cloud Flows
{ action: 'refreshFlows' }
{ action: 'activateFlows', flows: [...] }
{ action: 'deactivateFlows', flows: [...] }
{ action: 'openFlowInMaker', flowIds: [...], connectionName }

// Platform Settings
{ action: 'refreshSettings' }
{ action: 'syncSettings', items: [...], selectedOnly: boolean }

// Tab change
{ action: 'tabChanged', tab: string }
```

## Fluent UI Components to Use

| Current WinForms | Fluent UI v9 Replacement |
|-----------------|------------------------|
| ListView | `<DataGrid>` (from @fluentui/react-table) or `<Table>` |
| TextBox (search) | `<SearchBox>` |
| ComboBox (filter) | `<Dropdown>` |
| CheckBox (toggle) | `<Switch>` |
| TabControl | `<TabList>` + `<Tab>` |
| PropertyGrid | Custom form with `<Field>` + `<Input>` / `<Switch>` / `<Dropdown>` |
| ToolStrip buttons | `<Toolbar>` + `<ToolbarButton>` |
| StatusPill (custom) | `<Badge>` |
| GroupBox | `<Card>` |
| ContextMenu | `<Menu>` + `<MenuTrigger>` |

## NPM Dependencies

```json
{
  "dependencies": {
    "@fluentui/react-components": "^9.x",
    "@fluentui/react-icons": "^2.x",
    "@fluentui/react-table": "^9.x",
    "react": "^18.x",
    "react-dom": "^18.x"
  },
  "devDependencies": {
    "@vitejs/plugin-react": "^4.x",
    "vite": "^6.x",
    "vite-plugin-singlefile": "^2.x",
    "typescript": "^5.x",
    "@types/react": "^18.x",
    "@types/react-dom": "^18.x"
  }
}
```

## C# Changes

### New: `WebUIHost.cs`
Single DockContent form containing WebView2. Replaces MainForm, EnvironmentVariablesForm, CloudFlowsWebForm, OrgSettingsForm as tab hosts. Exposes typed methods matching the bridge protocol above.

### Modified: `SolutionTransferTool.cs`
- Replace `mForm`, `evForm`, `cfForm`, `osForm`, `evEditPanel`, `flowActionPanel`, `sForm` with single `webUI` field
- Keep `pForm` (ProgressForm) as separate WinForms DockRight
- Route all existing events through `webUI.OnMessage` handler
- Toolbar button visibility managed by listening to `tabChanged` messages

### Removed:
- `CloudFlowsWebForm.cs` (replaced by unified host)
- `Resources/CloudFlows.html` (replaced by Vite output)

### Kept as-is:
- All modal dialog forms
- ProgressForm + ProgressItem
- Source/Target org bar in SolutionTransferTool.Designer.cs

## Implementation Phases

### Phase 1: Scaffold + Cloud Flows (validate the stack)
1. Create `WebUI/` Vite project with React + Fluent UI v9
2. Build CloudFlowsTab.tsx (port from current working HTML)
3. Create bridge.ts with typed message passing
4. Create WebUIHost.cs
5. Wire up in SolutionTransferTool.cs (Cloud Flows only)
6. Verify it works identically to current WebView2 implementation

### Phase 2: Solutions Tab
7. Build SolutionsTab.tsx with DataGrid, target columns, version coloring
8. Port MainForm logic to WebUIHost bridge
9. Keep source/target org bar as WinForms above the WebView2

### Phase 3: Environment Variables Tab
10. Build EnvironmentVarsTab.tsx with DataGrid + inline edit sidebar
11. Port EnvVarEditPanel as React sidebar component
12. Wire up edit/transfer/refresh actions

### Phase 4: Platform Settings Tab
13. Build PlatformSettingsTab.tsx
14. Port OrgSettingsForm logic

### Phase 5: Right Panels + Polish
15. Port FlowActionPanel, SettingsPanel to React sidebars
16. Add dark mode toggle (Fluent UI supports it natively)
17. Clean up old WinForms form files

## Build Integration

- `npm run build` in WebUI/ outputs `WebUI.html` to `Resources/`
- `WebUI.html` is an EmbeddedResource in the .csproj
- Pre-build event in .csproj runs `npm run build` (or manual)
- No node_modules shipped — only the built HTML file goes into the DLL

## Verification

1. Build the Vite project: `cd WebUI && npm run build`
2. Build the C# project in Visual Studio
3. Launch XrmToolBox, open the plugin
4. Connect to a source environment
5. Verify each tab renders correctly with data
6. Add a target environment, verify target columns appear
7. Test activate/deactivate flows
8. Test env var edit and transfer
9. Test solution transfer
10. Verify modal dialogs still work (pre-import summary, results, etc.)
