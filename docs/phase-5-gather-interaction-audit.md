# Phase 5.8 Gather Interaction Audit

## Purpose

Allow the Godot visual shell to route right-clicks on visible resource primitives into existing gather commands.

This is client intent routing only. It does not add economy rules or validation to Godot.

## Behavior

When local units are selected:

- Right-click on a visible resource primitive queues `QueueGatherResource`.
- Right-click elsewhere queues `QueueMoveUnits`.

The simulation remains responsible for validating:

- Selected units exist.
- Selected units are owned by the player.
- Selected units are villagers.
- Resource node exists and is not depleted.
- Carry-resource restrictions are respected.

## Layer Boundary

- Simulation behavior changed: no
- GameState schema changed: no
- Tick order changed: no
- Checksum changed: no
- Lockstep code changed: no
- Replay code changed: no
- Godot gameplay authority added: no

## Regression Coverage

Tests cover a facade-driven gather flow:

- Place and complete a local Town Center.
- Queue gather through `GodotClientFacade`.
- Advance deterministic ticks.
- Verify food is deposited by simulation economy systems.
- Verify the valid flow does not reject.

## Next Safe Step

The client can now support the minimum early loop visually:

select villager -> place Capital -> gather -> move.

Training and attack UI can be added next through the same facade boundary.
