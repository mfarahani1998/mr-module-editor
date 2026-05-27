# ADR 0002 — Step Type Catalog Direction

## Status

Planned for Phase 2.

## Context

The current MVP has working step types, but step metadata is duplicated across validation, editor UI, defaults, templates, and runtime registration.

## Decision

Do not jump directly to a heavy plugin system. Add a small local Step Catalog in Phase 2.

The catalog should map:

```text
step type string
  -> display metadata
  -> parameter schema/defaults
  -> validation rules
  -> optional editor drawer
  -> runtime handler registration pattern
```

## Why

- This solves the immediate maintenance problem.
- It keeps the persistent module JSON stable.
- It supports future domains without forcing the platform core to know every domain step name.
- It is easier to debug than reflection-heavy plugin discovery.

## Phase 1 implication

Phase 1 should document the current pain honestly but should not implement the catalog yet.

