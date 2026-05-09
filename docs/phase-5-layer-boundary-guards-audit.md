# Phase 5 Layer Boundary Guards Audit

## Scope

This slice adds source-level regression guards for presentation and simulation boundaries.

The goal is to make architecture drift harder:

- Simulation source must not reference presentation namespaces.
- Simulation source must not reference Godot.
- Godot bridge helpers must not import or use Godot engine API types.
- The Godot client script must not reference simulation core types directly.
- The Godot client script must not switch on raw primitive kind values.

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- The new tests inspect source files only.
- The Godot client script remains the only place in this slice that uses Godot engine API types.
- The Godot client script remains a presentation shell over bridge DTOs and facade methods.

## Verification

Tests cover:

- Simulation project dependency direction.
- Simulation source namespace/API boundary.
- Godot bridge helper engine-API boundary.
- Godot client script simulation-boundary behavior.
- Godot client script raw primitive-kind guard.
