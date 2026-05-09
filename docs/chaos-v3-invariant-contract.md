# ChaosV3 Invariant Contract

Version: 1

ChaosV3 is the frozen area-damage stress scenario. It exists to protect the first nonlinear combat model from deterministic drift.

## Scope

- 6-player FFA lockstep.
- 1200+ tick smoke coverage in tests.
- 5000+ tick headless coverage before merging area-damage changes.
- Scripted mangonel volleys against clustered enemy infantry.
- Simultaneous area-damage deaths across all players.
- Area damage near normal RTS lifecycle systems: cleanup, population accounting, ranking, resignation, match end, replay, and lockstep checksum agreement.

## Invariants

- ScenarioVersion remains `1` unless a new golden contract is intentionally created.
- Commands are scheduled at exact ticks.
- No AI randomness or heuristic target selection is used.
- Mangonel damage collection is deterministic.
- Area targets are applied in ascending entity ID order.
- Friendly units are excluded from splash damage.
- Buildings are excluded from mangonel splash damage in this slice.
- Death marking happens after the full area-damage pass.
- Cleanup remains centralized in `CleanupSystem`.
- Population after simultaneous area deaths matches living units plus training reservations.
- Entity lookup matches unit and building lists after cleanup.
- Replay checksum matches lockstep checksum.
- Final match result finishes with player 5 as winner.

## Frozen Behavior

Do not edit ChaosV3 to accommodate future area-damage mechanics. If path blocking, projectile timing, building splash, or new siege roles need stress coverage, create ChaosV4 instead.
