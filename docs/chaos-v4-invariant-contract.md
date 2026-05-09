# ChaosV4 Invariant Contract

Version: 1

ChaosV4 is the frozen pathfinding stress scenario. It protects deterministic grid pathfinding from drift under repeated requests, wall blockers, route changes, and lockstep replay pressure.

## Scope

- 6-player FFA lockstep.
- 1200+ tick smoke coverage in tests.
- 5000+ tick headless coverage before merging pathfinding changes.
- Maze-like neutral wall barrier.
- Repeated movement requests across blocked space.
- Siege destroys a wall and opens a route.
- Multiple units request the same clogged tile.
- Trade carts receive route and movement commands during the same scenario.

## Invariants

- ScenarioVersion remains `1` unless a new golden contract is intentionally created.
- Pathfinding is grid-based.
- Neighbor order is East, South, West, North.
- Open-set tie-breakers are lowest total cost, lowest heuristic, then lowest tile index.
- No smoothing, physics, pushing, sliding, or random target selection is used.
- Destroyed walls affect pathfinding only after the normal tick order reaches cleanup.
- Replay checksum matches lockstep checksum.
- Entity lookup, population, ownership, and tile occupancy invariants remain valid.
- Final match result finishes with player 5 as winner.

## Frozen Behavior

Do not edit ChaosV4 to accommodate future flow fields, formation movement, dynamic avoidance, or advanced path caches. Add ChaosV5 for those mechanics.
