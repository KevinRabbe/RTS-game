# Current Vertical Slice

## Immediate Target

`DryArabiaTest01 Playable Economy Slice`

This is the short-term source of truth for what we finish before broad polish or expansion work.

## Current Branch State

- Branch: `phase-6-visual-placeholder-pipeline`
- Recent focus: Phase 7 stabilization and architecture hardening
- Simulation remains deterministic lockstep/replay-first

## Done Systems (Current Slice Foundation)

- Resource architecture:
  - `ResourceArea`, `ResourceNode`, `GatherProfile`
  - footprint vs visual geometry split
  - depletion and same-area continuation
- Worker behavior:
  - explicit worker task phases
  - deterministic interaction slot reservations
  - intent retention under temporary congestion
  - local dynamic unit avoidance / pass-around
- Building and economy flow:
  - TC placement/preview parity
  - build/drop-off/spawn interaction rings from simulation footprints
  - TC spawn slots outside footprint, no stacking
- Input and command usability:
  - rectangle selection
  - deterministic group move/gather dispatch order
- Debug and diagnostics:
  - selected-unit task/reservation range status in snapshot/HUD lines
  - traffic and jitter stabilization passes landed

## Known Blockers

- Worker command reliability still needs more hardening in live manual smoke:
  - occasional congestion knots near TC front/drop-off corridors
  - intermittent jitter/stutter perception under crowding
  - command targeting clarity around crowded entities
- DryArabia economy layout still needs a deliberate playable lane pass.
- `RtsClientRoot` remains overloaded and should be split after core loop reliability is acceptable.

## Exact Next Issue Order

1. Worker command reliability hardening follow-up (small deterministic traffic fixes only).
2. Playability invariants and worker trace diagnostics pass.
3. DryArabiaTest01 playable economy layout pass.
4. HUD foundation pass (read-only snapshot UX, no gameplay logic in UI).
5. `RtsClientRoot` split into focused presentation/input components.
6. Basic combat vertical slice stabilization.
7. Easy bot for local pressure testing.
8. Replayable local 1v1 flow hardening.
9. LAN host/join lockstep slice.
10. Reconnect v1.
11. Later 6-player FFA expansion hardening.

## Manual Smoke Gates

Run before promoting slice status:

1. F1 `DryArabiaTest01`.
2. Place/build TC with multiple villagers.
3. Gather food/wood/gold with 3+ workers.
4. Confirm deposit loop and return-to-target behavior.
5. Train villagers and confirm spawn flow stays unclogged.
6. Box-select workers, issue group gather/move commands.
7. Verify no stacking, no endless jitter, no intent loss from temporary congestion.
8. Confirm deterministic stress (`chaos-v4`) and full tests still pass.

## Regression Workflow (Default)

Core gameplay debugging now follows this order:

1. Manual playtest captures a symptom.
2. Convert the symptom into a deterministic simulation-only scenario test.
3. Add invariant checks and compact worker trace output.
4. Reproduce failure in tests first.
5. Fix only the failing simulation layer.
6. Keep the regression test permanently.
7. Run Godot manual smoke for readability/feel after sim tests are green.

Rule:
If a manual bug needs more than one guess, it must become a sim-only scenario before additional patches.

### Sim-First Bug Matrix (Merge Blocking)

Core reliability scenarios are grouped into deterministic matrix packs:

- `resource_stall`: repeated gather/deposit cycles through TC choke points.
- `dropoff_congestion`: full-carrier return from opposite approach sides.
- `command_replacement`: repeated `move -> gather -> move` replacement churn.
- `spawn_overlap`: spawn pressure overlapping active worker economy loops.
- `pressure_120`: budget and invariants under high worker pressure windows.

Each pack must enforce:

- no stacking
- no duplicate final-purpose reservations
- bounded `MovingTo*` no-progress
- bounded reservation churn
- bounded legal-command reject volume
- deterministic replay/checksum consistency

## Validation Gate (Run In This Order)

Run validation serially after each simulation change:

1. `dotnet build GodotClient\RtsGame.GodotClient.csproj --no-restore`
2. `dotnet build tests\RtsGame.Tests.csproj --no-restore`
3. `dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --fail-fast`
4. `dotnet run --project src\tools\Headless\RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v4 --ticks 5000 --seed 77`

Important:
- Keep this sequence serial (not parallel) to avoid test host file-lock races on `tests\bin\Debug\net10.0\*.dll`.
- Manual Godot smoke remains informational only until the simulation gate is green.

## Scale-First Foundation Rule

All future simulation work is now constrained by a hard foundation rule:

- Design for `6 players`, `200+ pop each`, and `1200+ active units` by default.
- Prioritize deterministic and bounded behavior over perfect RTS polish.
- Preserve long-term intent under temporary congestion.
- Avoid hot-path full scans when deterministic indexed checks are available.

Execution order is locked:

1. Build deterministic simulation scenario first.
2. Add invariants and compact trace output.
3. Reproduce the bug in tests.
4. Fix the exact failing simulation layer.
5. Keep regression tests permanently.
6. Run Godot manual smoke only after sim tests are green.

## Stable Upgrade Boundary (AoE4-Like Path Ready)

Movement-quality improvements must be additive upgrades through service implementations:

- `ISpatialIndexService`
- `IPathQueryService`
- `ITrafficReservationService`
- `IMovementProgressPolicy`

Rules:

- No direct pathfinder usage outside path service.
- No direct reservation writes outside reservation service (except entity initialization).
- Deterministic ordered tie-breaks only in conflict logic.
- Temporary congestion may not clear long-term intent.

This keeps future v2/v3/v4 movement improvements upgrade-safe without gameplay API rewrites.

## No-Goals For This Slice

- Combat depth expansion
- Bots beyond simple local helper
- Networking feature expansion beyond lockstep slices listed above
- HUD/menu final polish
- Full terrain engine
- Wall UX expansion
- Broad balance tuning

## Definition Of Done

The slice is done when:

- Worker gather/deposit/build loops are reliable in repeated manual smoke.
- Temporary congestion does not clear valid long-term worker intent.
- Group commands behave deterministically without stacking or endless jitter.
- DryArabia supports a readable 5-10 minute economy loop.
- Builds pass, full deterministic test suite passes, and stress remains desync-free.
