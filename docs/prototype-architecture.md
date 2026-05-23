# Prototype Architecture

## Core Laws

1. Simulation is deterministic, engine-agnostic, command-driven.
2. Presentation is read-only over simulation outputs.
3. Networking transports commands only (lockstep), not authority snapshots.
4. Mutable gameplay state lives in `GameState` only.

## Layer Model

1. Simulation Core (`src/sim`)
2. Networking (`src/net`)
3. Presentation (`src/presentation`, `GodotClient`)
4. Tooling/Debug (`src/tools`, tests/harness)

No cross-layer mutation bypass is allowed.

## Simulation Invariants

- Fixed tick rate: 20 TPS
- Stateless systems + fixed order
- No engine/physics/deltaTime in sim
- No unordered iteration in deterministic decisions
- Cleanup-only entity removal (swap-remove in cleanup)
- Replay reconstructed from command stream
- Checksum used for lockstep desync detection

## Current Accepted Architecture State

- Movement + gather V2 are default-on.
- Legacy V1 toggles remain as rollback and parity controls.
- Worker loops are accepted as prototype-ready.
- Reservation, fallback, and no-progress policies are explicit and deterministic.
- `RtsClientRoot` split completed (orchestration focused).
- HUD foundation is read-only and presentation-only.
- Test suites/harness split completed.

## Service Boundaries (Must Keep)

- `ISpatialIndexService`
- `IPathQueryService`
- `ITrafficReservationService`
- `IMovementProgressPolicy`

Rules:

- no direct pathfinder bypass outside path query service,
- no direct reservation field writes outside reservation service (except explicit init paths),
- deterministic tie-break ordering in all conflict resolution.

## Operational Workflow

- Convert manual symptom -> deterministic scenario test first.
- Preserve regression scenarios permanently.
- Run full deterministic gate after each sim change.

## Prototype Readiness vs Full-Game Readiness

Prototype readiness: achieved for current slice reliability.
Full-game readiness: depends on remaining scalability and operational items documented in [architecture-load-audit.md](architecture-load-audit.md).
