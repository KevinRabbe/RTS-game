# Current Vertical Slice

## Accepted State (Phase 9C.0 Entry)

- Branch: `phase-6-visual-placeholder-pipeline`
- `MovementEngineV2` and `GatherEngineV2` are default-on and accepted as prototype-ready.
- Legacy movement/gather flags remain available for rollback and deterministic comparison.
- `RtsClientRoot` split is complete; presentation orchestration is separated into focused helpers.
- Prototype HUD foundation is implemented from read-only snapshot/frame data.
- Prototype HUD UX polish is implemented (clearer hints + command status readability).
- Test project cleanup is complete: suites + harness split, `Program.cs` is runner/registration-focused.
- Scalable combat hotspot architecture plan is documented before new combat implementation.
- Combat baseline + hotspot pressure slices are implemented and validated (9A.1-9A.4).
- Combat test lab scenarios are implemented (`F2 CombatTest01` + enemy building target).
- Combat scenario UX polish is complete (scenario camera hint + clearer hotkey labels).
- Full gate remains green:
  - tests: `489/489 PASS`
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

Phase 9C.0 is complete (attack-move/auto-target architecture planning only).

Next implementation steps:

1. 9C.1 `AttackMoveCommand` + state only
2. 9C.2 bounded indexed auto-target acquisition
3. 9C.3 resume-after-kill/invalidation behavior
4. 9C.4 attack-move pressure scenarios
5. 9C.5 attack-move HUD/command feedback

No attack-move depth expansion beyond these slices until scale scenario gates are green.
