<p align="center">
  <img src="err403.SolutionManagment/Resources/Icon.png" alt="Solution Management" width="80" />
</p>

<h1 align="center">Solution Management (err403)</h1>

<p align="center">
  An <a href="https://www.xrmtoolbox.com/">XrmToolBox</a> plugin to transfer Dataverse solutions, environment variables, and cloud flows across organisations.
</p>

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
