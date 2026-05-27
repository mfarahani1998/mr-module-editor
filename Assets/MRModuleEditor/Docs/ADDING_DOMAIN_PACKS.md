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
