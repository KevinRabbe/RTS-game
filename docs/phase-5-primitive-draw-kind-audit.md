# Phase 5 Primitive Draw Kind Audit

## Scope

This slice moves raw Godot primitive kind classification into a presentation-only bridge helper.

`GodotPrimitiveDrawKindResolver` consumes `GodotPrimitiveDto` data and returns a coarse draw kind for the Godot script.

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- No Godot API types are used by the resolver.
- The Godot script no longer switches directly on raw DTO kind numbers.

## Draw Kind Contract

The resolver maps:

- Unit primitive -> unit draw kind.
- Building and wall primitives -> building draw kind.
- Trade route primitive -> trade route draw kind.
- Health bar primitive -> health bar draw kind.
- Fog primitive -> fog overlay draw kind.
- Resource primitives -> resource draw kind.
- Unknown primitive kinds -> none.

## Verification

Tests cover every known primitive kind and unknown-kind fallback.
