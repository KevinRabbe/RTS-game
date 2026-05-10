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

Godot facade path:

`Godot script -> GodotClientFacade -> LocalPlaySession -> VisualFrame DTOs`

`GodotClientFacade` lives in `src/presentation/GodotBridge`. It exposes simple methods such as `QueueMoveUnits`, `QueuePlaceTownCenter`, and `GetFrame`. The returned DTOs use fixed-point raw coordinates so drawing code can convert to pixels without introducing gameplay math.

Current Godot shell:

- `Scenes/Main.tscn` hosts one `Node2D`.
- `Scripts/RtsClientRoot.cs` creates a local facade and draws primitive rectangles/lines.
- `F1` restarts a local 1v1 prototype match.
- `F6` restarts a local 6-player FFA prototype match.
- Arrow keys pan the presentation camera.
- Left click selects a visible local unit.
- Left-click selection is resolved by a presentation-only selection router with priority: local unit, then local building or wall.
- Right click issues a move intent for selected units.
- Right click on an enemy unit or building issues an attack intent for selected units.
- Right click on a visible own under-construction building or wall assigns selected units to build it.
- Right click on visible food, wood, or gold issues a gather intent for selected units.
- Right-click routing is resolved by a presentation-only interaction router with priority: attack target, resource gather, then move fallback.
- `C` queues a local Town Center placement at the mouse tile.
- `W` queues a local wall placement at the mouse tile.
- `T` queues a local Trade Post placement at the mouse tile.
- Select a Trade Cart, then press `R` over two visible local Trade Posts to create a trade route.
- Left click a local building, then `V` queues villager training.
- Left click a local building, then `I` queues infantry training.
- Left click a local Trade Post, then `K` queues Trade Cart training.
- Left click a local Barracks, then `Y` queues Infantry Attack I research.
- `Space` pauses presentation tick advancement.
- Visible food, wood, and gold are drawn as resource circles.
- Visual DTOs include type ids so the shell can draw unit/building/resource types without guessing.
- Building status DTOs expose construction and training progress for the debug HUD.
- Unit status DTOs expose gathering, carried resources, movement, build targets, and attack targets for the debug HUD.
- HUD text is assembled by a presentation-only bridge helper so status formatting is testable outside Godot.
- Visual style keys are resolved by a presentation-only bridge helper; the Godot script maps those keys to actual colors.
- Primitive draw kinds are resolved by a presentation-only bridge helper so the Godot script does not switch on raw DTO kind numbers.
- Fixed raw, pixel, and tile coordinate conversions are centralized in a presentation-only bridge helper.

## Phase 6 Placeholder Art Pack

Placeholder sprite assets must live in:

`GodotClient/Art/Phase6Pack/`

Expected filenames:

- `villager_sheet.png`
- `infantry_sheet.png`
- `scout_sheet.png`
- `trade_cart_sheet.png`
- `capital.png`
- `wall_sheet.png`

If some files are missing, the Godot shell keeps running and falls back to primitive rendering for missing sprite mappings.

## Manual Godot Smoke Test (Visual Pipeline)

1. Launch the Godot client scene (`Scenes/Main.tscn`).
2. Press `F1` to start local 1v1.
3. Press `F6` to start local 6-player FFA.
4. Press `F9` to toggle primitive/sprite rendering.
5. Check HUD line for loaded/expected asset count.
6. If assets are missing, verify affected entities still render via primitive fallback.

The script may convert fixed raw coordinates to pixels for drawing. It must not contain combat, economy, placement, pathing, or validation rules.
