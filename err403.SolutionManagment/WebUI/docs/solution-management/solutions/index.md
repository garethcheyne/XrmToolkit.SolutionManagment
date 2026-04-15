---
title: Solutions
excerpt: View, transfer, and manage solutions across environments
---

## Overview

The Solutions tab is the main workspace. It displays all unmanaged solutions from your source environment and shows version comparisons against each connected target.

## The Solutions Grid

Each row shows a solution with:
- **Unique Name** — The technical name
- **Display Name** — The friendly name
- **Version** — Current version in source
- **Target columns** — One column per connected target showing the installed version, managed/unmanaged status, and version match indicator

## Visual Indicators

| Icon | Meaning |
|------|---------|
| :badge[green dot]{success} | Version matches between source and target |
| :badge[red dot]{error} | Version differs — target is out of date |
| Lock icon (filled) | Solution is managed on the target |
| Lock icon (open) | Solution is unmanaged on the target |
| Dash (—) | Solution not found on the target |

## Toolbar Actions

| Button | Description |
|--------|-------------|
| **Refresh** | Reload the solution list from source |
| **Transfer** | Transfer selected solutions to all targets |
| **Import from File** | Import a solution zip file to targets |
| **Export** | Export selected solutions to a zip file on disk |
| **Remove from Targets** | Delete selected solutions from target environments |
| **Remove from Source** | Delete selected solutions from the source environment |
| **Switch** | Swap source and target connections |
| **Missing Deps** | Check for missing dependencies |
| **Settings** | Open the settings drawer |
