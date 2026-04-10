Solution Management (err403) — Changelog
==========================================

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
