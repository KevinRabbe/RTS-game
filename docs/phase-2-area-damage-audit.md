# Phase 2 Area Damage Audit

Status: Green

## System Order Change

`AreaDamageSystem` was inserted after `SiegeAttackSystem` and before `CombatResolutionSystem`.

Current combat slice:

1. `SiegeSetupSystem`
2. `SiegeAttackSystem`
3. `AreaDamageSystem`
4. `CombatResolutionSystem`
5. `DeathMarkSystem`
6. `CapitalSystem`
7. `EliminationSystem`
8. `RankingSystem`
9. `MatchEndSystem`
10. `CleanupSystem`

Death marking and cleanup still happen after the full damage phase. Area damage does not remove entities directly.

## Added Unit Role

`UnitTypeId.Mangonel`

The first mangonel slice is intentionally narrow:

- Unit-targeted area damage.
- No friendly fire.
- No building splash damage.
- No projectiles.
- No physics.
- No random target selection.
- Integer/fixed-point range and radius checks only.

## Determinism Rules

- The attack command remains the only way to assign targets.
- Area target collection reads stable entity lists.
- Damage targets are sorted by ascending entity ID before damage application.
- Damage is applied as a batch before `DeathMarkSystem`.
- All mutable state remains in `GameState`.
- No engine or presentation types enter simulation.

## Verification

- Full test suite: 116 tests, 0 failures.
- ChaosV1 5000 ticks: no desync, no invariant failures.
- ChaosV2 5000 ticks: no desync, no invariant failures.
- ChaosV3 5000 ticks: no desync, no invariant failures.

## Next Gate

Do not add projectile timing, building splash, or advanced target selection inside this slice. Create a new explicit phase and stress contract if those mechanics are introduced.
