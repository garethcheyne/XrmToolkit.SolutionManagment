---
title: Import Settings
excerpt: Configure how solutions are imported into target environments
---

## Settings Reference

| Setting | Description | Default |
|---------|-------------|---------|
| **Import mode** | How the solution is applied to the target | Update |
| **Check for missing dependencies** | Verify all dependencies exist before importing | True |
| **Convert to managed** | Convert unmanaged customizations to managed during import | False |
| **Deploy missing packages** | Automatically deploy missing dependency packages | True |
| **Overwrite unmanaged** | Overwrite existing unmanaged customizations | True |
| **Publish workflows** | Activate workflows/flows included in the solution after import | True |
| **Skip product update deps** | Skip dependency checks for product updates | False |

## Import Modes Explained

| Mode | Behaviour | When to Use |
|------|-----------|-------------|
| **Update** | Adds and updates components, never removes | Day-to-day deployments |
| **Stage for Upgrade** | Installs new version alongside old one | When you need to test before committing |
| **Upgrade** | Replaces old version, removes deleted components | Clean deployments where removed components should be deleted |

> [!IMPORTANT]
> The **Upgrade** mode is destructive. Components that exist in the target but not in the new solution version will be permanently deleted. Always verify the solution contents before using this mode.

## Publish Settings

| Setting | Description | Default |
|---------|-------------|---------|
| **Publish customizations** | Run PublishAllCustomizations after import | True |

Publishing makes all imported changes visible to users. If disabled, changes are imported but not active until someone manually publishes.
