# High-Pop Simulation Architecture

## Purpose

Capital Frontiers targets a high-pop deterministic RTS, not a small skirmish sandbox. The worker jitter fixes exposed the first visible version of a general traffic problem that will also affect spawn exits, rally points, group movement, attack surrounds, siege deploy positions, trade endpoints, and choke-point traffic.

This document defines the simulation split and traffic policy for future gameplay work.

## Scale Assumptions

Design every simulation feature against these assumptions:

- 6 active players.
- 200+ population per player.
- Bonus population above cap may be granted by kills, rewards, or future systems.
- 1200+ active units can exist in one match.
- Deterministic lockstep and replay remain authoritative.
- Spectator and caster clients consume read-only snapshots.

The target scale does not mean every early prototype test must create 1200 units. It does mean no system should be shaped in a way that obviously breaks at that scale.

## Simulation Split

`src/sim` should keep responsibilities separated.

### Commands

Commands validate player actions and set intent only.

Rules:

- Commands must not perform long-running gameplay resolution.
- Commands must not pathfind repeatedly or solve traffic globally.
- Command validation remains authoritative and deterministic.
- Invalid commands reject deterministically.

### Intent And Order State

Intent state stores each unit's long-term purpose.

Examples:

- Move intent.
- Resource area or resource node target.
- Build target.
- Attack target.
- Trade route target.
- Future rally or formation intent.

Temporary congestion must not erase long-term intent.

### Spatial Geometry

Spatial geometry is pure map and footprint truth.

It owns:

- Map bounds.
- Static blockers.
- Building footprints.
- Resource footprints.
- Interaction rings.
- Walkability helpers.

Spatial helpers should stay mostly pure geometry. They should not become the worker, combat, or traffic brain.

### Traffic And Slot Ownership

Traffic owns final-purpose reservations and deterministic conflicts.

It answers:

- Which unit owns a final-purpose slot?
- Which slots are unavailable because they are occupied or reserved?
- Which unit wins a conflict?
- Does the loser wait or choose an alternate?

The current worker interaction reservation model is the first version of this layer.

### Movement Execution

Movement executes movement only.

It owns:

- Moving toward the current target.
- Deterministic arrival snap.
- No-stacking enforcement.
- Blocked/no-progress tracking.

Movement must not decide gather continuation, deposit policy, build progress, attack targeting, or spawn logic.

### Worker And Economy Task Resolution

Worker/economy systems resolve task phases using intent, traffic, and movement state.

They own:

- Gathering.
- Drop-off selection.
- Depositing.
- Construction progress.
- Resource continuation later.

### Production And Spawn

Production systems train units and use deterministic spawn slots.

Future spawn slots must follow the same ownership policy as worker slots: no two live units spawn into the same tile, conflicts resolve deterministically, and blocked exits wait or choose deterministic alternates.

### Combat And Siege

Future combat traffic should use explicit slots for:

- Melee attack surrounds.
- Siege deploy positions.
- Ranged formation or standoff positions if needed.

Do not solve these as one-off movement hacks.

### Snapshot And Presentation

Presentation, spectator, caster, and debug clients consume read-only snapshots.

Rules:

- Snapshot consumers never mutate `GameState`.
- Visual colliders, sprite bounds, and interpolation never decide gameplay.
- Debug data may expose traffic state, but must not affect simulation results.

## Core Simulation Laws

- Every gameplay system must be designed with 6-player 200+ pop scale in mind.
- No two live units may occupy the same tile.
- No two units may reserve the same final-purpose slot unless explicitly allowed by reservation kind.
- Temporary traffic must not clear long-term intent.
- Conflicts resolve deterministically.
- Losers wait cleanly or choose deterministic alternate slots.
- Movement executes movement only; task systems decide gameplay intent.
- Spatial rules stay mostly pure geometry, not worker/task brain logic.
- Presentation, caster, and spectator clients are read-only snapshot consumers.
- Avoid per-unit full scans when indexed or bounded alternatives are available.
- Avoid per-tick pathfinding and per-tick retargeting.
- Avoid unordered iteration in simulation.
- Avoid presentation-driven gameplay.
- Avoid spammy per-unit logs.

## Traffic Terminology

`Occupied tile`

A tile currently containing a live unit. No other live unit may move into or spawn into it during the same deterministic resolution step.

`Reserved final-purpose slot`

A tile claimed by a unit for a task endpoint, such as a resource interaction slot, drop-off slot, build slot, spawn exit, move destination, attack surround, siege deploy position, rally exit, or trade endpoint.

`Pass-through/path tile`

A tile used while traveling. It is not a final-purpose reservation unless a future traffic layer explicitly reserves corridors. Pass-through conflicts are resolved by movement rules.

`Static blockers`

Map bounds, walls, building footprints, resource footprints, and any explicit sim blocker objects.

`Dynamic/live unit blockers`

Live units that currently occupy tiles.

`Reservation conflict`

Two or more units attempting to own the same final-purpose slot. Conflicts must resolve by stable tie-breakers, usually unit id after task-specific priority.

`No-progress timeout`

A bounded deterministic threshold after which a unit may re-evaluate its current movement target or final-purpose slot without clearing long-term intent.

`Deterministic retarget`

Choosing a new valid slot or target using stable ordered candidates and explicit tie-breakers. Retargeting must not use randomness or unordered collection iteration.

## Reservation Kinds

Current reservation concepts:

- `ResourceInteraction`
- `DropoffInteraction`
- `BuildInteraction`

Current implementation names may use `ResourceNode`, `Dropoff`, and `BuildSite`; those are the concrete v1 names for the concepts above.

Future reservation concepts:

- `Spawn`
- `MoveDestination`
- `Formation`
- `AttackSurround`
- `SiegeDeploy`
- `RallyExit`
- `TradeEndpoint`

Do not implement future reservation kinds until their gameplay issue needs them.

## Scale Checklist

Every future simulation issue should answer:

- Does this iterate over all units, buildings, or resources?
- Is that acceptable at 1200+ units?
- Does it pathfind or repath every tick?
- Does it retarget every tick?
- Does it use unordered iteration?
- Does it create per-unit logs every tick?
- Does it rely on presentation, sprites, or colliders for gameplay?
- Are deterministic tie-breakers defined?
- Does this remain replay and lockstep safe?
- Can spectator and caster clients consume it read-only?

If the answer is unclear, narrow the feature before implementation.

## Prepared Future Systems

This architecture prepares the next slices for:

- Resource depletion and area continuation.
- Town Center villager production and spawn slots.
- Rectangle selection and group gather commands.
- DryArabiaTest01 playable economy layout.
- Group movement destinations and formations.
- Rally points and production exits.
- Melee surrounds and siege deployment.
- Trade endpoint traffic.
- 6-player FFA choke-point traffic.
