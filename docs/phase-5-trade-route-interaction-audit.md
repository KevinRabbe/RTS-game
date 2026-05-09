# Phase 5 Trade Route Interaction Audit

## Scope

This slice adds a minimal Godot prototype flow for creating trade routes.

Flow:

1. Select a Trade Cart.
2. Press `R` over the first visible local Trade Post.
3. Press `R` over the second visible local Trade Post.
4. Godot queues `CreateTradeRoute` through `GodotClientFacade`.

## Architecture Notes

- No simulation behavior changed.
- No command validation changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- Godot still queues commands through `GodotClientFacade`.
- The trade-route router consumes only `GodotFrameDto` primitives and unit status DTOs.
- Simulation remains authoritative for whether the trade route command is valid.

## Verification

Tests cover:

- Finding a visible local Trade Post under the cursor.
- Ignoring enemy Trade Posts and non-Trade-Post buildings.
- Finding a selected Trade Cart from unit status DTOs.
- Ignoring selected non-cart units.
