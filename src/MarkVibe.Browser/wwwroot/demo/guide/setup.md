---
title: Prepare a workspace
---

# Prepare a workspace

A documentation workspace can be a connected folder, an imported folder, a set of selected files or a ZIP bundle. Each method provides a useful way to begin reading.

## Installation

Use the running viewer through HTTPS or localhost. No local documents need to be uploaded to a service.

For this sample collection, choose **Explore sample workspace** in settings. For your own collection, choose **Open folder** or **Open file**.

## Choose the right source

| Source | Keeps folder paths | Refresh |
| --- | --- | --- |
| Connected folder | Yes | Read the folder again |
| Imported folder | Yes | Select the folder again |
| Selected files | Filenames initially | Add related files |
| Documentation ZIP | Yes | Select the ZIP again |

A connected folder requires browser support and your permission. When that capability is unavailable, the viewer offers an imported folder. A ZIP is another option when folder selection cannot preserve paths.

### A tiny example

~~~text
notes/
  README.md
  guide/
    setup.md
  assets/
    workspace.svg
~~~

The path [../README.md](../README.md) returns to the collection. An encoded heading can lead to [a reading section](reading.md#finding-your-place).

## Related images

![Connected documents](../assets/workspace.svg)

Only supported local images are resolved from the opened workspace. Missing resources have a useful message and selected-file workspaces let you choose the missing file.

## Installation

This deliberately repeated heading has its own anchor: [the second installation section](#installation-1).

## Next steps

Read [the browser notes](../reference/browser-notes.md) or [the reading guide](reading.md#finding-your-place).
