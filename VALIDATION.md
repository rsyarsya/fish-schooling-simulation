# Validation status

Publication checks performed on 13 October 2026 using Unity 6000.4.9f1 reference
assemblies and the editor's bundled Roslyn C# compiler.

| Check | Result |
|---|---|
| All published C# sources, including Editor utilities | Compiled successfully against installed Unity and UI assemblies |
| Asset GUID uniqueness, matching metadata, package JSON, build scene, tracked-file scope | Passed repository validator |
| Full Unity project import and serialized scene validation | Not completed: the local Editor reported no valid active license |
| Play Mode, GPU execution, WebGL rebuild, and device performance | Not tested in this publication pass |

The direct compiler check confirms C# compilation, not Unity asset import,
shader compilation, or runtime behavior. Existing source emits warnings for
deprecated Unity/TextMesh Pro APIs and an unused field; they do not prevent
compilation. The repository workflow performs structural checks only and does
not require a Unity license.

The itch.io URL is supplied by the project owner. The hosted build is separate
from this source snapshot. The Notion page may require access to the owner's
workspace.
