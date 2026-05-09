# Phase 5.10 Attack Interaction Audit

## Purpose

Allow the Godot visual shell to route right-clicks on visible enemy unit/building primitives into existing attack commands.

This is client intent routing only. It does not add combat rules or validation to Godot.

## Behavior

When local units are selected:

- Right-click on a visible enemy unit or building queues `QueueAttack`.
- Right-click on a visible resource queues `QueueGatherResource`.
- Right-click elsewhere queues `QueueMoveUnits`.

Attack target routing is presentation hit detection only. The simulation remains authoritative.

## Simulation Authority

The simulation remains responsible for validating:

- Attacker ids exist.
- Attackers are owned by the player.
- Target id exists.
- Target is not friendly.
- Unit type can attack that target kind.
- Range, cooldown, siege setup, area damage, death marking, and cleanup.

## Layer Boundary

- Simulation behavior changed: no
- GameState schema changed: no
- Tick order changed: no
- Checksum changed: no
- Lockstep code changed: no
- Replay code changed: no
- Godot gameplay authority added: no

## Regression Coverage

Tests cover facade attack routing through the normal simulation command path.

Existing combat, siege, area damage, replay, lockstep, and chaos tests continue to cover authoritative combat behavior.

## Next Safe Step

The local visual prototype now has the minimum interactive loop:

place Capital -> gather -> train -> move -> attack.

The next client slice can improve local match usability, such as selecting multiple units or adding simple camera controls.
