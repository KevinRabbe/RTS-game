# Current Vertical Slice

## Accepted State (Phase 8C Entry)

- Branch: `phase-6-visual-placeholder-pipeline`
- `MovementEngineV2` and `GatherEngineV2` are default-on and accepted as prototype-ready.
- Legacy movement/gather flags remain available for rollback and deterministic comparison.
- `RtsClientRoot` split is complete; presentation orchestration is separated into focused helpers.
- Prototype HUD foundation is implemented from read-only snapshot/frame data.
- Test project cleanup is complete: suites + harness split, `Program.cs` is runner/registration-focused.
- Full gate remains green:
  - tests: `472/472 PASS`
  - `chaos-v4` (`5000` ticks): `desync=False`, `invariant_failures=0`

## Vertical Slice Scope

Current slice is a deterministic playable core, not feature-complete RTS content:

- Nomad start and capital placement
- Worker gather/deposit/build loops
- Movement + congestion handling with deterministic reservations
- Combat + siege baseline
- Trade baseline
- Local play shell + HUD/readability
- Replay/lockstep correctness gates

## Locked Workflow

Simulation-first workflow is mandatory:

1. Reproduce with a deterministic sim scenario.
2. Add invariant checks and compact traces.
3. Fix only the failing simulation contract.
4. Keep scenario as permanent regression.
5. Run full deterministic gate.
6. Use manual Godot smoke only after sim gate is green.

## Scale-First Operating Target

All new simulation work is shaped for:

- 6 players
- 200+ pop each
- up to ~1200 active units
- up to ~720 villagers

## Explicit Freeze

Movement/gather behavior is frozen by default.
Only touch it when:

- deterministic scenario fails,
- manual play is blocked,
- pressure/chaos gate fails,
- or new feature integration requires a scoped contract change.

## Next Phase Intent

Phase 8C is documentation/contracts/load-audit only:

- no gameplay feature expansion,
- no simulation semantic changes,
- no architecture rewrites.
