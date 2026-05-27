# Demo Runbook — MVP Baseline

This runbook describes the current MVP demo before Phase 2 refactors begin.

## Demo goal

Show that MR Module Editor can:

1. Open a JSON-backed module in the Unity Editor authoring window.
2. Validate module data.
3. Preview the module in the runtime scene.
4. Execute text, image, audio, object, movement, RoboticsLite, and MCQ steps.
5. Branch based on the MCQ answer.
6. Export the module folder.

## Project version

- Unity: `2022.3.62f1c1`
- Main sample: `Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json`
- Runtime preview scene: `Assets/MRModuleEditor/Samples/Scenes/RuntimePreview.unity`
- Optional headset preview scene: `Assets/MRModuleEditor/Samples/Scenes/RuntimePreview_Headset.unity`

## Pre-demo checks

- [ ] Open the project in Unity.
- [ ] Wait for scripts to finish compiling.
- [ ] Open the Console and confirm there are no red compile errors.
- [ ] Confirm the sample module file exists.
- [ ] Confirm sample media exists in the real project:
  - `assets/images/intro.png`
  - `assets/audio/intro.mp3`
- [ ] Run EditMode tests if time allows.
- [ ] Run PlayMode tests if time allows.

## Demo script

### 1. Open the authoring window

Unity menu:

```text
MR Module Editor > Module Editor
```

Explain: this is the Unity Editor authoring surface. It edits a `ModuleDocument` and saves it as `module.json`.

### 2. Load or create the sample module

Option A: click **Load** and choose:

```text
Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json
```

Option B: click **New Template** to recreate the Forward Kinematics Mini template from code.

Explain: the sample module contains assets, objects, anchors, layouts, and steps.

### 3. Show validation

Look at the validation summary in the editor window.

Expected result:

```text
Validation passed.
```

If there are warnings, read them out loud and explain whether they are safe for the demo.

### 4. Preview the module

Click **Preview**.

Expected behavior:

1. Unity opens `RuntimePreview.unity` if it is not already open.
2. Unity enters Play Mode.
3. The runtime loader loads the selected module.
4. The Console logs that preview loaded.
5. The runtime control panel can start the module.

### 5. Run through the module

During preview, demonstrate:

- text panel,
- audio narration,
- image panel,
- object visibility,
- object movement,
- object-attached/head/world layout behavior,
- RoboticsLite frame/joint/reset steps,
- MCQ interaction,
- branch behavior.

For the MCQ, the correct answer is:

```text
End-effector pose from joint angles
```

Expected result after correct answer: the module branches to the correct/summary path.

### 6. Export the module

Return to Edit Mode.

Use:

```text
MR Module Editor > Export > Export Current Module Folder
```

Choose an output folder such as:

```text
ModuleExports/
```

Expected result: the module folder is copied, referenced assets are included, and `.meta` files are excluded unless intentionally included by the exporter.

Optional headset path:

```text
MR Module Editor > Export > Export Current Module To StreamingAssets
```

Use this only when preparing a headset/build demo.

## Demo failure fallback

If preview fails:

1. Stop Play Mode.
2. Reopen `MR Module Editor > Module Editor`.
3. Reload the sample `module.json`.
4. Check validation messages.
5. Open `RuntimePreview.unity` manually.
6. Confirm the scene has a `RuntimeModuleLoader`, `ModuleRunner`, anchor/layout services, display panels, and a `BindableObject` with binding key `RobotPreview`.

If the editor window gets into a bad state, close and reopen it from the menu.

## Phase 1 exit demo

At the end of Phase 1, run this same demo without changing the sample module. The deliverable is the unchanged working demo plus the documentation set.

