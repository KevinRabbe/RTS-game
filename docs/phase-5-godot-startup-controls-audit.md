# Phase 5 Godot Startup Controls Audit

## Scope

The Godot shell can restart prototype matches from the presentation layer:

- `F1` starts local 1v1.
- `F6` starts local 6-player FFA.

## Architecture Boundary

- No simulation rules changed.
- No command execution rules changed.
- No `GameState` is exposed to Godot.
- Restarting creates a fresh `GodotClientFacade` through the existing local play presentation boundary.

## Implementation

- `RtsClientRoot.StartLocalMatch(playerCount)` centralizes local match startup.
- Startup clears presentation-only selection, hover, route-pending, pause, and tick accumulator state.
- The match uses the fixed default seed for repeatable prototype startup.

## Determinism Notes

- The match seed is explicit.
- Player count is passed into existing `LocalPlaySession.Create`.
- Godot still reads DTOs and queues commands only through the facade.
- The first tick is advanced through the normal facade path so snapshots are immediately drawable.

## Verification

- `dotnet build`
- `godot` filtered regression suite
