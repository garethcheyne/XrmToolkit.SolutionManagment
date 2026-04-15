---
title: Export Settings
excerpt: Configure how solutions are exported from the source environment
---

## Settings Reference

| Setting | Description | Default |
|---------|-------------|---------|
| **Export as managed** | Export the solution as a managed solution | True |
| **Export async** | Use asynchronous export (recommended for large solutions) | True |
| **Autonumbering** | Include auto-numbering settings | False |
| **Calendar** | Include calendar settings | False |
| **Customization** | Include customization settings | False |
| **Email Tracking** | Include email tracking settings | False |
| **External Apps** | Include external application settings | False |
| **General** | Include general organization settings | False |
| **ISV Config** | Include ISV configuration settings | False |
| **Marketing** | Include marketing settings | False |
| **Outlook Sync** | Include Outlook synchronization settings | False |
| **Relationship Roles** | Include relationship role settings | False |
| **Sales** | Include sales settings | False |

## Managed vs Unmanaged

:::tabs
@tab Managed
- Components cannot be edited in the target
- Can be cleanly uninstalled
- Best for production deployments
- Recommended for ALM

@tab Unmanaged
- Components can be edited in the target
- Cannot be cleanly uninstalled
- Best for development environments
- Use when target developers need to modify components
:::

## Organization Settings (Autonumbering, Calendar, etc.)

These toggles control whether organization-wide settings are included in the exported solution. In most cases, leave these disabled — they add system-level configuration that may not be appropriate to transfer between environments.

> [!CAUTION]
> Enabling organization-wide settings (Autonumbering, Calendar, General, etc.) may overwrite target environment configuration that was intentionally set differently. Only enable these if you specifically need to synchronize these settings.
