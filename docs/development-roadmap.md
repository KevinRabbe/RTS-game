# Development Roadmap

## Current Completion Status

Completed tracks:

- Deterministic core + lockstep/replay baseline
- Economy/movement/gather prototype stability
- Movement/Gather V2 default-on rollout
- Local playable shell + HUD foundation + HUD UX polish
- RtsClientRoot presentation split
- Test-suite/harness structural cleanup
- Architecture/load audit and contracts (8C)
- Scalable combat hotspot architecture plan (9A.0)
- Combat implementation baseline complete:
  - 9A.1 explicit target attack
  - 9A.2 move-into-range attack-slot foundation
  - 9A.3 hotspot pressure scenarios
  - 9A.4 combat HUD/debug status
- Combat lab scenarios complete:
  - 9B.1 deterministic CombatTest01 (F2)
  - 9B.2 enemy building target in CombatTest01
  - 9B.3 combat scenario UX polish

Current gate status:

- `tests=489/489 PASS`
- `chaos-v4` 5000-tick stress: pass

## Active Stage

Phase 9C.0 complete: attack-move/auto-target architecture documented.

Next active implementation stage:

- **9C.1 AttackMoveCommand + state only (no auto-target yet)**

## Near-Term Combat Sequence (9C)

1. **9C.1** AttackMoveCommand + state only
2. **9C.2** bounded indexed target acquisition
3. **9C.3** resume-after-kill/invalidation behavior
4. **9C.4** attack-move pressure scenarios
5. **9C.5** attack-move HUD/command feedback

Guardrails:

- Keep explicit `AttackCommand` and `AttackMoveCommand` semantics separate.
- Keep combat pathing owned by movement (no combat pathfinding bypass).
- No O(units x enemies) per tick loops.
- No unbounded acquisition/retarget churn in hotspot fights.

## Full-Game Target Envelope

- 6-player FFA
- 200+ pop per player
- ~1200 active units
- up to ~720 villagers

## Delivery Rules

- No feature slice without deterministic scenario coverage.
- No simulation semantics change without full gate pass.
- No architecture bypass around command/lockstep/replay contracts.
- Prefer additive upgrades over rewrites.

## Validation Gate (Always)

1. `dotnet build GodotClient\RtsGame.GodotClient.csproj --no-restore`
2. `dotnet build tests\RtsGame.Tests.csproj --no-restore`
3. `dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --fail-fast`
4. `dotnet run --project src\tools\Headless\RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v4 --ticks 5000 --seed 77`

## References

- Current slice state: [current-vertical-slice.md](current-vertical-slice.md)
- Architecture contracts: [prototype-architecture.md](prototype-architecture.md)
- High-pop policy: [high-pop-simulation-architecture.md](high-pop-simulation-architecture.md)
- Load risk classification: [architecture-load-audit.md](architecture-load-audit.md)
- Combat scale plan: [combat-architecture.md](combat-architecture.md)
