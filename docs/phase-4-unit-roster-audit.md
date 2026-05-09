# Phase 4 Unit Roster Audit

Status: Green

## Added Unit Role

`UnitTypeId.Cavalry`

Cavalry is the minimal fast punish unit for the competitive baseline.

## Scope

- Fast movement.
- Stronger linear melee damage than infantry.
- 2 population.
- Food and gold cost.
- Trainable from Town Center in the current prototype roster.

## Guardrails

- No new systems.
- No tick order changes.
- No inheritance hierarchy.
- No special targeting logic.
- No charge mechanic.
- No armor or damage-type system yet.
- No tech dependency yet.

## Verification

- Cavalry training completes and reserves 2 population.
- Cavalry moves faster than infantry.
- Cavalry combat is deterministic.
- Replay and lockstep tests cover cavalry combat.

## Next Gate

Do not add additional units before a combat philosophy pass. The current minimal roster is:

- Villager
- Scout
- Infantry
- Cavalry
- Siege Cannon
- Mangonel
- Trade Cart
