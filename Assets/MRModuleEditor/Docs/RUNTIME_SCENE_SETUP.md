# Runtime Scene Setup

The runtime preview scene is:

```text
Assets/MRModuleEditor/Samples/Scenes/RuntimePreview.unity
```

The optional headset preview scene is:

```text
Assets/MRModuleEditor/Samples/Scenes/RuntimePreview_Headset.unity
```

## Required concepts

A runtime scene needs enough components to:

1. Load `module.json`.
2. Validate it.
3. Run steps.
4. Resolve scene-bound objects.
5. Resolve anchors/layouts.
6. Show text/image/MCQ panels.
7. Play audio.
8. Receive interaction signals.
9. Reset runtime state between runs.

## Important components

| Component/concept | Purpose |
|---|---|
| `RuntimeModuleLoader` | Reads and validates a module JSON file. |
| `ModuleRunner` | Executes steps and controls play/pause/stop/restart. |
| `StepHandlerRegistry` | Maps step type strings to runtime handlers. |
| `SceneBindingRegistry` | Maps module object IDs to scene `BindableObject`s. |
| `BindableObject` | Marks a scene object as controllable by module data. |
| Anchor/layout resolver components | Resolve head/world/object placement. |
| Spatial UI services/panels | Show text, image, and MCQ panels. |
| Audio service | Plays module audio assets. |
| Interaction providers/context | Send UI/selection events to runtime panels. |

## Sample object binding

The sample module defines:

```json
{
  "id": "object.robot_preview",
  "bindingKey": "RobotPreview"
}
```

The runtime scene must contain a matching `BindableObject` with:

```text
Binding Key = RobotPreview
```

If the binding is missing, object steps such as `showObject`, `moveObject`, `showFrame`, `rotateJoint`, and `resetRobot` will not behave correctly.

## Loader path modes

`RuntimeModuleLoader` supports:

| Mode | Use case |
|---|---|
| Assets-relative | Editor preview and sample modules under `Assets/`. |
| StreamingAssets-relative | Builds/headset demos. |
| Absolute path | External module folder during development. |

## Preview from the authoring window

`ModulePreviewLauncher` stores the selected module path in Unity `SessionState`, opens `RuntimePreview.unity`, enters Play Mode, finds the scene loader, sets the module path, and loads it.

This means preview depends on:

1. the module being saved,
2. the module path being inside `Assets/`,
3. the preview scene existing,
4. the preview scene containing a loader and runner.

## Common scene setup failures

| Symptom | Likely cause |
|---|---|
| Preview button refuses to run | Unsaved module or validation errors. |
| Preview opens but no module loads | Missing `RuntimeModuleLoader` or path not set. |
| Module loads but does not play | Missing `ModuleRunner` or runtime control panel not started. |
| Object does not move/show | Missing `BindableObject` binding key. |
| Text/image panel appears too far away | Bad layout or anchor placement. |
| MCQ does not submit | Interaction provider/context/panel issue. |
| Audio never completes | Audio step has `loop=true` and `waitForCompletion=true`, which validation should reject. |

