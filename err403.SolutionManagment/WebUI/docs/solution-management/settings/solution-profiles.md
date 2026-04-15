---
title: Per-Solution Profiles
excerpt: Save different settings for individual solutions
---

## Overview

By default, all solutions use the same export/import settings. Per-solution profiles let you override these defaults for specific solutions.

## Why Use Profiles?

Common scenarios:

- **SolutionA** should always be exported as managed, but **SolutionB** as unmanaged
- One solution needs **Upgrade** import mode while others use **Update**
- A specific solution requires organization settings (Calendar, Marketing) to be included

## Creating a Profile

:::steps
### Select a solution

Click on a solution in the grid to select it.

### Open Settings

Click **Settings** in the toolbar to open the settings drawer.

### Create profile

Click the **+** button next to the profile dropdown. This creates a profile for the selected solution, pre-filled with the current default settings.

### Customize settings

Modify the export, import, version, and publish settings. Changes are saved to the profile, not to the defaults.
:::

## Switching Between Profiles

Use the **Profile** dropdown at the top of the settings drawer:

- **Default (all solutions)** — Edit the connection-level defaults
- **{Solution Name}** — Edit that solution's specific profile

## Deleting a Profile

Switch to the profile you want to delete, then click the trash icon. The solution will revert to using the default settings.

## Profile Indicator

In the Pre-Import Summary dialog, solutions with a custom profile show a :badge[profile]{error} badge next to their name, so you know which solutions use custom settings.

> [!TIP]
> General settings (auto-save, refresh interval, toast notifications) are always connection-level — they cannot be overridden per solution.
