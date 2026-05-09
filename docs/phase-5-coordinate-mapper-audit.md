# Phase 5 Coordinate Mapper Audit

## Scope

This slice centralizes Godot bridge coordinate conversion.

`GodotCoordinateMapper` converts between fixed raw tile coordinates, screen pixels, and integer tile indices. The Godot script still owns `Vector2` / `Vector2I` creation and drawing.

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- No Godot API types are used by the mapper.
- The mapper is presentation-only and carries no gameplay authority.

## Conversion Contract

- Raw fixed coordinates convert to pixels using a provided tile pixel scale.
- Screen pixels convert to raw fixed coordinates using the same scale.
- Screen pixels convert to tile indices using floor behavior, including negative coordinates.

## Verification

Tests cover:

- Raw-to-pixel conversion.
- Pixel-to-raw conversion.
- Positive and negative floor-to-tile behavior.
