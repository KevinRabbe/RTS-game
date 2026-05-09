# Phase 5 Wall And Trade Hotkeys Audit

## Scope

This slice exposes existing facade placement commands in the Godot prototype shell.

New local hotkeys:

- `W` places a wall blueprint at the mouse tile.
- `T` places a Trade Post blueprint at the mouse tile.

## Architecture Notes

- No simulation behavior changed.
- No command validation changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- Godot still queues commands through `GodotClientFacade`.
- Simulation remains authoritative for resources, placement, construction, and rejection.

## Verification

Tests cover:

- Godot facade wall placement routing.
- Godot facade Trade Post placement routing.

Manual Godot usage:

- Build/collect enough resources.
- Press `W` or `T` over a valid tile.
