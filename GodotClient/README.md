# Godot Client Shell

This folder is reserved for the presentation client.

Rules:

- Godot is presentation only.
- Godot may read snapshots.
- Godot must not mutate `GameState`.
- Godot must not contain gameplay authority.
- Godot physics and delta time must not affect simulation.
- Headless simulation, replay, lockstep, and chaos tests remain authoritative.

Current presentation path:

`GameState -> GameSnapshot -> VisualFrame -> Godot drawing`

The first visual pass should draw intentionally plain primitives: colored unit squares, building rectangles, larger Capital rectangles, wall rectangles, trade route lines, health bars, and a simple fog overlay.

Input path:

`Godot input -> ClientCommandIntent -> CommandEnvelope -> simulation command validation`

Godot collects intent only. The simulation remains the only authority that validates and executes commands.

Local 1v1 prototype path:

`Godot input -> ClientCommandIntent -> LocalPlaySession -> GameSnapshot -> VisualFrame`

`LocalPlaySession` lives in `src/presentation/LocalPlay`. It owns a private local `GameState` for prototype play, fills missing 1v1 input with deterministic no-ops, advances only through `TickRunner`, and exposes snapshots/visual frames for drawing.
