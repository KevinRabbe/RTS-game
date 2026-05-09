# Phase 5 Trade Cart Training Interaction Audit

## Scope

Godot presentation can now queue Trade Cart training from a selected Trade Post through the existing facade command path.

## Architecture Boundary

- No simulation behavior changed.
- No `GameState`, checksum, tick order, replay, chaos, or lockstep code changed.
- Godot still emits only client intent.
- Simulation remains the only authority for trainability, costs, population, and completion.

## Implementation

- `RtsClientRoot` maps `K` to `QueueTrainUnit(..., TradeCart)`.
- The command flows through:

`Godot input -> GodotClientFacade -> LocalPlaySession -> ClientCommandIntent -> TrainUnitCommand -> simulation validation`

## Determinism Notes

- No new randomness.
- No presentation-side validation rules were added.
- No engine types entered simulation.
- The facade test proves the command path trains a Trade Cart from a completed Trade Post and rejects nothing in the valid flow.

## Verification

- `godot facade routes trade cart training command`
- `godot` filtered regression suite
