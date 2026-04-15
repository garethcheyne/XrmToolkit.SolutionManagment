---
title: Syncing Settings
excerpt: Push organization settings from source to target environments
---

## Sync Selected

:::steps
### Select settings

Click rows in the grid to select the settings you want to sync.

### Click Sync Selected

A confirmation dialog appears: *"Sync N setting(s) to M target(s)?"*

### Confirm

Click **OK** to push the selected source values to all connected targets.
:::

## Sync All Diffs

Click **Sync All Diffs** to automatically find every setting that differs between the source and at least one target, and sync them all at once.

A confirmation dialog shows how many differing settings will be synced and to how many targets.

> [!WARNING]
> Sync All Diffs can change a large number of settings at once. Review the diffs first using the **Diffs only** toggle before syncing.

## Finding Differences

Use these tools to identify what's different:

- **Diffs only** toggle — filters the grid to show only settings where at least one target differs from the source
- **Red values** in target columns indicate mismatches
- **Category** dropdown — narrow down to a specific area (e.g., Features, Email)
- **Search** — find specific settings by name or value

> [!CAUTION]
> Organization settings affect all users in the target environment. Be especially careful with **Features** and **Integration** categories, as these control functionality that users rely on.
