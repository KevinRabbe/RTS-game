# Phase 5.6 Godot Visual Shell Audit

## Purpose

Add a minimal Godot C# scene that can consume the presentation facade and draw the current local 1v1 prototype with plain primitives.

This is intentionally ugly. It exists to prove the client boundary, not to polish art or UI.

## Added Files

- `GodotClient/RtsGame.GodotClient.csproj`
- `GodotClient/Scenes/Main.tscn`
- `GodotClient/Scripts/RtsClientRoot.cs`

## Boundary Contract

The Godot script:

- Creates `GodotClientFacade.CreateLocal1v1`.
- Advances the local prototype session.
- Reads `GodotFrameDto`.
- Draws rectangles, lines, health bars, and a small HUD.
- Queues input through facade methods only.

The Godot script does not:

- Reference `GameState`.
- Execute simulation commands directly.
- Validate gameplay actions.
- Implement combat, economy, placement, pathing, or cleanup rules.
- Feed Godot physics or delta time into simulation logic.

`delta` is used only to schedule presentation-side calls to `AdvanceOneTick` at the existing 20 TPS cadence.

## Current Controls

- Left click: select visible local unit primitive.
- Right click: queue move intent for selected units.
- `C`: queue Town Center placement intent at mouse tile.
- `Space`: pause/resume local presentation tick advancement.

## Determinism Notes

This is a local visual prototype path, not ranked multiplayer.

The deterministic contract remains:

`Godot input -> GodotClientFacade -> ClientCommandIntent -> LocalPlaySession -> TickRunner`

All invalid commands are still rejected by the simulation validation system.

## Verification

Core verification remains the C# test suite for `RtsGame.Sim`, `RtsGame.Net`, `RtsGame.Presentation`, and stress scenarios.

The Godot project is intentionally outside the core test path because it depends on the Godot .NET SDK.

`dotnet build GodotClient/RtsGame.GodotClient.csproj --no-restore` requires Godot/NuGet restore first because Godot writes its C# assets under `.godot/mono/temp/obj`.
