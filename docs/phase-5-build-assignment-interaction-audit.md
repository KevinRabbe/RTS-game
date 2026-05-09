# Phase 5 Build Assignment Interaction Audit

## Scope

This slice adds presentation routing for assigning selected units to visible own under-construction buildings.

Right-click priority for selected units is now:

1. Enemy target -> attack.
2. Own under-construction building or wall -> assign build.
3. Resource node -> gather.
4. Empty tile -> move.

## Architecture Notes

- No simulation behavior changed.
- No command validation changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- Godot queues build assignment through `GodotClientFacade.QueueAssignBuild`.
- The interaction router consumes only `GodotFrameDto` primitives and building status DTOs.
- Simulation remains authoritative for whether the assign-build command is valid.

## Verification

Tests cover:

- Own under-construction building routes to assign-build intent.
- Completed own building does not route to assign-build intent.
- Existing attack, gather, and move routing remains covered.
