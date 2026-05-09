# ChaosV1 Invariant Contract

`ChaosV1` is the frozen golden stress scenario for the deterministic RTS foundation.

- Scenario name: `chaos-v1`
- Scenario version: `1`
- Default command: `run-stress --scenario chaos-v1 --ticks 5000 --seed 77`
- Player count: 6

## Version Rule

`ChaosV1` must not be silently changed.

If a future intentional change alters the scripted schedule, prepared initial state, invariant contract, final expected behavior, or stress semantics, increment `ChaosV1Scenario.Version`.

If a new system needs new stress coverage, prefer adding `ChaosV2` instead of weakening `ChaosV1`.

## What ChaosV1 Proves

ChaosV1 is an engine-contract test, not gameplay tuning.

It proves:

- Deterministic fixed tick advancement under long simulation.
- Stable command scheduling with exact tick-indexed commands.
- Stable command rejection across lockstep peers.
- Deterministic system ordering from validation through checksum.
- DeathMark correctness before capital, elimination, ranking, cleanup, and checksum.
- Capital loss ordering under siege pressure.
- Population lifecycle integrity for trained units, dead units, and destroyed training queues.
- Ranking ordering when multiple players resign or are defeated near one another.
- Cleanup stability after same-tick deaths and entity removals.
- Entity lookup consistency after swap-remove cleanup.
- Trade route determinism, trip income, and endpoint invalidation.
- Elimination and match-end ordering.
- Despawn timing compatibility with resignation and cleanup.
- Replay agreement with lockstep final checksum.

## Required Final Invariants

At the end of a passing ChaosV1 run:

- No lockstep desync reports exist.
- Replay/single-run checksum equals lockstep checksum.
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

Before adding nonlinear combat, path blocking, congestion, or advanced trade/path mechanics:

1. Run ChaosV1 at 5000 ticks.
2. Confirm `desync_count=0`.
3. Confirm `invariant_failures=0`.
4. If a change intentionally alters the contract, increment the version or create a new scenario.
