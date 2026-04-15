---
title: Connecting to Environments
excerpt: How to connect your source and target environments
---

## Source Environment

The source environment is where your solutions are exported from. This is set by connecting through XrmToolBox.

:::steps
### Open XrmToolBox

Launch XrmToolBox and click **Connect** in the toolbar to open the connection manager.

### Select your source connection

Choose the environment that contains the solutions you want to transfer. If you haven't created a connection yet, click **New Connection** and follow the prompts.

### Open Solution Management

Once connected, open the **Solution Management** plugin from the plugin list. The connection bar at the top will show your source environment with a green dot indicating it's connected.
:::

## Target Environments

Target environments are where your solutions will be imported to. You can add multiple targets to transfer to several environments at once.

:::steps
### Click Add

In the connection bar at the top of the plugin, click the **Add** button.

### Select target connections

The XrmToolBox connection dialog will open. Select one or more target environments and click **OK**.

### Verify targets

Each target appears as a blue tag in the connection bar. You can remove a target by clicking the **X** on its tag.
:::

> [!TIP]
> You can add up to as many targets as you need. Each transfer operation will export once from source and import to every target in sequence.

## Switching Source and Targets

Click the **Switch** button in the Solutions toolbar to swap your source and target connections. This is useful when you need to transfer solutions in the reverse direction.

> [!WARNING]
> Switching reloads the solution list from the new source environment. Any unsaved selections will be lost.
