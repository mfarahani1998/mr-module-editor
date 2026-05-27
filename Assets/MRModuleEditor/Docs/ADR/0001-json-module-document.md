# ADR 0001 — JSON Module Document

## Status

Accepted for MVP baseline.

## Context

The project needs modules that can be saved, loaded, exported, and eventually packaged. The current MVP uses a `module.json` file mapped to `ModuleDocument`.

## Decision

Use a JSON-backed module document with this top-level shape:

```text
ModuleDocument
├─ schemaVersion
├─ moduleId
├─ title
├─ description
├─ author
├─ estimatedDurationSeconds
├─ assets[]
├─ objects[]
├─ anchors[]
├─ layouts[]
└─ steps[]
```

Step-specific data lives in:

```csharp
Dictionary<string, JToken> parameters
```

## Why

- JSON is easy to inspect and edit while the editor is still early.
- JSON is portable for export/import.
- Flexible parameters avoid C# polymorphic serialization complexity in the MVP.
- The format supports runtime loading.

## Tradeoffs

- Parameter schema is implicit until Phase 2.
- Mistyped parameter names can silently fail unless validation catches them.
- Future schema versions need migration support.

## Follow-up

Phase 2 should centralize step parameter metadata through a Step Catalog while preserving the current JSON shape.

