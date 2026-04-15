---
title: Transferring Values
excerpt: Edit and sync environment variable values to target environments
---

## Editing Values

Double-click any environment variable row to open the **Edit Panel** on the right side.

The edit panel shows:

- **Variable metadata** — display name, type badge, schema name
- **Source Value** — the current value in the source environment (read-only)
- **Per-target fields** — an editable input for each connected target

For **JSON** type variables, the editor uses a multi-line text area. All other types use a single-line input.

## Copy Source to All Targets

Click the **Copy source to all targets** button to populate every target field with the source value. You can then adjust individual targets before saving.

## Saving Changes

Click **Save** to push modified values to the targets. Only targets whose values you actually changed are updated — unchanged targets are skipped.

## Bulk Transfer

:::steps
### Select variables

Use the checkboxes to select one or more variables in the grid.

### Click Transfer Selected

The toolbar button sends all selected variables' source values to every connected target.
:::

> [!TIP]
> Use the search box and colour coding to quickly find variables that differ between environments, then select and transfer them in bulk.
