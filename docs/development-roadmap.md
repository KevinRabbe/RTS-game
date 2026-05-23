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

Current gate status:

- `tests=472/472 PASS`
- `chaos-v4` 5000-tick stress: pass

## Active Stage

Phase 9A.0 complete: scalable combat hotspot architecture documented.

Next active implementation stage:

- **9A.1 Explicit Attack Target Slice**

## Near-Term Combat Sequence (9A)

1. **9A.1** Explicit attack target slice
2. **9A.2** Attack slot / attack ring system
3. **9A.3** Combat hotspot pressure scenarios
4. **9A.4** Combat HUD debug/status

Guardrails:

- Keep attack-move and auto-acquire out of first combat slice.
- Keep combat pathing owned by movement (no combat pathfinding bypass).
- No O(units x enemies) per tick loops.

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
