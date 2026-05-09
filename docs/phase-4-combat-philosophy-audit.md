# Phase 4 Combat Philosophy Audit

Status: Green

## Rule Added

Moving now clears `AttackTargetId`.

This makes disengagement explicit:

- A player can move a unit away from combat.
- The unit stops pursuing its previous target.
- Existing attack cooldown is preserved and continues ticking normally.
- Re-engagement requires a new `AttackCommand`.

## Design Intent

This supports the desired combat curve:

1. Positioning.
2. Commitment.
3. Decisive resolution.
4. Recovery.

Disengagement remains possible, but it is not free automation. A unit that is repositioned must be explicitly recommitted.

## Guardrails

- No new systems.
- No tick order changes.
- No randomness.
- No damage-type model.
- No armor model.
- No auto-targeting.
- No hidden mutable combat state.

## Verification Targets

- Move command clears attack target.
- Move command preserves attack cooldown.
- Disengaged unit does not resume attacking without explicit attack command.
- Existing combat, replay, lockstep, and chaos scenarios remain deterministic.

## Verification

- Full test suite: 129 tests, 0 failures.
- ChaosV1 5000 ticks: no desync, no invariant failures.
- ChaosV2 5000 ticks: no desync, no invariant failures.
- ChaosV3 5000 ticks: no desync, no invariant failures.
- ChaosV4 5000 ticks: no desync, no invariant failures.
