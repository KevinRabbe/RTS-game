# Development Roadmap

## Current Completion Status

Completed foundation tracks:

- Deterministic core + lockstep/replay baseline
- Capital/economy/movement/combat/siege/trade prototype slices
- Local playable shell + HUD foundation
- RtsClientRoot presentation split
- Full test-suite/harness structural cleanup

Current gate status:

- `tests=472/472 PASS`
- `chaos-v4` 5000-tick stress: pass

## Active Stage

Phase 8C: Documentation, Contracts, and Load Audit

This stage is architecture validation and planning, not gameplay expansion.

## Near-Term Sequence After 8C

1. Convert 8C audit outcomes into scoped follow-up issues.
2. Prioritize blockers for full-game load target:
   - high-risk movement/traffic scalability items
   - bot/AI loop budget contracts
   - visibility and networking operational risks
3. Start next feature phase only after high-risk blockers are scheduled and contract tests exist.

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
