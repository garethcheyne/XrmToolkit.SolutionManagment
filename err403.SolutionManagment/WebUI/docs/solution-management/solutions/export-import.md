---
title: Export & Import from File
excerpt: Export solutions to disk or import from zip files
---

## Export to File

To save a solution as a zip file:

1. Select one or more solutions in the grid
2. Click **Export** in the toolbar
3. Choose a folder in the file browser
4. The solution zip is saved to that folder

The filename follows the pattern: `{UniqueName}_{Version}_{managed|unmanaged}.zip`

## Auto-Save

When **Auto save solutions** is enabled in General Settings, every transfer automatically saves the exported solution zip to the configured save path. This gives you a backup of every solution you deploy.

## Import from File

To import a solution from a zip file:

1. Click **Import from File** in the toolbar
2. Select the solution zip file
3. The solution is imported to all connected target environments

> [!WARNING]
> Importing from file uses your current import settings (import mode, overwrite unmanaged, etc.). Review your settings before importing.
