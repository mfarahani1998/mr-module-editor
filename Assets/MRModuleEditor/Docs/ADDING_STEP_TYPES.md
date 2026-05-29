# Adding Step Types with Step Catalog v0.2

Step types are saved as plain strings in `ModuleStep.type`, and parameters live in `ModuleStep.parameters`. The Step Catalog keeps editor UI, defaults, and validation aligned from one definition instead of many hard-coded lists.

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
        new StepParameterDefinition("buttonLabel", "Button Label", StepParameterKind.String, false, "Continue"),
        new StepParameterDefinition("completeOnSignal", "Complete On Interaction Signal", StepParameterKind.Bool, false, false),
        new StepParameterDefinition("signalTargetId", "Signal Target ID", StepParameterKind.String, false, "{stepId}.confirm")
            .VisibleWhenBool("completeOnSignal", true)
    }));
```

The add menu, type popup, defaults, generic fields, and basic validation all come from this definition.

## Avoid near-duplicate gates

Before adding a new flow/gating step, check whether `confirm` should be extended instead. `confirm` is the single built-in learner acknowledgement gate; its optional signal-completion parameters cover advanced interaction-gated confirmations without introducing a second author-facing step type.
