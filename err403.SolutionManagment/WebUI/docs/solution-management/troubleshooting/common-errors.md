---
title: Common Errors
excerpt: Solutions for frequently encountered errors
---

## "Not connected"

**Cause:** No source environment is connected.

**Fix:** Use the **Source Connection** button in XrmToolBox to connect to a Dataverse environment.

---

## "(auth required)" on context menus

**Cause:** The environment ID hasn't been resolved. Features like "Open in Maker Portal" and "Open in Power Automate" need this to build the correct URL.

**Fix:** Click the **Authenticate** button in the connection bar. If you see a blinking red dot on the button, authentication is needed.

---

## Export fails with missing dependencies

**Cause:** The solution references components from other solutions that aren't installed.

**Fix:**
1. Enable **Check for missing dependencies** in [Import Settings](../settings/import.md)
2. Review the missing components dialog that appears
3. Install the required solutions before retrying

---

## Import fails — "Unable to overwrite unmanaged customizations"

**Cause:** The target has unmanaged customizations that conflict with the import.

**Fix:** Enable **Overwrite unmanaged customizations** in [Import Settings](../settings/import.md).

---

## Version not updating

**Cause:** The version update policy is set to **None** or **Prompt** with all solutions unchecked.

**Fix:**
1. Open [Version Settings](../settings/version.md)
2. Set **Update solution version** to **Always** or **Prompt**
3. If using Prompt, make sure solutions are checked in the Pre-Import Summary

---

## "Upgrade" import removes components

**Cause:** The **Upgrade** import mode deletes components that exist in the target but not in the new solution.

**Fix:** This is expected behaviour. Switch to **Update** mode if you don't want deletions, or verify the solution contents before upgrading.

---

## Environment variable values not syncing

**Cause:** The variable exists on the target as a definition but has no current value record.

**Fix:** Use the Edit Panel to manually set the value for the target, or use **Transfer Selected** to push source values.

---

## Platform settings sync changes too many values

**Cause:** **Sync All Diffs** pushes every differing setting.

**Fix:** Use the **Diffs only** filter and **Category** dropdown to review differences first, then use **Sync Selected** to push only the settings you want.
