# Phase 4 Expansion Risk Audit

Status: Green

## Contract

Second Town Centers are expansion commitments, not replacement Capitals.

## Rules Locked

- First Town Center is free.
- Later Town Centers cost meaningful wood.
- Later Town Centers are normal buildings.
- Normal Town Centers have lower hit points than the Capital.
- Capital population bonus applies only once.
- Capital bonus never transfers to another Town Center.
- Capital cannot be rebuilt after destruction.

## Design Intent

Expansion should be:

- High reward.
- High risk.
- Exposed.
- Punishable.

The Capital remains the unique strategic anchor. Losing it is a permanent turning point, not a temporary setback solved by rebuilding elsewhere.

## Verification Targets

- Expansion cost remains meaningfully above trade posts and wall spam.
- Completed normal Town Center stays weaker than Capital.
- Town Center placed after Capital loss is normal.
- Normal Town Center does not inherit Capital bonus after Capital destruction.
- Existing placement, replay, lockstep, and chaos tests remain deterministic.

## Verification

- Full test suite: 133 tests, 0 failures.
- ChaosV1 smoke: clean.
- ChaosV2 smoke: clean.
- ChaosV3 smoke: clean.
- ChaosV4 smoke: clean.

No simulation behavior changed in this phase; this branch locks existing expansion rules with tests and documentation.
