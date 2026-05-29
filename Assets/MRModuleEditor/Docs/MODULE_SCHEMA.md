# Module Schema Guide

The persistent module format is a JSON file named `module.json`. It deserializes into `ModuleDocument`.

Current schema version:

```json
"schemaVersion": "0.1"
```

## Top-level shape

```json
{
  "schemaVersion": "0.1",
  "moduleId": "module.forward_kinematics_mini",
  "title": "Forward Kinematics Mini Demo",
  "description": "...",
  "author": "...",
  "estimatedDurationSeconds": 90,
  "assets": [],
  "objects": [],
  "anchors": [],
  "layouts": [],
  "steps": []
}
```

## `assets[]`

Assets are files referenced by steps.

```json
{
  "id": "asset.intro_image",
  "type": "image",
  "path": "assets/images/intro.png",
  "label": "Intro Image"
}
```

Current asset types used by the sample:

```text
image
audio
```

The path is relative to the module folder, not relative to the Unity `Assets/` root.

## `objects[]`

Objects connect module IDs to scene-bound objects.

```json
{
  "id": "object.robot_preview",
  "label": "Robot Preview",
  "bindingKey": "RobotPreview"
}
```

The runtime scene must contain a `BindableObject` with the matching binding key.

## `anchors[]`

Anchors define reference frames for layouts.

```json
{
  "id": "anchor.head.default",
  "type": "head",
  "targetObjectId": ""
}
```

Current anchor types:

| Type | Meaning |
|---|---|
| `head` | Relative to the user's/camera's head. |
| `world` | Relative to a stable world/root point. |
| `object` | Relative to a module object. Requires `targetObjectId`. |

## `layouts[]`

Layouts place steps, panels, or objects relative to anchors.

```json
{
  "id": "layout.step.001.head_panel",
  "targetId": "step.001",
  "anchorId": "anchor.head.default",
  "position": { "x": 0, "y": -0.15, "z": 1.25 },
  "rotationEuler": { "x": 0, "y": 0, "z": 0 },
  "scale": { "x": 1, "y": 1, "z": 1 }
}
```

`targetId` can currently reference module IDs such as step IDs or object IDs.

## `steps[]`

A step is a single runtime action.

```json
{
  "id": "step.001",
  "type": "text",
  "title": "Welcome",
  "durationSeconds": 10,
  "parameters": {
    "text": "Welcome to the mini demo.",
    "anchorId": "anchor.head.default"
  }
}
```

## Step parameters

`ModuleStep.parameters` is a flexible dictionary:

```csharp
Dictionary<string, JToken>
```

This is similar to a Python `dict[str, Any]`. It avoids needing a separate C# subclass for every step type.

Parameter definitions for built-in steps live in `StepCatalog` registrations so the editor, defaults, and generic validation stay aligned with runtime handlers.

## Current flow fields

Most steps continue to the next step in the list. Optional flow override:

```json
"nextStepId": "step.012"
```

MCQ-specific branch fields:

```json
"onCorrectStepId": "step.012",
"onWrongStepId": "step.011"
```

## Current confirm example

`confirm` is the single built-in learner-gate step. It normally waits for the learner to press Continue, and can optionally complete from a filtered `InteractionContext` signal.

```json
{
  "id": "step.002.ready",
  "type": "confirm",
  "title": "Ready Check",
  "durationSeconds": 0,
  "parameters": {
    "message": "Look at the equipment model. Select Continue when you are ready.",
    "buttonLabel": "Continue",
    "autoContinueAfterSeconds": 0,
    "anchorId": "anchor.head.default",
    "completeOnSignal": false
  }
}
```

When `completeOnSignal` is true, `signalTargetId` may use `{stepId}` as a placeholder. For example, `{stepId}.confirm` resolves to the current confirm button target.

## Current MCQ example

```json
{
  "id": "step.010",
  "type": "mcq",
  "title": "Quick Check",
  "durationSeconds": 0,
  "parameters": {
    "question": "What does forward kinematics compute?",
    "choices": [
      "Joint angles from pose",
      "End-effector pose from joint angles"
    ],
    "correctIndex": 1,
    "onCorrectStepId": "step.012",
    "onWrongStepId": "step.011"
  }
}
```

## Validation rules to remember

Validation currently checks:

- required module fields,
- duplicate IDs,
- known step types,
- known anchor types,
- object references,
- asset references,
- asset type matching,
- layout references,
- MCQ validity,
- branch references,
- RoboticsLite step parameters,
- audio loop/wait conflicts.

