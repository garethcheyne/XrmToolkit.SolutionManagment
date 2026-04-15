---
title: Pre-Import Summary
excerpt: Review and adjust transfer settings before importing
---

## Overview

The Pre-Import Summary dialog appears before every transfer. It gives you a chance to review and change key settings before the transfer begins.

## Editable Settings

These settings can be adjusted per-transfer without changing your saved profile:

| Setting | Description | Default |
|---------|-------------|---------|
| **Import as managed** | Whether the solution is imported as a managed solution | True |
| **Check for missing dependencies** | Verify dependencies exist in the target before import | True |
| **Convert to managed** | Convert unmanaged customizations to managed during import | False |
| **Overwrite unmanaged customizations** | Overwrite existing unmanaged customizations | True |
| **Skip product update dependencies** | Skip dependency checks related to product updates | False |
| **Import mode** | How the solution is imported (Update, Stage for Upgrade, Upgrade) | Update |

## Import Modes

:::tabs
@tab Update
Applies changes from the solution to the target. Existing components are updated but nothing is removed. This is the safest and most common option.

@tab Stage for Upgrade
Installs the new version alongside the old one. You must manually apply the upgrade later. Use this when you need to test the new version before committing.

@tab Upgrade
Installs the new version and removes any components that are no longer in the solution. This is a destructive operation — components deleted from the solution will be removed from the target.
:::

## Version Table

When version updates are enabled in your settings, the summary shows a version table:

| Column | Description |
|--------|-------------|
| **Friendly name** | Solution display name |
| **Unique name** | Solution technical name |
| **Current version** | Version currently in source |
| **New version** | Computed version after bump |

Use the checkboxes to select which solutions should have their version updated. Uncheck a solution to keep its current version.

### Skip New Solution Version

Enable the **Skip new solution version** checkbox to skip version updates for all solutions in this transfer. Only checked solutions in the table will be updated.

> [!TIP]
> Version preview is computed using the same algorithm as the actual version bump — what you see in the summary is what will be applied.
