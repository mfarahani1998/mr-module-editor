# Known Limitations

This is a living list. Update it whenever a limitation is discovered or fixed.

## Phase 1 baseline limitations

### Step extension is too manual

Adding a new step type currently requires edits in runtime, validation, editor UI, defaults, templates, and tests. This is the main reason Phase 2 exists.

### RoboticsLite appears in platform core/editor

The current sample/domain steps are useful, but RoboticsLite step names are still known by platform validation/editor files. Phase 2 should move this toward catalog/domain registration.

### Parameter schema is implicit

`ModuleStep.parameters` is flexible, but parameter meaning is spread across handlers, validators, and editor UI. Phase 2 should preserve the JSON shape while centralizing metadata.

### Authoring UI is functional but still demo-level

The editor can create/load/save steps and validate, but asset import/copy, object layout editing, preview-from-selected-step, grouped validation UX, and storyboard features are later phases.

### Asset paths are still manual

Current assets use relative paths inside the module folder. Asset import/copy workflow is a Phase 4 target.

### Object layouts are not fully first-class in authoring

Step layouts exist, but object layout editing needs more authoring support later.

### Anchor/layout system is MVP-level

Head/world/object anchors exist, but provider state, calibration status, readability checks, follow modes, and device profiles are future work.

### Runtime service lookup is still demo-friendly

Some runtime services are discovered through scene lookups. This is fine for MVP, but a `RuntimeServices` container may be useful later.

### Current documentation is new

Phase 1 docs are first-pass handoff docs. They should be updated during every later phase.

