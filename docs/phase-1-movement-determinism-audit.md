# Phase 1 Movement Determinism Audit

Date: 2026-05-09

This audit records the deterministic movement state after Phase 1.1 tile occupancy resolution and ChaosV2 spatial stress.

## Current Movement Contract

Movement is fixed tick and command-driven.

Current Phase 1.1 rules:

- Unit positions use fixed-point values.
- Tile occupancy uses deterministic fixed-point floor conversion.
- Wall blockers are deterministic static blockers.
- Units cannot enter wall-blocked positions.
- Units cannot enter currently occupied unit tiles.
- Multiple moving units attempting the same empty tile all fail.
- Tile swaps fail.
- No pushing.
- No sliding.
- No alternate routing.
- No pathfinding.
- Movement decisions are resolved in a two-pass batch:
  - Build movement intents.
  - Reject conflicts.
  - Apply successful moves in stable unit list order.

## State Fields

Movement-relevant mutable unit state:

- `Position`
- `HasMoveTarget`
- `MoveTarget`
- `LastMovedTick`

All listed fields are included in `StateChecksum`.

## Determinism Audit

Confirmed:

- No `float` or `double` gameplay math in simulation.
- No async, task, thread, wall-clock, or GUID usage in simulation.
- No physics engine usage.
- Movement uses fixed-point arithmetic.
- Movement conflict resolution uses list iteration, not unordered collection iteration.
- Entity lookup dictionary is not used for movement iteration.
- Command buffer dictionary cleanup sorts tick keys before removal.
- No hidden mutable state is stored inside `MovementSystem`.
- Movement tie-breakers avoid gameplay priority. Conflicting movers all fail.

## Stress Coverage

Focused unit tests cover:

- Unit blocked by wall.
- Unit blocked by occupied tile.
- Two units attempting the same tile.
- Three units attempting the same tile.
- Tile swap failure.
- Unit death later in the same tick still blocks movement.
- Wall destruction later in the same tick still blocks movement.
- Replay determinism.
- Two-peer lockstep determinism.

Frozen stress coverage:

- `ChaosV1` validates macro lifecycle stability after movement changes.
- `ChaosV2` validates spatial congestion and tile occupancy.

Long-run gates passed:

- `ChaosV1` 10,000 ticks, seed `77`, checksum `12757226426589041677`.
- `ChaosV2` 10,000 ticks, seed `78`, checksum `2125748326682007096`.

## Known Limits

These are intentional Phase 1.1 limits:

- No pathfinding.
- No congestion priority.
- No dynamic unit pushing.
- No sliding around blockers.
- No alternate route selection.
- No flow fields.
- No diagonal occupancy rules beyond current tile floor checks.

## Expansion Gate

Before adding area damage or pathfinding:

1. Run full tests.
2. Run ChaosV1 at 5,000 ticks.
3. Run ChaosV2 at 5,000 ticks.
4. For area damage specifically, rerun ChaosV1 and ChaosV2 at 10,000 ticks.
5. If movement semantics change intentionally, create a new frozen scenario instead of weakening ChaosV2.
