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

Tradeoff: parameter meaning is currently scattered across:

```text
Runtime handlers
ModuleValidator.cs
StepInspectorView.cs
ModuleTemplateFactory.cs
Tests
```

Phase 2 will keep this JSON shape but introduce a Step Catalog so parameter definitions are easier to share between validation, authoring, and runtime.

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

