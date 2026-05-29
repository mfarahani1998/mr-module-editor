# Troubleshooting

## Compile errors

Unity editor tools will not work correctly while C# compile errors exist.

Check:

1. Console red errors.
2. Missing `using` statements.
3. Scripts placed in the wrong assembly/folder.
4. Editor-only code using `UnityEditor` outside an `Editor` folder.

## Module JSON file missing

Symptom:

```text
Module JSON file not found
```

Check:

- Path selected in the authoring window.
- `RuntimeModuleLoader` load mode.
- Whether you are using Assets-relative, StreamingAssets-relative, or absolute path.

## Sample media missing

Symptom:

- image does not display,
- audio does not play,
- export reports missing referenced assets.

Check the module folder contains:

```text
assets/images/intro.png
assets/audio/intro.mp3
```

The curated source bundle may omit binary media; the real local project should include it.

## Unknown step type

Symptom:

```text
[Error] step.unknownType: Unknown step type '...'
```

Known types come from `StepCatalog` registrations plus runtime handlers.

Known baseline step types:

```text
text, image, audio, wait, confirm, setVariable,
showObject, highlightObject, showCallout, moveObject, mcq,
showFrame, rotateJoint, resetRobot
```

`confirm` is also the interaction-gated acknowledgement step; there is no separate wait-for-signal step type.

## Object binding missing

Symptom:

- object does not show,
- object does not move,
- RoboticsLite handler cannot find rig/object.

Check:

1. The module object has a `bindingKey`.
2. The scene has a `BindableObject` with the same binding key.
3. For the sample, the expected key is `RobotPreview`.

## MCQ does not branch as expected

Check:

- `correctIndex` is inside the `choices` array.
- `onCorrectStepId` exists.
- `onWrongStepId` exists.
- The MCQ panel is receiving interaction/select events.

## Audio step never finishes

Check:

```json
"waitForCompletion": true,
"loop": true
```

This combination is invalid because looping audio never completes. Validation should report it.

## Panel too far, too small, or behind the user

Check the matching `LayoutDefinition`:

- `anchorId`,
- `position`,
- `rotationEuler`,
- `scale`.

Also check whether the panel is using a fallback `anchorId` from step parameters instead of a layout.

## StreamingAssets headset path problem

Use:

```text
MR Module Editor > Export > Export Current Module To StreamingAssets
```

Then copy the reported StreamingAssets-relative path into the relevant loader/build setup.

