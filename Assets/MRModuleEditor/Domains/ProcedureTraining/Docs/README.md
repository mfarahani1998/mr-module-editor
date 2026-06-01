# ProcedureTraining Domain Pack

ProcedureTraining is the Phase 6 proof domain. It demonstrates how to add domain-specific lesson behavior without adding new step names to platform core validation, runtime, or authoring code.

## Step types

| Step type | Purpose | Main parameters |
|---|---|---|
| `showProcedureItem` | Reveals a bound procedure object, records the current item, and optionally highlights it. | `objectId`, `itemId`, `instruction`, `highlight`, `resultVariableKey` |
| `checkSafetyPoint` | Shows a learner-paced safety checkpoint and records a safety status. | `objectId`, `safetyPointId`, `prompt`, `status`, `autoCompleteAfterSeconds`, `resultVariableKey` |

Both steps are registered through `ProcedureTrainingStepDefinitions`, so the editor add menu and generic inspector can discover them from `StepCatalog.Global`.

## Runtime setup

Add `ProcedureTrainingStepInstaller` to the same runtime scene that contains `ModuleRunner`, or to a nearby services GameObject. The installer registers the two runtime handlers with the active runner.

The sample expects a scene object with:

```text
BindableObject.BindingKey = Equipment Demo
```

If the object does not already have a `ProcedureItemMarker`, the domain resolver adds one at runtime so the demo can run incrementally with the existing equipment sample object.

## Sample module

Open:

```text
Assets/MRModuleEditor/Domains/ProcedureTraining/Samples/EquipmentSafetyProcedureMini/module.json
```

The sample mixes generic and domain-specific steps:

```text
text
showProcedureItem
checkSafetyPoint
showCallout
mcq
text
```

That mix is intentional: the platform keeps generic teaching primitives reusable, while the domain pack adds procedure-specific meaning and result recording.

## Tests

Domain tests live inside this folder so the main platform tests do not need to know ProcedureTraining step names.

```text
Assets/MRModuleEditor/Domains/ProcedureTraining/Tests/EditMode
Assets/MRModuleEditor/Domains/ProcedureTraining/Tests/PlayMode
```

The EditMode tests cover catalog registration, validation, and core isolation. The PlayMode tests cover runtime side effects for both domain steps.
