---
title: Environment Variables
excerpt: Compare and transfer environment variable values across environments
---

## Overview

The **Environment Variables** tab shows all environment variable definitions from your source environment along with their current values. When targets are connected, you can compare values side-by-side and transfer them.

## Variable List

| Column | Description |
|--------|-------------|
| **Display Name** | Friendly name of the variable |
| **Schema Name** | Technical schema name (toggle visibility with the **Schema names** switch) |
| **Type** | Variable type: `String`, `Number`, `Boolean`, `JSON`, `Data Source`, or `Secret` |
| **Default** | The default value set on the definition |
| **Current Value** | The active value in the source environment, or *(default)* if using the default |
| **\<Target Name\>** | One column per target — shows the target's value with colour coding |

## Colour Coding

Target value columns use colour to highlight differences at a glance:

- **Green** — target value matches the source
- **Red** — target value differs from the source
- *Italic gray "not found"* — variable doesn't exist on the target
