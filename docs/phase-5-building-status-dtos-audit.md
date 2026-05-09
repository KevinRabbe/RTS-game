# Phase 5.12 Building Status DTOs Audit

## Purpose

Expose construction and training progress to the presentation client without giving the client gameplay authority.

This lets the Godot shell show whether a selected building is still under construction or actively training a unit.

## Added Snapshot Data

`BuildingSnapshot` now includes:

- Build progress ticks
- Required build ticks
- Training queue count
- Active training unit type id
- Active training progress ticks
- Active training required ticks

## Added Godot DTO Data

`GodotFrameDto` now includes:

- `GodotBuildingStatusDto[] BuildingStatuses`

Each status is keyed by building id, so UI code can pair a selected building primitive with its construction/training state.

## Godot Shell Update

When a local building is selected, the debug HUD can show:

- `Build current/required`
- `Train unitType current/required`

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

- Snapshot exposes under-construction progress.
- Snapshot exposes required construction ticks.
- Godot DTO exposes active training queue state.
- Existing snapshot/visual checksum immutability tests still cover the expanded presentation path.
