---
title: Your First Transfer
excerpt: Step-by-step guide to transfer a solution from source to target
---

## Performing a Transfer

:::steps
### Select solutions

In the **Solutions** tab, click on the solutions you want to transfer. Use the search bar to filter by name.

### Verify targets

Check the connection bar to confirm your target environments are connected.

### Review settings

Click the **Settings** button to open the settings drawer. Verify your export and import settings are correct for this transfer.

### Click Transfer

Click the **Transfer** button in the toolbar. The **Pre-Import Summary** dialog will open.

### Review the Pre-Import Summary

The summary shows:
- The import settings that will be used (you can change them here)
- Target environments that will receive the solution
- A version table showing current and new version numbers

Adjust any settings as needed, then click **Transfer** to begin.

### Monitor progress

The progress panel at the bottom shows real-time status for each solution/target combination:
- **Exporting** — Solution is being exported from source
- **Importing** — Solution is being imported to target
- **Publishing** — Customizations are being published
- **Complete** — Transfer finished successfully
:::

> [!NOTE]
> If you have **Toast notifications** enabled in settings, you'll also receive a Windows notification when each transfer completes or fails.

## What Happens During Transfer

For each solution + target combination, the plugin performs these steps:

1. **Export** — Exports the solution from the source environment (managed or unmanaged based on settings)
2. **Auto-save** — If enabled, saves the exported solution zip to disk
3. **Version bump** — If configured, updates the solution version in source before export
4. **Import** — Imports the solution to the target environment
5. **Publish** — Publishes all customizations on the target

## Transfer Results

After all transfers complete, a results dialog shows the outcome for each operation. Solutions that failed will show the error message from the Dataverse API.
