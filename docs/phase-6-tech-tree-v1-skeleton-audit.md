# Phase 6.1 Tech Tree v1 Skeleton Audit

## Scope

Phase 6.1 adds the first conservative tech skeleton:

- `TechId.InfantryAttack1`
- command-driven research
- player-owned completed tech list
- player-owned research queue
- player-owned modifier table
- checksum, replay, and lockstep coverage

This is intentionally not a branching tech tree and not a balance expansion.

## Architecture Boundary

- Base unit constants in `GameData` remain immutable.
- Research mutates only `PlayerState.TechState`.
- Systems remain stateless.
- No engine or presentation types enter simulation.
- No hidden global state is introduced.
- Research is triggered only by `ResearchTechCommand`.

## Data Model

`PlayerState` now owns:

- `PlayerTechState.CompletedTechs`
- `PlayerTechState.ResearchQueue`
- `PlayerTechState.Modifiers`

All collections are lists for deterministic iteration. No dictionary is iterated.

## Command Flow

`ResearchTechCommand` validates:

- tick and player index
- completed, owned, non-dead research building
- tech allowed at that building type
- tech not already completed
- tech not already queued
- resources available

Execution spends resources and appends a `ResearchQueueItem`.

## System Order

`ResearchSystem` runs after `TrainingSystem` and before trade/combat systems.

This means research queued by a command begins progressing during the same tick, matching the existing training pattern.

## Modifier Application

`TechRules.GetModifiedUnitAttackDamage` reads the owner player's modifier table.

`InfantryAttack1` adds:

- `ModifierId.InfantryAttackBonus`
- value: `GameData.InfantryAttack1DamageBonus`

Only normal infantry single-target damage uses this first modifier.

## Determinism Coverage

Added tests:

- research completion
- duplicate completed tech rejection
- modifier affects infantry damage
- checksum covers completed tech and modifiers
- replay determinism
- lockstep determinism

## Explicit Non-Goals

- No branching tech tree.
- No faction-specific tech.
- No Godot UI.
- No dynamic registration.
- No research cancellation.
- No aura or temporary modifier system.
