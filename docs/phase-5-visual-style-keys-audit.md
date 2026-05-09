# Phase 5 Visual Style Keys Audit

## Scope

This slice moves primitive style selection into a presentation-only bridge helper.

`GodotVisualStyleResolver` consumes `GodotPrimitiveDto` data and returns simple style keys. The Godot script maps those style keys to actual Godot `Color` values while remaining responsible only for drawing.

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- No Godot API types are used by the style resolver.
- Style resolution is presentation-only and carries no gameplay authority.

## Style Contract

The resolver covers:

- Local unit type styles.
- Enemy unit style.
- Normal building, Capital building, and wall styles.
- Food, wood, and gold resource styles.

## Verification

Tests cover:

- Local villager, scout, infantry, and cavalry style keys.
- Enemy unit style override.
- Building, Capital, and wall style keys.
- Food, wood, and gold resource style keys.
