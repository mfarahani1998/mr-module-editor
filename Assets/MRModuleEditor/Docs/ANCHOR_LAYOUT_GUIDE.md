# Anchor and Layout Guide

Phase 5 makes anchors and layouts more explicit without committing to persistent spatial anchors yet. Treat these fields as authoring/runtime hints that keep simulator, desktop, and headset demos predictable.

## Anchor fields

| Field | Use |
|---|---|
| `id` | Stable module ID, such as `anchor.head.default`. |
| `type` | `head`, `world`, or `object`. |
| `provider` | Leave blank or use `simulator` for the built-in resolver. Use `manual` only when the scene has a `ManualAnchorProvider`. `marker` and `spatial` are reserved for later providers. |
| `displayName` | Human-readable editor label. |
| `targetObjectId` | Required only for object anchors. |
| `fallbackAnchorId` | A safer anchor to use if this anchor cannot resolve. Calibration-required anchors should usually fall back to the head anchor. |
| `calibrationRequired` | `true` when the learner or demo operator must recenter, place, scan, or otherwise prepare the anchor. |
| `calibrationStatus` | Authoring/runtime status hint: `unknown`, `ready`, `approximate`, or `lost`. |
| `notes` | Authoring notes for setup, headset placement, or demo limitations. |

## Layout fields

| Field | Use |
|---|---|
| `targetId` | Step ID for panels/callouts, or object ID for bound scene objects. |
| `anchorId` | Anchor that supplies the coordinate frame. |
| `position` | Local offset from the anchor. |
| `rotationEuler` | Local rotation tweak. |
| `scale` | Spatial size. Keep all axes positive. |
| `faceUser` | Rotates the target toward the viewer. Usually good for panels and callouts, risky for 3D equipment objects. |
| `followMode` | `fixed`, `followAnchor`, or `smoothFollow`. |
| `visibilityRange` | Validation hint in meters; `0` means unlimited for now. |
| `readabilityProfile` | `headPanel`, `worldPanel`, `objectCallout`, or `worldObject`. |
| `deviceProfile` | Blank for any target, or `simulator`, `headset`, `desktop`. |

## Recommended presets

Start with these combinations:

| Situation | Anchor | `faceUser` | `followMode` | `readabilityProfile` |
|---|---|---:|---|---|
| Head instruction panel | `head` | true | `smoothFollow` | `headPanel` |
| World narration panel | `world` | true | `fixed` | `worldPanel` |
| Object-attached callout | `object` | true | `followAnchor` | `objectCallout` |
| 3D object placement | `world` | false | `fixed` | `worldObject` |

## Calibration convention

For demos, keep a `world` anchor named something like `anchor.world.table` with `calibrationRequired: true` and `fallbackAnchorId: "anchor.head.default"`. The simulator recenter action can place the world origin in front of the learner. If the world provider fails, the fallback keeps the module previewable instead of hard-failing every layout.

## Common validation warnings

- `anchor.fallbackAnchorId.recommended`: add a fallback for calibration-required anchors.
- `layout.faceUser.recommended`: a world/object anchored panel may not face the learner.
- `layout.readability.visibilityRange`: the layout is farther from its anchor than the intended range.
- `layout.faceUser.objectTarget`: you are rotating a scene object toward the learner; make sure that is intentional.
