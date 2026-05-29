# Adding Step Types — Current Workflow Before Step Catalog

This document is historical. The project now uses the Step Catalog flow described in `ADDING_STEP_TYPES.md`; keep this only as context for why the catalog exists.

## Current rule

A step type is identified by a string:

```json
"type": "text"
```

The same string must be known by runtime, validation, authoring UI, defaults, templates, and tests.

## Current step type list

```text
text
image
audio
wait
showObject
moveObject
mcq
showFrame
rotateJoint
resetRobot
```

## Files usually touched for a new generic step

```text
Assets/MRModuleEditor/Runtime/StepHandlers/<NewStepHandler>.cs
Assets/MRModuleEditor/Runtime/StepHandlers/StepHandlerRegistry.cs
Assets/MRModuleEditor/Core/Validation/ModuleValidator.cs
Assets/MRModuleEditor/Authoring/Editor/StepListView.cs
Assets/MRModuleEditor/Authoring/Editor/StepInspectorView.cs
Assets/MRModuleEditor/Core/Templates/ModuleTemplateFactory.cs, if templates use the step
Assets/MRModuleEditor/Tests/EditMode/*
Assets/MRModuleEditor/Tests/PlayMode/*
```

## Why this is a problem

Adding a step in many files is error-prone.

Examples:

- Runtime can know a handler, but validation can still reject the step as unknown.
- The editor can add a step, but the inspector might not draw its fields.
- The inspector can draw fields, but defaults might be missing.
- A domain step can accidentally become hard-coded in platform core/editor code.

## Current hard-coded places

### `StepListView.cs`

The add-step buttons are manually listed. Example:

```csharp
if (GUILayout.Button("Text")) selectedStepIndex = Add(addStep, document, "text");
if (GUILayout.Button("Image")) selectedStepIndex = Add(addStep, document, "image");
```

RoboticsLite buttons are also currently listed here.

### `StepInspectorView.cs`

The valid type list is hard-coded:

```csharp
private static readonly string[] StepTypes =
{
    "text",
    "image",
    "audio",
    "wait",
    "showObject",
    "moveObject",
    "mcq",
    "showFrame",
    "rotateJoint",
    "resetRobot"
};
```

Defaults and parameter fields are also implemented as `if/else` branches.

### `ModuleValidator.cs`

Known step types and per-step validation are hard-coded. RoboticsLite types currently appear in core validation.

## Temporary current workflow

Until Phase 2 is complete:

1. Add the runtime handler implementing `IStepHandler`.
2. Register the handler in the appropriate installer/registry.
3. Add the type string to `ModuleValidator`.
4. Add validation rules in `ModuleValidator`.
5. Add an add button in `StepListView`.
6. Add the type to `StepInspectorView.StepTypes`.
7. Add defaults in `StepInspectorView.EnsureDefaultsForType`.
8. Add inspector fields in `StepInspectorView.DrawSpecificFields`.
9. Add template usage only if needed.
10. Add EditMode tests.
11. Add PlayMode tests.
12. Update docs.

## Phase 2 target

After Step Catalog v0.2, a simple built-in step should require:

1. Add handler.
2. Add a `StepTypeDefinition`.
3. Add custom validator only if generic parameter validation is insufficient.
4. Add custom editor drawer only if generated fields are insufficient.
5. Add tests and docs.

