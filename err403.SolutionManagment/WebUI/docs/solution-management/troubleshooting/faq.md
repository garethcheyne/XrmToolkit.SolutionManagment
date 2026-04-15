---
title: FAQ
excerpt: Frequently asked questions
---

## Are settings saved per environment or globally?

Settings are saved **per connection**. Each environment you connect to gets its own saved settings that reload automatically. Within a connection, you can also create [per-solution profiles](../settings/solution-profiles.md).

---

## Can I transfer to multiple targets at once?

Yes. Use the target connections panel to add multiple target environments. Transfers, flow operations, and env var syncs apply to all connected targets simultaneously.

---

## Does the tool support managed and unmanaged solutions?

Yes. Use the **Export as managed** toggle in [Export Settings](../settings/export.md) to control this. You can also set this on a per-solution basis with [profiles](../settings/solution-profiles.md).

---

## What's the difference between Update, Stage for Upgrade, and Upgrade?

| Mode | Adds new components | Updates existing | Removes deleted | Requires holding solution |
|------|:---:|:---:|:---:|:---:|
| **Update** | Yes | Yes | No | No |
| **Stage for Upgrade** | Yes | Yes | No (staged) | Yes |
| **Upgrade** | Yes | Yes | Yes | No |

See [Import Settings](../settings/import.md) for details.

---

## How does version bumping work?

The tool can automatically increment solution versions before export using several policies (Major, Minor, Build, Revision, Date). See [Version Management](../solutions/version-management.md) for the full explanation.

---

## Do I need to authenticate separately from XrmToolBox?

The XrmToolBox connection handles Dataverse API access. The separate **Authenticate** button in the plugin resolves your environment ID, which is needed for:

- Opening solutions in the Maker Portal
- Opening flows in Power Automate

If you don't need these features, you can skip authentication.

---

## Can I export solutions to a file without transferring?

Yes. Right-click a solution and choose **Export to file** from the context menu. The solution is exported and saved as a zip file to a location you choose.

---

## What happens if I cancel a transfer mid-way?

The export/import operation that is currently running on the server will continue. However, no further solutions in the batch will be processed. Any already-imported solutions remain in the target.
