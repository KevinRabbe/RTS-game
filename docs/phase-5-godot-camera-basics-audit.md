# Phase 5 Godot Camera Basics Audit

## Scope

The Godot shell now owns a basic presentation camera.

- A `Camera2D` is created by `RtsClientRoot`.
- Arrow keys pan the camera.
- Mouse interaction uses global world coordinates so selection and command tiles remain aligned after panning.

## Architecture Boundary

- No simulation code changed.
- Camera movement is presentation-only.
- Camera movement does not queue commands.
- Camera movement does not affect tick advancement, checksums, replay, lockstep, or GameState.

## Determinism Notes

- Camera input uses Godot delta time only for presentation movement.
- Simulation still advances at fixed 20 TPS through the facade.
- All gameplay commands still convert world pixel coordinates into fixed raw coordinates and tile coordinates before entering the existing client intent path.

## Verification

- `dotnet build`
- `godot` filtered regression suite
