# Phase 5 Client Command Intents Audit

Status: Green

## Added Boundary

`RtsGame.Presentation.ClientInput`

This layer maps client UI intentions into simulation command envelopes.

## Purpose

Godot can collect input and produce client intents. The mapper converts those intents into normal simulation commands.

The simulation command pipeline remains authoritative.

## Supported Intents

- NoOp
- PlaceTownCenter
- AssignBuild
- GatherResource
- TrainUnit
- MoveUnits
- Attack
- Resign
- PlaceWall
- CreateTradeRoute
- PlaceTradePost

## Guardrails

- No `src/sim` changes.
- No command execution in presentation.
- No direct `GameState` mutation.
- No command validation bypass.
- No gameplay authority in Godot.
- Invalid commands must still be rejected by simulation validation.

## Verification

- Full test suite: 144 tests, 0 failures.
- Movement intent maps to `MoveUnitsCommand`.
- Local 1v1 command flow can place Capitals, assign builders, and move a unit through normal sim commands.
- Mapping client intent does not mutate checksum-covered state.
- ChaosV1-V4 smoke tests remain green.
