# Phase 6 Visual Placeholder Pipeline Audit

## Scope

This slice refines presentation-only sprite loading and diagnostics in the Godot client:

- centralized sprite asset registry in `Phase6SpriteRenderer`
- dynamic expected/loaded asset count
- explicit missing-asset HUD label when sprite pack is incomplete

No gameplay, simulation, pathfinding, combat, or deterministic state logic changed.

## Guardrails Confirmed

- simulation remains 2D deterministic and command-driven
- Godot visual mode remains a projection layer only
- primitive fallback remains available through `F9`
- missing sprite assets do not block runtime rendering

## Runtime Outcome

- Godot client C# build passes
- full tests remain green (`250/250`)

## Follow-up Playtest Focus

- verify HUD missing-asset label appears only when relevant
- verify `F9` toggle still swaps sprite/primitive modes cleanly
- verify unit/building selection outlines remain readable in both modes
