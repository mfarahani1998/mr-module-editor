# Architecture Overview

MR Module Editor is a Unity-based authoring and runtime preview project for data-driven mixed-reality learning modules.

## Folder map

```text
Assets/MRModuleEditor/
├─ Core/                 # Data model, JSON serialization, templates, validation
├─ Runtime/              # Runtime loading, execution, UI, anchors, interaction, scene binding
├─ Authoring/Editor/     # Unity Editor authoring window and editor-only utilities
├─ Domains/RoboticsLite/ # Current sample/domain-specific robotics-lite handlers
├─ Samples/              # Sample modules and preview scenes
├─ Styles/               # Visual styling assets/data
├─ Tests/                # EditMode and PlayMode tests
└─ Docs/                 # Unity-local docs/checklists
```

## Data flow

```text
module.json
  -> ModuleJsonSerializer
  -> ModuleValidator
  -> RuntimeModuleLoader
  -> ModuleRunner
  -> StepHandlerRegistry
  -> IStepHandler implementation
  -> RuntimeContext services
  -> UI / Audio / SceneBinding / Anchors / Variables / Interaction
```

## Authoring flow

```text
ModuleEditorWindow
  -> StepListView
  -> StepInspectorView
  -> LayoutInspectorView
  -> Asset/Object/Anchor list views
  -> ModuleJsonSerializer.SaveToFile
  -> ModulePreviewLauncher
```

The authoring window is an Editor script. It can use `UnityEditor` APIs and can open scenes or enter Play Mode.

## Runtime flow

1. `RuntimeModuleLoader` builds a path from Assets-relative, StreamingAssets-relative, or absolute-path mode.
2. The loader reads JSON and deserializes a `ModuleDocument`.
3. `ModuleValidator` validates the loaded module.
4. `ModuleRunner` executes steps in order.
5. For each step, `ModuleRunner` asks `StepHandlerRegistry` for the matching handler.
6. The handler performs work such as showing text, playing audio, moving an object, or displaying an MCQ.
7. Flow proceeds by default to the next list item, unless `nextStepId`, `onCorrectStepId`, or `onWrongStepId` overrides it.

## Current built-in/generic step handlers

| Step type | Runtime behavior |
|---|---|
| `text` | Show a text panel. |
| `image` | Show an image panel from a module asset. |
| `audio` | Play an audio asset. |
| `wait` | Wait for `durationSeconds`. |
| `showObject` | Show or hide a scene-bound object. |
| `moveObject` | Move a scene-bound object. |
| `mcq` | Show a multiple-choice question and branch based on answer. |

## Current RoboticsLite domain steps

| Step type | Runtime behavior |
|---|---|
| `showFrame` | Show/hide a robot frame gizmo. |
| `rotateJoint` | Rotate a RobotLite joint. |
| `resetRobot` | Reset the RobotLite rig to home state. |

## Important current limitation

Adding a new step type currently touches too many places:

```text
Runtime/StepHandlers/<NewStepHandler>.cs
Runtime/StepHandlers/StepHandlerRegistry.cs
Core/Validation/ModuleValidator.cs
Authoring/Editor/StepListView.cs
Authoring/Editor/StepInspectorView.cs
Core/Templates/ModuleTemplateFactory.cs, if the template uses it
Tests/EditMode/*
Tests/PlayMode/*
```

Phase 2 addresses this with a Step Catalog and modular validation/inspector flow.

## Assembly definition notes

- `MRModuleEditor.Core` contains models, serializer, templates, and validation.
- `MRModuleEditor.Runtime` references Core and contains runtime MonoBehaviours and handlers.
- `MRModuleEditor.Authoring.Editor` is Editor-only and references Core/Runtime.
- `MRModuleEditor.Domains.RoboticsLite` references Core/Runtime and contains domain code.

Rule of thumb:

```text
Core should not depend on Runtime, Authoring, or Domains.
Runtime should not depend on UnityEditor.
Authoring/Editor may depend on UnityEditor.
Domains may depend on Core/Runtime.
```

