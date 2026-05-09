# ChaosV2 Invariant Contract

`ChaosV2` is the frozen spatial congestion stress scenario for deterministic movement and blocking.

- Scenario name: `chaos-v2`
- Scenario version: `1`
- Default command: `run-stress --scenario chaos-v2 --ticks 5000 --seed 78`
- Player count: 6

## Version Rule

`ChaosV2` must not be silently changed.

If a future intentional change alters its scripted command schedule, prepared congestion state, spatial invariant contract, or final expected behavior, increment `ChaosV2Scenario.Version`.

If pathfinding or later movement systems need different coverage, add a new scenario instead of weakening ChaosV2.

## What ChaosV2 Proves

ChaosV2 is an engine-contract test for spatial truth.

It proves:

- Deterministic tile occupancy resolution.
- Stationary units holding occupied tiles.
- Movement failure into wall-blocked tiles.
- Repeated movement attempts through a narrow blocked choke.
- Same-tile movement conflicts resolving without priority advantage.
- Tile swap attempts resolving as failures.
- Same-tick movement before unit death remains deterministic.
- Same-tick movement before wall destruction remains deterministic.
- Trade can continue while congestion and blocking occur.
- Resignation during a congested scenario remains deterministic.
- Replay agreement with lockstep final checksum.

## Required Final Invariants

At the end of a passing ChaosV2 run:

- No lockstep desync reports exist.
- Replay/single-run checksum equals lockstep checksum.
- No two living units occupy the same tile.
- No living unit occupies a wall-blocked tile.
- Match result is finished.
- The scripted winner is player `5`.
- Ranking placement order contains all players.
- No player has negative resources.
- No player has negative population.
- Population used equals living owned units plus queued training population.
- No dead entities remain in unit or building lists.
- Entity lookup count equals unit count plus building count.
- Every entity lookup points to the correct kind and list index.
- Every unit and building owner is valid or neutral.
- Any active trade route references completed live trade posts.

## Expansion Rule

Before adding area damage, pathfinding, congestion priority, pushing, sliding, or alternate routing:

1. Run ChaosV1 at 5000 ticks.
2. Run ChaosV2 at 5000 ticks.
3. Confirm `desync_count=0` for both.
4. Confirm `invariant_failures=0` for both.
5. If a change intentionally alters the contract, increment the version or create a new scenario.
