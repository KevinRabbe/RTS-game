# Phase 5 Interaction Router Audit

## Scope

This slice adds a presentation-only right-click router for the Godot bridge.

The router lives in `src/presentation/GodotBridge` and consumes only `GodotFrameDto` primitive data. It returns a simple interaction intent for the Godot script to convert into facade calls.

## Routing Contract

Right-click priority is deterministic and explicit:

1. Enemy unit, building, or wall under cursor -> attack intent.
2. Visible food, wood, or gold resource under cursor -> gather intent.
3. Empty cursor position -> move intent.
4. No selected units -> no intent.

Friendly-owned and neutral-owned combat primitives are ignored as attack targets.

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- No Godot API types are used by the router.
- The Godot script still only queues command intents through `GodotClientFacade`.
- Simulation validation remains authoritative.

## Determinism Notes

The router is not part of deterministic simulation. Its job is client-side intent selection only.

The behavior is still kept stable for presentation consistency:

- Primitive iteration follows DTO array order.
- Hit tests use fixed raw integer coordinates.
- Priority is fixed: attack before gather before move.

## Verification

Tests cover:

- Attack priority over overlapping resource primitives.
- Resource gather routing.
- Empty-position move fallback.
- Friendly target rejection.
