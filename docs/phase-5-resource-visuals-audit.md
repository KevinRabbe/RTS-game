# Phase 5.7 Resource Visuals Audit

## Purpose

Expose visible resource nodes through the presentation pipeline so the Godot prototype can show the economy layer.

This is a read-only visual slice. It does not alter gather, deposit, depletion, economy, visibility, or command validation behavior.

## Added Presentation Data

`GameSnapshot` now includes visible `ResourceNodeSnapshot` values:

- Resource id
- Resource type
- Fixed-point position
- Remaining amount

Depleted resources are filtered out.

## Added Visual Primitives

`VisualPrimitiveKind` now includes:

- `FoodResourceCircle`
- `WoodResourceCircle`
- `GoldResourceCircle`

The primitive id is the resource node id. The owner is neutral.

## Godot Shell Update

`RtsClientRoot` now draws resource primitives with simple circles:

- Food: green
- Wood: brown
- Gold: gold

Hovering a visible resource highlights it and shows the resource id in the debug HUD.

## Layer Boundary

- Simulation behavior changed: no
- GameState schema changed: no
- Tick order changed: no
- Checksum changed: no
- Lockstep code changed: no
- Replay code changed: no
- Godot gameplay authority added: no

## Regression Coverage

Tests cover:

- Visible resource nodes appear in snapshots.
- Depleted resource nodes are hidden.
- Resource visual primitives are emitted.
- Godot DTOs expose resource primitive kind, neutral ownership, and fixed raw coordinates.
- Existing snapshot/visual checksum immutability tests still cover the extended path.

## Next Safe Step

The next client slice can route right-clicks on resource primitives into `QueueGatherResource` for selected villagers.

The client must still not validate gather rules; invalid gather commands remain simulation rejections.
