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
| `confirm` | Show a learner-paced confirmation prompt. Can optionally complete from a filtered `InteractionContext` signal. |
| `setVariable` | Write a value into `RuntimeVariableStore`. |
| `showObject` | Show or hide a scene-bound object. |
| `highlightObject` | Apply reversible visual emphasis to a scene-bound object. |
| `showCallout` | Show a small spatial text callout. |
| `moveObject` | Move a scene-bound object. |
| `mcq` | Show a multiple-choice question and branch based on answer. |

## Current RoboticsLite domain steps

| Step type | Runtime behavior |
|---|---|
| `showFrame` | Show/hide a robot frame gizmo. |
| `rotateJoint` | Rotate a RobotLite joint. |
| `resetRobot` | Reset the RobotLite rig to home state. |

## Important current limitation

New built-in steps should be added through the Step Catalog: define one `StepTypeDefinition`, register one runtime handler, then add tests/docs. The editor add menu, type popup, defaults, and most validation flow from the catalog instead of separate hard-coded lists.

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

