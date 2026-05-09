# Phase 3 Pathfinding Audit

Status: Green

## Added System Surface

`DeterministicPathfinder` provides minimal grid A* for movement.

No new mutable `GameState` fields were added.

## Determinism Rules

- Grid based only.
- Cardinal movement only.
- No smoothing.
- No physics.
- No flow fields.
- No pushing or sliding.
- Neighbor order is frozen: East, South, West, North.
- Open-set selection uses:
  1. Lowest total cost.
  2. Lowest heuristic.
  3. Lowest tile index.
- Wall checks use simulation `SpatialRules`.

## Movement Integration

`MovementSystem` keeps the existing command-owned `MoveTarget`.

Each tick:

1. Resolve current tile.
2. Resolve target tile.
3. Ask pathfinder for the next path tile when current and target differ.
4. Move toward that tile using fixed-point unit speed.
5. Preserve existing deterministic occupancy conflict rules.

## Guardrails

- No path cache yet.
- No alternate routing state.
- No dynamic priority model.
- No unit-aware A* yet; unit occupancy is still resolved by `MovementSystem`.
- Cleanup remains unchanged.

## Verification Targets

- Same start/end returns same path.
- Wall blocks target tile.
- Destroyed wall opens path after cleanup.
- No path returns deterministic failure.
- Multiple same-tick movement requests remain deterministic.
- ChaosV4 protects long-run pathfinding drift.

## Verification

- Full test suite: 121 tests, 0 failures.
- ChaosV1 5000 ticks: no desync, no invariant failures.
- ChaosV2 5000 ticks: no desync, no invariant failures.
- ChaosV3 5000 ticks: no desync, no invariant failures.
- ChaosV4 5000 ticks: no desync, no invariant failures.
