# Quickstart

Use this when opening the project for the first time.

## Requirements

- Unity `2022.3.62f1c1`, or the closest available Unity 2022.3 LTS patch.
- The package manifest in `Packages/manifest.json` restored successfully.
- The project folder contains `Assets/`, `Packages/`, and `ProjectSettings/`.

## 1. Open the project

1. Open Unity Hub.
2. Add/open this project folder.
3. Wait for import and script compilation.
4. Open the Console:

```text
Window > General > Console
```

5. Confirm there are no red compile errors.

Compile errors must be fixed first because Unity disables broken editor scripts.

## 2. Open the module editor

Unity menu:

```text
MR Module Editor > Module Editor
```

Expected result: a window named `MR Module Editor` opens.

## 3. Load the sample module

Click **Load** and select:

```text
Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json
```

Expected module title:

```text
Forward Kinematics Mini Demo
```

## 4. Validate

Read the validation summary in the editor window.

Expected result:

```text
Validation passed.
```

Do not preview a module with validation errors.

## 5. Preview

Click **Preview**.

Unity should open:

```text
Assets/MRModuleEditor/Samples/Scenes/RuntimePreview.unity
```

Then Unity enters Play Mode. Use the runtime control panel to start the module.

## 6. Export

Return to Edit Mode and use:

```text
MR Module Editor > Export > Export Current Module Folder
```

For headset/build preparation, use:

```text
MR Module Editor > Export > Export Current Module To StreamingAssets
```

Expected StreamingAssets folder:

```text
Assets/StreamingAssets/MRModuleEditor/SampleModules/ForwardKinematicsMini
```

## Common first-run problems

| Problem | What to check |
|---|---|
| Console compile errors | Fix these before using the editor window. Unity disables broken editor scripts. |
| Sample media missing | Make sure `intro.png` and `intro.mp3` exist in the real project. The source bundle may omit binary files. |
| Preview opens but nothing starts | Look for runtime control panel and Console logs. Confirm `RuntimePreview.unity` contains a `ModuleRunner`. |
| Robot object does not appear | Confirm the scene has a `BindableObject` with binding key `RobotPreview`. |
| Unknown step type | Current known step types are hard-coded in `ModuleValidator.cs` and `StepInspectorView.cs`. This is fixed later in Phase 2. |

