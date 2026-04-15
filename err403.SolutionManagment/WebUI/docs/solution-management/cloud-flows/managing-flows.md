---
title: Managing Flows
excerpt: Activate, deactivate, and inspect flows across environments
---

## Bulk Activate / Deactivate

:::steps
### Select flows

Click rows in the grid — multi-select is supported — or use the checkbox column to pick multiple flows.

### Click Activate or Deactivate

The toolbar buttons apply to all selected flows across all connected targets.

### Review results

After the operation completes, a results dialog appears showing success or failure for each flow on each target.
:::

## Open in Power Automate

Right-click a flow and choose **Open in Power Automate** to open it directly in the Power Automate maker portal.

> [!NOTE]
> This option requires authentication. If you see "(auth required)" next to the menu item, click the **Authenticate** button in the connection bar first. The blinking red indicator on the button means your environment ID hasn't been resolved yet.

## Flow Types

| Type | Description |
|------|-------------|
| **Classic** | Legacy Dynamics 365 workflows (category 0) |
| **Cloud Flow** | Modern Power Automate flows (category 5) |
| **Desktop Flow** | Power Automate Desktop flows (category 6) |

## Flow States

| State | Meaning |
|-------|---------|
| **On** | Flow is active and will trigger on events |
| **Off** | Flow is disabled and will not trigger |
| **Suspended** | Flow was automatically suspended (usually due to repeated failures) |
