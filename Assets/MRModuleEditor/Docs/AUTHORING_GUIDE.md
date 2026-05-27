# Authoring Guide

This is a Phase 1 first-pass guide. Phase 4 will expand authoring usability and asset workflows.

## Open the authoring window

```text
MR Module Editor > Module Editor
```

## Create a module

Use **New Empty** or **New Template**.

- **New Empty** creates minimal module data.
- **New Template** creates the Forward Kinematics Mini sample template.

## Load a module

Click **Load** and select a `module.json` file.

Current sample:

```text
Assets/MRModuleEditor/Samples/SampleModules/ForwardKinematicsMini/module.json
```

## Edit metadata

Use the module metadata fields for:

- schema version,
- module ID,
- title,
- author,
- estimated duration,
- description.

## Add/edit steps

Use the left step list to select and add steps.

The right inspector edits:

- step ID,
- step type,
- title,
- duration,
- type-specific parameters,
- flow fields.

## Current add-step buttons

Current step types available through the editor include:

```text
Text
Image
Audio
Wait
Show Object
Move Object
MCQ
Show Frame
Rotate Joint
Reset Robot
```

The RoboticsLite buttons are useful for the sample, but Phase 2 should move domain-specific definitions out of core/editor hard-coded lists.

## Edit assets/objects/anchors/layouts

Use the lower module data editors in the authoring window.

Current limitations:

- asset paths are still mostly manual,
- object layout editing is not fully first-class,
- validation is shown as one summary block,
- the editor does not yet have preview-from-selected-step.

## Validate

Read the validation summary before previewing.

Do not preview modules with validation errors.

## Preview

Click **Preview**. The module must be saved and inside the Unity `Assets/` folder for the current preview launcher.

## Export

Use the export menu:

```text
MR Module Editor > Export > Export Current Module Folder
```

or, for StreamingAssets/headset prep:

```text
MR Module Editor > Export > Export Current Module To StreamingAssets
```

## Current limitations

- Asset import/copy is not yet a comfortable editor workflow.
- Adding new step types is still hard-coded.
- Object layout editing needs improvement.
- Preview-from-selected-step is not yet implemented.
- Validation messages are not yet grouped by category.

