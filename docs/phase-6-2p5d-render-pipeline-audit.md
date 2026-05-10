# Phase 6.4 2.5D Render Pipeline Audit

## Scope

This slice locks a presentation-only 2.5D asset path while preserving deterministic simulation behavior:

- imported first placeholder art pack under `GodotClient/Art/Phase6Pack`
- added sprite renderer helper extraction from `RtsClientRoot`
- added runtime render mode toggle (`F9`) between sprites and primitive fallback
- added HUD render diagnostics (`Render <mode>`, loaded asset count)

## Boundary

- No simulation code or deterministic rules changed.
- Rendering remains fully presentation-side in Godot.
- Primitive rendering remains available as a debug/dev fallback.

## Runtime behavior

- When sprites are enabled and assets exist, units/buildings use 2.5D visuals.
- Missing assets gracefully fall back to primitive rendering paths.
- Render mode and loaded asset count are visible in HUD.

## Validation

- Godot C# project builds successfully.
- Full deterministic test suite remains green, including chaos scenarios.
