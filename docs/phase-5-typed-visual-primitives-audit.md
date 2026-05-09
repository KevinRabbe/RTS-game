# Phase 5.11 Typed Visual Primitives Audit

## Purpose

Expose stable type ids on visual primitives and Godot DTOs so the presentation client can distinguish unit, building, and resource types without guessing from colors or entity ids.

This is presentation metadata only.

## Added Data

`VisualPrimitive` now includes:

- `TypeId`

Meaning depends on primitive kind:

- Unit primitives: `UnitTypeId`
- Building and wall primitives: `BuildingTypeId`
- Resource primitives: `ResourceType`
- Health bars, fog, and trade-route lines: `0` or the source unit type where useful for drawing

`GodotPrimitiveDto` mirrors the same `TypeId`.

## Godot Shell Update

The Godot visual shell now colors local unit primitives by type id:

- Villagers
- Scouts
- Infantry
- Cavalry
- Generic fallback for other unit types

This is visual styling only. It does not affect simulation.

## Layer Boundary

- Simulation behavior changed: no
- GameState schema changed: no
- Tick order changed: no
- Checksum changed: no
- Lockstep code changed: no
- Replay code changed: no
- Godot gameplay authority added: no

## Regression Coverage

Tests cover:

- Unit visual primitives expose `UnitTypeId`.
- Building visual primitives expose `BuildingTypeId`.
- Resource visual primitives expose `ResourceType`.
- Godot DTOs expose the same type ids.

Existing snapshot/visual checksum immutability tests continue to cover this path.
