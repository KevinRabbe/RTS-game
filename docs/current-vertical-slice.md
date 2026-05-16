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

