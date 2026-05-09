# Phase 5 Selection Router Audit

## Scope

This slice adds a presentation-only selection router for the Godot bridge.

The router lives in `src/presentation/GodotBridge` and consumes only `GodotFrameDto` primitive data. It returns a simple selection result for the Godot script to reflect in local UI state.

## Selection Contract

Left-click priority is deterministic and explicit:

1. Local unit under cursor -> unit selection.
2. Local building or wall under cursor -> building selection.
3. Empty cursor position -> no selection.

Enemy and neutral primitives are ignored for local selection.

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- No Godot API types are used by the router.
- Selection remains local presentation state only.
- Simulation validation remains authoritative for all commands.

## Determinism Notes

The router is outside deterministic simulation. It is kept stable for presentation consistency:

- Primitive iteration follows DTO array order.
- Hit tests use fixed raw integer coordinates.
- Priority is fixed: unit before building before none.

## Verification

Tests cover:

- Local unit priority over overlapping local building primitives.
- Local building selection.
- Enemy primitive rejection.
- Empty-position no-selection fallback.
