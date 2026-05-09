# Phase 5 Primitive Hit Test Audit

## Scope

This slice centralizes presentation-side primitive hit testing for the Godot bridge.

`GodotPrimitiveHitTest` consumes a `GodotPrimitiveDto` plus fixed raw cursor coordinates and returns whether the point is inside the primitive's square interaction bounds.

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- No Godot API types are used by the helper.
- Selection and right-click interaction routers now share the same integer hit-test behavior.

## Determinism Notes

The helper is outside deterministic simulation. It is kept stable for UI consistency:

- Uses fixed raw integer coordinates.
- Includes boundary points.
- Does not use floating point math.

## Verification

Tests cover:

- Boundary inclusion.
- Outside-boundary rejection.
