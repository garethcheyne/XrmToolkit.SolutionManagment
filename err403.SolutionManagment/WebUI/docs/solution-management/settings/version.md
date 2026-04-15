---
title: Version Settings
excerpt: Configure automatic solution version bumping
---

## Settings Reference

| Setting | Description | Default |
|---------|-------------|---------|
| **Update solution version** | Whether to bump the version before export | Prompt |
| **Version policy** | Which part of the version number to increment | Date |
| **Date version mask** | Template for date-based versions (shown when policy is Date) | `yyyy.MM.dd.x` |

## Version Policies

See [Version Management](../solutions/version-management.md) for detailed documentation on each policy and the date mask format.

## Prompt Behaviour

When set to **Prompt**, the Pre-Import Summary dialog shows a version table where you can:

- See the computed new version for each solution
- Check/uncheck individual solutions to control which ones get bumped
- Skip all version updates with the "Skip new solution version" checkbox
