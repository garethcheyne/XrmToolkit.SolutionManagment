---
title: Cloud Flows
excerpt: View, activate, deactivate, and manage cloud flows across environments
---

## Overview

The **Cloud Flows** tab displays all cloud flows, classic workflows, and desktop flows from your source environment. You can activate or deactivate flows in bulk and compare flow states across connected targets.

## Flow List

Each flow row shows:

| Column | Description |
|--------|-------------|
| **Flow Name** | Name of the workflow |
| **Type** | `Classic`, `Cloud Flow`, or `Desktop Flow` |
| **Status** | Current state: `On`, `Off`, or `Suspended` |
| **Owner** | The user who owns the flow |
| **Modified** | Last modification date |
| **\<Target Name\>** | One column per connected target showing the flow's state |

## Target State Toggles

When you have target environments connected, each target column shows a live **switch** for each flow:

- **On** — the flow is active on that target
- **Off** — the flow is inactive on that target
- **"not found"** — the flow doesn't exist on the target

Click any switch to immediately activate or deactivate that individual flow on the target.

## Filtering

- **Search** — filter by flow name or owner
- **Active only** — show only flows that are currently on
- The count badge shows the total number of flows and how many are selected
