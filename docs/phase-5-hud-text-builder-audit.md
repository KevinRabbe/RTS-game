# Phase 5 HUD Text Builder Audit

## Scope

This slice moves Godot debug HUD text assembly into a presentation-only bridge helper.

`GodotHudTextBuilder` consumes `GodotFrameDto` data plus local presentation selection state and returns a single text string for the Godot script to draw.

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- No Godot API types are used by the HUD text builder.
- The Godot script still owns drawing and local UI state.
- Simulation validation remains authoritative for all commands.

## Display Contract

The HUD text includes:

- Tick.
- Local player resources.
- Population usage and cap.
- Selected unit ids.
- Selected unit status when available.
- Selected building id.
- Selected building status when available.
- Hovered resource id.
- Pause state.

Missing unit or building status is treated as absent display data. The builder does not invent gameplay state.

## Verification

Tests cover:

- Economy, population, selection, hovered resource, and pause display.
- Selected unit gather status display.
- Selected building training status display.
- Missing status fallback behavior.
