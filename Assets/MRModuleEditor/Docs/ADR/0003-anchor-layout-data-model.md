# ADR 0003 — Anchor and Layout Data Model

## Status

Accepted for MVP baseline; planned for v0.2 evolution later.

## Context

The project needs MR-style placement without hard-coding every panel/object position in scenes.

## Decision

Represent anchors and layouts as data in `module.json`.

Current anchor model:

```text
AnchorDefinition
├─ id
├─ type              # head | world | object
└─ targetObjectId    # for object anchors
```

Current layout model:

```text
LayoutDefinition
├─ id
├─ targetId
├─ anchorId
├─ position
├─ rotationEuler
└─ scale
```

## Why

- Module authors can describe spatial placement outside the runtime scene.
- Runtime can resolve head/world/object placement consistently.
- Future layout presets and validation can build on the same data model.

## Tradeoffs

- Current anchors do not yet expose provider state, health, fallback, or calibration status.
- Current layouts do not yet include readability rules, follow modes, billboard modes, or device profiles.

## Follow-up

Phase 5 should add anchor provider/status/calibration concepts and stronger layout readability validation.

