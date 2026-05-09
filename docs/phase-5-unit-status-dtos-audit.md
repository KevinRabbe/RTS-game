# Phase 5.13 Unit Status DTOs Audit

## Purpose

Expose selected-unit work, movement, combat, and carry state to the presentation client.

This is read-only UI/debug data. It does not add unit behavior or validation to Godot.

## Added Snapshot Data

`UnitSnapshot` now includes:

- Move target state
- Build target id
- Resource node target id
- Carried resource type and amount
- Attack target id
- Attack cooldown ticks
- Existing trade route ids

## Added Godot DTO Data

`GodotFrameDto` now includes:

- `GodotUnitStatusDto[] UnitStatuses`

Each status is keyed by unit id, so UI code can pair selected unit primitives with their current authoritative simulation state.

## Godot Shell Update

When a local unit is selected, the debug HUD can show:

- Gathering target and carried amount
- Build target
- Attack target and cooldown
- Moving state
- Carried amount

This is display only.

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

- Snapshot exposes unit gather/carry state.
- Godot DTO exposes unit type, gather target, carried resource type, and carried amount.
- Existing snapshot/visual checksum immutability tests still cover the expanded presentation path.
