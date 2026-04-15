---
title: Version Management
excerpt: Automatically bump solution versions before transfer
---

## How Version Bumping Works

When **Update solution version** is set to **Yes** or **Prompt** in settings, the plugin bumps the solution version in the source environment before exporting.

## Version Policies

| Policy | Example | Description |
|--------|---------|-------------|
| **Major** | `2.0.0.0` → `3.0.0.0` | Increments the first number, resets others |
| **Minor** | `1.2.0.0` → `1.3.0.0` | Increments the second number, resets lower |
| **Build** | `1.0.3.0` → `1.0.4.0` | Increments the third number |
| **Revision** | `1.0.0.5` → `1.0.0.6` | Increments the fourth number |
| **Manual** | — | You provide the version manually |
| **Date** | `2026.04.12.1` | Uses a date-based mask |

## Date Version Mask

When using the **Date** policy, the version string is constructed from a mask pattern:

| Token | Replaced With | Example |
|-------|--------------|---------|
| `yyyy` | Current year | `2026` |
| `MM` | Month (zero-padded) | `04` |
| `dd` | Day (zero-padded) | `12` |
| `HHmm` | Hour and minute | `1430` |
| `x` | Incremental counter | `1`, `2`, `3`... |

The `x` token is an auto-incrementing counter that resets when the date prefix changes. For example, with mask `yyyy.MM.dd.x`:

- First transfer today: `2026.04.12.1`
- Second transfer today: `2026.04.12.2`
- First transfer tomorrow: `2026.04.13.1`

> [!TIP]
> The default mask `yyyy.MM.dd.x` produces clean date-based versions that sort correctly and tell you exactly when a solution was last deployed.

## Update Version Setting

| Value | Behaviour |
|-------|-----------|
| **No** | Never bump versions |
| **Yes** | Always bump before transfer |
| **Prompt** | Show version table in the Pre-Import Summary and let you choose |
