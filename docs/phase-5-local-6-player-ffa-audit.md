# Phase 5 Local 6-Player FFA Audit

## Scope

The presentation/local play layer can now create a six-player local FFA session for prototype viewing and smoke testing.

## Architecture Boundary

- No simulation systems changed.
- No command validation changed.
- No replay, checksum, lockstep, or chaos scenario code changed.
- Local play still advances only through `TickRunner`.
- Missing local inputs are filled with deterministic `NoOpCommand`s for every configured player.

## Implementation

- `LocalPlaySession.Create(matchSeed, playerCount)` centralizes local session construction.
- `LocalPlaySession.Create1v1` remains as the existing two-player path.
- `LocalPlaySession.Create6PlayerFfa` creates the six-player FFA path.
- `GodotClientFacade.CreateLocal6PlayerFfa` exposes that setup to Godot without exposing simulation state.

## Determinism Notes

- The player count feeds existing `GameRules.CreatePhaseZeroDefaults`.
- All player inputs still receive ordered sequence numbers.
- Automatic no-op filling iterates player indexes from `0` to `Rules.MaxPlayers - 1`.
- The six-player facade test verifies one tick executes six deterministic no-ops and can render player 5's local view.

## Verification

- `local play session creates 6 player ffa`
- `godot facade creates local 6 player ffa`
- `godot` filtered regression suite
