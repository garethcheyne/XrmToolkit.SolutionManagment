---
title: Platform Settings
excerpt: Compare and synchronize organization settings across environments
---

## Overview

The **Platform Settings** tab shows all organization-level settings from your source Dataverse environment. When targets are connected, you can compare settings side-by-side and sync differences.

## Settings List

| Column | Description |
|--------|-------------|
| **Category** | Auto-assigned category based on the setting name (see below) |
| **Setting** | The setting key name |
| **Source Value** | The value in your source environment |
| **\<Target Name\>** | One column per target — colour-coded comparison |

## Colour Coding

- **Green** — target value matches the source
- **Red (bold)** — target value differs from the source
- *Gray dash* — setting not found or null on the target

## Auto-Categories

Settings are automatically categorized based on their key name:

| Category | Key patterns |
|----------|-------------|
| **Features** | Keys starting with `is`, `allow`, `enable`, `block`, `require` |
| **Email** | Contains `email` or `mail` |
| **Calendar & Time** | Contains `calendar`, `fiscal`, `date`, `time` |
| **Currency** | Contains `currency` or `pricing` |
| **Localization** | Contains `format`, `locale`, `language`, `numberseparator` |
| **Limits** | Contains `max`, `min`, `limit`, `threshold` |
| **Diagnostics** | Contains `plugin`, `trace`, `debug`, `log` |
| **Integration** | Contains `sharepoint`, `onenote`, `teams`, `yammer` |
| **General** | Everything else |

Use the **Category** dropdown to filter to a specific category.
