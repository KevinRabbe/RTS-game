# Phase 5.4 Local 1v1 Session Audit

## Purpose

Add a presentation-side local play harness for early playable 1v1 testing before a real Godot client is wired in.

This is not simulation authority. It is a thin dev loop that owns a private `GameState`, accepts `ClientCommandIntent` values, converts them into normal command envelopes, advances through `TickRunner`, and exposes read-only snapshots and visual frames.

## Layer Boundary

- Location: `src/presentation/LocalPlay`
- Simulation behavior changed: no
- Tick order changed: no
- Checksum changed: no
- GameState schema changed: no
- Lockstep code changed: no
- Replay code changed: no

## Local Session Contract

`LocalPlaySession`:

- Creates a deterministic nomad 1v1 state from a match seed.
- Queues presentation intents for the current tick only.
- Uses `ClientCommandMapper` to create command envelopes.
- Tracks deterministic per-player command sequence numbers.
- Fills missing local player input with `NoOpCommand` before advancing.
- Advances only through `TickRunner.AdvanceOneTick`.
- Exposes `GameSnapshot` and `VisualFrame`.
- Keeps the owned `GameState` private.

## Determinism Notes

The local harness is intentionally not a multiplayer replacement. It exists to make the next Godot slice playable while preserving the same command-driven path used by replay and lockstep.

No client intent mutates simulation state until `AdvanceOneTick` runs the normal command validation and execution systems.

## Regression Coverage

Tests cover:

- Local tick advance with automatic no-op input fill.
- Queueing commands without pre-tick mutation.
- 1v1 capital placement and construction through client intents.
- Snapshot-to-visual-frame access through the local harness.
- Invalid local intent rejection through simulation validation.

## Next Safe Step

The Godot prototype can now depend on:

`Godot input -> ClientCommandIntent -> LocalPlaySession -> GameSnapshot -> VisualFrame`

This keeps Godot interactive while preserving the simulation boundary.
