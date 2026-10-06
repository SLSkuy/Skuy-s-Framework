---
description: Never author Unity .meta files; the Editor generates them.
alwaysApply: true
---

# Unity `.meta` files

Do not create, edit, copy, or invent Unity `.meta` files. Add or change the asset itself (script, prefab, scene, material, …) and let the Editor generate the `.meta` on import.

Leave existing `.meta` files that Unity already wrote. Do not hand-assign GUIDs, importers, or `fileID`s to "help" Unity.
