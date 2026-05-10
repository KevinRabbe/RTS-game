# Phase 6.2 Godot Tech Label Audit

## Scope

This slice improves presentation readability for Phase 6 tech state without changing gameplay logic:

- map known tech ids to short HUD labels
- map known modifier ids to short HUD labels
- keep deterministic numeric fallback for unknown ids
- display completed tech label when no active research is running

## Boundary

- Simulation rules, command flow, and checksums are unchanged.
- Mapping lives in `src/presentation/GodotBridge`.
- HUD text remains a presentation-only concern.

## Implementation

- Added `GodotTechLabelResolver` with:
  - `ResolveTechLabel(int techId)`
  - `ResolveModifierLabel(int modifierId)`
- Updated `GodotHudTextBuilder` to use resolver labels for:
  - active research text
  - completed tech summary text
  - modifier status text

## Coverage

Added tests for:

- known tech label mapping
- unknown tech fallback mapping
- known modifier label mapping
- unknown modifier fallback mapping
- HUD completed-tech label output

All tests pass, including replay, lockstep, and chaos suites.
