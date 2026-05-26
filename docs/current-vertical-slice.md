# Current Vertical Slice

## Accepted State (Post-10A Selection/Control Pass)

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
- Code documentation and targeted contract comments pass is complete (9C.8).
- Control group and selection quality pass is implemented:
  - `Ctrl+1..9` assign + `1..9` recall
  - `Shift+1..9` additive control-group merge
  - double-tap control-group recall camera jump
  - shift-click unit selection toggles
  - double-click same-type owned unit selection
  - shift-drag additive rectangle selection toggles
  - HUD control-group feedback plus matched `CG:x` indicator
- Full gate remains green:
  - tests: `512/512 PASS`
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

## Accepted Input + Attack-Move Contract

Client input contract is now accepted for prototype:

- Left click = select/info
- Left drag = box select
- Right click = contextual command
- `A` + click = attack-move
- `Escape` = cancel active mode
- `F1` / `F2` = scenario switch (`DryArabiaTest01` / `CombatTest01`)

Attack-move core loop is accepted for prototype:

- 9C.1 state/command plumbing
- 9C.2 bounded indexed target acquisition
- 9C.3 resume-after-clear behavior
- 9C.4 pressure scenarios (`10-path`, `50v50`, `150v150`, hotspot)
- 9C.5 HUD/debug exposure
- 9C.6 explicit RTS input mode contract

Remaining attack-move work is polish or future command-depth/AI/formation scope, not a core-foundation blocker.

## Next Phase Intent

Next recommended phase:

1. **10A — Control Groups and Selection Quality**

Reason: highest value for both economy and combat without touching simulation balance contracts.

## Updated Next Phase Intent

1. **10B - Command/Selection UX Depth**

Reason: extend player control quality (queueing/prioritized intents/readability) while preserving deterministic simulation contracts and avoiding balance churn.
