<p align="center">
  <img src="err403.SolutionManagment/Resources/Icon.png" alt="Solution Management" width="80" />
</p>

<h1 align="center">Solution Management (err403)</h1>

<p align="center">
  An <a href="https://www.xrmtoolbox.com/">XrmToolBox</a> plugin to transfer Dataverse solutions, environment variables, and cloud flows across organisations.
</p>

---

## This is a complete reinvention

This plugin shares a name and concept with [DamSim / SolutionTransferTool](https://github.com/MscrmTools/DamSim.SolutionTransferTool) by Damien Aicheh — the original proved the idea. Everything else has been rebuilt from scratch.

**The original** ([source](https://github.com/MscrmTools/DamSim.SolutionTransferTool)): solution transfer focused — no environment variable management, no cloud flow control, no React UI.

**This version:**

| | Original | err403 |
|---|---|---|
| UI technology | WinForms | **React 18 + Fluent UI 9 embedded via WebView2** |
| Scope | Solution transfer | Solutions + Environment Variables + Cloud Flows |
| Environment variables | None | Full browse, compare, edit, and bulk transfer |
| Cloud flows | None | List, compare, activate/deactivate across targets |
| Version management | Global plugin setting | **Per-solution policies** (Major / Minor / Build / Revision / Date) |
| Guidance | None | **Inline teaching popovers** with MS Learn links throughout |
| Test coverage | None | **76 unit tests** (Vitest + xUnit) with branded PDF report |

The C# layer is a thin set of focused service classes (no WinForms dialogs). All UI decisions — confirmations, settings, results, progress — live in React.

---

## Features

### Solution Transfer
- Export solutions (managed or unmanaged) from a source environment and import them into one or more target environments in a single operation.
- View solution versions installed on every connected target — prefixed with **(M)** managed or **(U)** unmanaged.
- Re-order the import sequence before transfer.
- Pre-import summary with per-solution confirmation.
- **Safety warnings** when importing managed over unmanaged (or vice-versa).

### Environment Variables
- Browse all environment variables from the source environment with their current and default values.
- Compare values side-by-side across source and targets.
- Edit individual values — with JSON pretty-printing, number, and boolean validation.
- Bulk-transfer selected variables to one or more targets.

### Cloud Flows
- List all cloud flows from the source with status, solution, owner, and modified date.
- Filter by active only, solution name, or free-text search.
- Compare flow activation status across source and target environments.
- Activate or deactivate selected flows on one or more targets in bulk.
- Results dialog with success/failure summary and colour-coded rows.
- **Open Flow in Browser** — jump directly to a failed flow in the target environment to fix connection references.

### Import Progress & Diagnostics
- Real-time progress tracking with automatic polling of async operations.
- **Refresh Status** button to manually re-check progress when large imports appear stale.
- **View Message** on import errors — modal dialog with:
  - Plain-text error message
  - Parsed Missing Dependencies table (Required Component, Schema, Solution, Dependent, Resolvable)
  - Raw content with pretty-printed XML
- **Download log file** saves SDK import log, plus split error message (`.txt`) and error XML (`.xml`) when available.
- **Missing Dependencies check** — select solutions and click **Missing Deps** to check all connected targets using the Dataverse `RetrieveMissingComponents` message; results grouped by target → solution with dependency cards and MS Learn links.

### Inline Guidance
- **Teaching popovers** on Import Mode (Update / Upgrade / Stage for Upgrade), Check Dependencies, Overwrite Unmanaged, Convert to Managed — with MS Learn links throughout.

### UI
- Tabbed layout powered by **WeifenLuo DockPanel Suite**.
- Context-sensitive toolbar — buttons change depending on the active tab.
- Shared source / target connection bar visible on every tab.

---

## Installation

Install from the **XrmToolBox Plugin Store** — search for **Solution Management**.

Or build from source:

```
MSBuild err403.SolutionManagment.sln -t:Build -p:Configuration=Release
```

The post-build step copies the output to XrmToolBox's plugin folders automatically.

---

## Development

### React UI
```bash
cd err403.SolutionManagment/WebUI
npm install
npm run dev        # dev server
npm run build      # production build → embeds WebUI.html
```

### Tests
```bash
npm run test            # Vitest (React/TypeScript)
dotnet test err403.SolutionManagment.Tests/  # xUnit (C#)
npm run report:test     # Combined branded PDF + Markdown → /reports/
```

---

## Requirements

- [XrmToolBox](https://www.xrmtoolbox.com/) 1.2024.1.x or later
- .NET Framework 4.8
- At least one Dataverse / Dynamics 365 connection configured in XrmToolBox

---

## Credits

Forked from [DamSim / SolutionTransferTool](https://github.com/MscrmTools/DamSim.SolutionTransferTool) by Damien Aicheh.

Maintained by [Gareth Cheyne](https://github.com/garethcheyne).

---

## License

[MIT](LICENSE)
