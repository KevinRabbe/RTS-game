# Phase 5.5 Godot Client Facade Audit

## Purpose

Add a Godot-facing presentation facade without adding Godot or engine types to the simulation.

This gives the future client one stable API for local prototype play:

`Godot script -> GodotClientFacade -> LocalPlaySession -> TickRunner -> GameSnapshot -> VisualFrame -> DTOs`

## Layer Boundary

- Location: `src/presentation/GodotBridge`
- Simulation behavior changed: no
- GameState schema changed: no
- Tick order changed: no
- Checksum changed: no
- Lockstep code changed: no
- Replay code changed: no
- Godot SDK dependency added: no

## Facade Contract

`GodotClientFacade`:

- Owns a `LocalPlaySession`.
- Queues client commands through `ClientCommandIntent`.
- Advances ticks through the local session only.
- Returns immutable DTOs for drawing and UI.
- Exposes fixed-point raw coordinates instead of floats.
- Does not expose mutable `GameState`.

## DTO Contract

`GodotFrameDto` contains:

- Tick
- Local player index
- Local player resources, population, and Capital status
- Match result status
- Visual primitive DTO array

`GodotPrimitiveDto` contains:

- Primitive kind as integer
- Entity id
- Owner player index
- Fixed raw start/end coordinates
- Fixed raw size
- Health values
- Capital flag

Godot can convert raw fixed values to pixels for drawing, but conversion remains presentation-only.

## Regression Coverage

Tests cover:

- Frame DTO creation.
- Local capital flow through the facade.
- Invalid command rejection through simulation validation.
- Fixed raw coordinate exposure.

## Next Safe Step

The next slice can create a minimal Godot script that calls this facade and draws primitive rectangles.

That script must remain presentation-only and must not add gameplay logic.
