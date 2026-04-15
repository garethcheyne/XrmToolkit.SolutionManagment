---
title: General Settings
excerpt: Auto-save, refresh interval, and notification settings
---

## Settings Reference

| Setting | Description | Default |
|---------|-------------|---------|
| **Auto save solutions** | Automatically save exported solution zip files to disk during transfer | False |
| **Save path** | Folder where auto-saved solutions are stored (shown when auto-save is enabled) | — |
| **Refresh interval** | How often the plugin checks transfer progress (format: `HH:MM:SS`) | `00:00:10` |
| **Pre-import summary** | Show the Pre-Import Summary dialog before every transfer | True |
| **Toast notifications** | Show Windows toast notifications when transfers complete or fail | True |

## Auto-Save

When enabled, every solution exported during a transfer is also saved as a zip file to the configured path. This creates an automatic backup of every deployment.

The file is named: `{SolutionName}_{Version}_{managed|unmanaged}.zip`

> [!TIP]
> Set the save path to a network share or cloud-synced folder for team-accessible solution backups.

## Refresh Interval

Controls how frequently the plugin polls for transfer progress updates. The default of 10 seconds works well for most scenarios.

> [!WARNING]
> Setting this too low (under 5 seconds) may cause unnecessary API calls. Setting it too high means slower progress updates in the UI.
