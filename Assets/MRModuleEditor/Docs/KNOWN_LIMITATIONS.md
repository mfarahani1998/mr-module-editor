# Known Limitations

This is a living list. Update it whenever a limitation is discovered or fixed.

## Phase 1 baseline limitations

### Step extension still requires runtime registration

Step metadata is centralized in the Step Catalog, so editor add buttons, defaults, and generic validation no longer need separate hard-coded lists. A new executable step still needs a matching runtime handler registration and tests.

### Domain packs still need installer discipline

The RoboticsLite sample now lives behind domain step definitions/installers, but new domain packs should keep their type definitions and handlers out of platform core unless the behavior is truly generic.

### Parameter schema is catalog-driven but still flexible

`ModuleStep.parameters` remains a flexible dictionary. Built-in and domain catalog definitions describe expected fields, but advanced/custom parameters are still possible and require clear docs/tests.

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

