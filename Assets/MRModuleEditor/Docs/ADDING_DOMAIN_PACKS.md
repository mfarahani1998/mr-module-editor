# Adding Domain Packs

A domain pack should add specialized behavior without making the platform core domain-specific.

## Folder shape

```text
Assets/MRModuleEditor/Domains/<DomainName>/
├─ <DomainName>StepDefinitions.cs
├─ <DomainName>StepInstaller.cs
├─ Runtime handlers
├─ Authoring helpers, if needed later
├─ Samples
├─ Tests
└─ Docs
```

## Rules

- Core validation must not hard-code domain step type names.
- Platform editor add menus and inspectors should read domain steps from `StepCatalog.Global`.
- Domain-specific validation belongs in the domain definition class through a custom validator delegate.
- Domain-specific runtime handlers are registered by a domain installer component in scenes that need that domain.
- Generic parameters should use `StepParameterDefinition` before adding custom editor drawers.

## RoboticsLite example

`RoboticsLiteStepDefinitions` registers `showFrame`, `rotateJoint`, and `resetRobot`. `RoboticsLiteStepInstaller` registers the matching runtime handlers with `ModuleRunner`.

That keeps RoboticsLite usable as a sample/domain while preventing future phases from becoming robotics-centered.

## ProcedureTraining example

`ProcedureTrainingStepDefinitions` registers `showProcedureItem` and `checkSafetyPoint`. `ProcedureTrainingStepInstaller` registers the matching runtime handlers with `ModuleRunner`.

Use this as the preferred Phase 6 pattern for future non-robotics domains:

```text
Assets/MRModuleEditor/Domains/ProcedureTraining/
├─ ProcedureTrainingStepDefinitions.cs
├─ ProcedureTrainingStepInstaller.cs
├─ ProcedureTrainingModuleFactory.cs
├─ Runtime/
│  ├─ ProcedureItemMarker.cs
│  ├─ ProcedureTrainingResolver.cs
│  ├─ ShowProcedureItemStepHandler.cs
│  └─ CheckSafetyPointStepHandler.cs
├─ Samples/EquipmentSafetyProcedureMini/module.json
├─ Tests/EditMode/
├─ Tests/PlayMode/
└─ Docs/README.md
```

Scene setup rule: add `ProcedureTrainingStepInstaller` to a services GameObject in any runtime preview scene that needs to run ProcedureTraining modules. Do not register these handlers from built-in runtime code, because that would make the platform core aware of this domain.
