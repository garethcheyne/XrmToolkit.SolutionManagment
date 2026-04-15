---
title: Authentication
excerpt: Authenticate for Maker Portal links and advanced features
---

## Why Authenticate?

Some features require an additional authentication step beyond the XrmToolBox connection:

- **Open in Maker Portal** — Right-click a solution to open it in Power Apps Maker Portal
- **Open in Power Automate** — Right-click a cloud flow to open it in the browser
- **Environment ID resolution** — Maps your connection to a Maker Portal environment URL

## How to Authenticate

Click the **Authenticate** button in the connection bar. A browser window will open for you to sign in with your Microsoft account.

> [!IMPORTANT]
> If the Authenticate button shows a blinking red dot, it means the environment ID hasn't been resolved yet. Click it to authenticate and enable Maker Portal links.

## Authentication Status

| Indicator | Meaning |
|-----------|---------|
| Blinking red dot on Authenticate button | Not authenticated — Maker Portal links disabled |
| No indicator | Authenticated — all features available |

## What If I Don't Authenticate?

The plugin works fully without authentication for:
- Solution transfers (export/import/publish)
- Cloud flow management (enable/disable)
- Environment variable comparison and transfer
- Platform settings synchronization
- All settings management

The only features that require authentication are the "Open in Maker Portal" and "Open in Power Automate" context menu links.
