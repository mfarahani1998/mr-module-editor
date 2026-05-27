# Adding Step Types with Step Catalog v0.2

Step types are still saved as plain strings in `ModuleStep.type`, and parameters still live in `ModuleStep.parameters`. Phase 2 adds a catalog so the editor and validator can learn about a step from one definition instead of many hard-coded lists.

## Generic built-in step workflow

1. Create a runtime handler in `Assets/MRModuleEditor/Runtime/StepHandlers/` implementing `IStepHandler`.
2. Register the handler in `BuiltInStepInstaller.RegisterHandlers`.
3. Add a `StepTypeDefinition` in `BuiltInStepDefinitions.Register`.
4. Use `StepParameterDefinition` entries for fields the editor and validator can draw/check generically.
5. Add a custom validator only when generic checks are not enough.
6. Add EditMode validation/default tests and PlayMode runtime tests.
7. Update the step reference or docs.

## Domain-specific step workflow

1. Keep the step handler under `Assets/MRModuleEditor/Domains/<DomainName>/`.
2. Keep the step definition under the same domain folder.
3. Register the definition with `StepCatalog.Global` from a domain definition class.
4. Register runtime handlers from a domain installer component.
5. Do not add the domain step type string to platform core/editor files.

## Minimal example

```csharp
catalog.Register(new StepTypeDefinition(
    "confirm",
    "Confirm",
    "Flow",
    "Shows a learner-paced acknowledgement step.",
    true,
    0f,
    new[]
    {
        new StepParameterDefinition("message", "Message", StepParameterKind.MultilineString, true, "Continue when ready."),
        new StepParameterDefinition("buttonLabel", "Button Label", StepParameterKind.String, false, "Continue")
    }));
```

The add menu, type popup, defaults, generic fields, and basic validation all come from this definition.
