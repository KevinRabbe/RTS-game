# Phase 5.9 Training Interaction Audit

## Purpose

Allow the Godot visual shell to route simple training hotkeys through the existing facade.

This is client intent routing only. It does not add training rules or validation to Godot.

## Behavior

- Left-click a visible local building to select it.
- Press `V` to queue villager training.
- Press `I` to queue infantry training.

The client does not decide whether the building can train the unit or whether the player can afford it.

## Simulation Authority

The simulation remains responsible for validating:

- Building id exists.
- Building belongs to the player.
- Building is completed.
- Unit type can be trained by that building type.
- Resource cost is payable.
- Population cap allows reservation.

## Layer Boundary

- Simulation behavior changed: no
- GameState schema changed: no
- Tick order changed: no
- Checksum changed: no
- Lockstep code changed: no
- Replay code changed: no
- Godot gameplay authority added: no

## Regression Coverage

Tests cover a facade-driven training flow:

- Place and complete a local Town Center.
- Gather enough food through the facade.
- Queue villager training through the facade.
- Advance deterministic training ticks.
- Verify food spend and population reservation are visible through DTO state.
- Verify the valid flow does not reject.

## Next Safe Step

The client now has a minimal playable economy loop:

select villager -> place Capital -> gather -> select Town Center -> train.

Attack selection and command routing can be added next through the same facade boundary.
