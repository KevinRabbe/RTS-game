# Prototype Architecture

This architecture exists to protect deterministic lockstep multiplayer.

Core law:

> Simulation must be deterministic, engine-agnostic, and command-driven.

If a feature conflicts with this law, redesign the feature.

Scale law:

> Simulation systems must be shaped for 6-player FFA, 200+ population per player, possible bonus population above cap, 1200+ active units, deterministic replay/lockstep, and read-only spectator/caster clients.

High-pop unit traffic policy lives in [High-Pop Simulation Architecture](high-pop-simulation-architecture.md).

## Layer Overview

The project is split into four layers.

1. Simulation Core
2. Networking
3. Presentation
4. Tooling and Debug

Only Simulation Core mutates `GameState`.

## Layer Responsibilities

### Simulation Core

Owns:

- `GameState`.
- Commands.
- Long-term unit intent/order state.
- Spatial geometry helpers for footprints, blockers, rings, and walkability.
- Traffic and final-purpose slot ownership.
- Command validation and execution.
- Deterministic systems.
- Movement execution.
- Worker/economy task resolution.
- Production and spawn resolution.
- Future combat/siege slot resolution.
- Tick advancement.
- Checksums.
- Replay reconstruction.

Forbidden:

- Engine types.
- Rendering types.
- Audio types.
- Real time or delta time.
- Physics engine calls.
- Network callbacks.
- Threads or async mutation.
- Unseeded random.
- Iteration over unordered collections.
- Presentation/collider-driven gameplay.
- Per-tick full pathfinding or retargeting unless explicitly bounded and proven acceptable at 1200+ units.

### Networking

Owns:

- Peer connections.
- Command transport.
- Future-tick input scheduling.
- Input delay.
- Stall behavior when commands are missing.
- Checksum exchange.
- Desync reports.

Forbidden:

- Direct mutation of `GameState`.
- Sending authoritative state as normal gameplay synchronization.
- Resolving gameplay conflicts outside simulation.

Networking output is a deterministic command buffer consumed by the simulation tick.

### Presentation

Owns:

- Rendering.
- Animation interpolation.
- Audio.
- UI.
- Selection.
- Camera.
- Local input collection.
- Read-only simulation snapshots.
- Spectator and caster read-only views.

Forbidden:

- Direct mutation of `GameState`.
- Gameplay decisions that affect simulation without commands.
- Presentation-only state that changes gameplay results.

Presentation may create command requests. The command request becomes gameplay only after it is serialized, tick-indexed, validated, and executed by Simulation Core.

Spectator and caster clients follow the same rule: they may read snapshots, interpolate, filter, annotate, and display, but they never mutate `GameState`.

### Tooling and Debug

Owns:

- Replay viewer.
- Simulation stepper.
- Checksum inspector.
- Map seed viewer.
- Command stream diffing.
- Desync diagnostics.

Forbidden:

- Hidden gameplay mutations.
- Debug-only state that changes normal match results.

## Tick Model

Simulation runs at 20 ticks per second.

Rules:

- One tick is one deterministic step.
- No `deltaTime`.
- Cooldowns, build progress, gathering, movement, setup, reload, and despawn use integer tick counters.
- Systems run in a fixed order every tick.
- Systems are stateless.
- All mutable gameplay data lives in `GameState`.

Example tick flow:

1. Read commands scheduled for current tick.
2. Validate commands.
3. Apply accepted commands to command-intent state.
4. Run simulation systems in fixed order.
5. Mark dead entities.
6. Run cleanup as the only removal step.
7. Produce checksum.
8. Publish read-only snapshot for presentation.

## GameState Shape

`GameState` is the single mutable root for simulation.

Recommended top-level structure:

```text
GameState
  Tick
  MatchSeed
  EntityState
  PlayerStateContainer
  EconomyState
  PopulationState
  MapState
  VisibilityState
  CommandState
  CombatState
  TradeState
  RankingState
```

Sub-states contain data only. They do not contain behavior.

## Entity Storage

`EntityState` contains deterministic lists and an ID lookup.

```text
EntityState
  NextEntityId
  Units: List<Unit>
  Buildings: List<Building>
  EntityLookup: Dictionary<int, EntityRef>
```

Rules:

- Lists are used for deterministic iteration.
- Dictionary is for ID lookup only.
- Dictionary is never iterated.
- Entity IDs are never reused.
- Dead entities are marked during systems.
- Entities are removed only in `CleanupSystem`.
- Swap-remove is allowed only in `CleanupSystem`.
- `EntityLookup` is updated immediately during cleanup.

## Entity Data

Use flat data records.

Example unit fields:

```text
Unit
  Id
  OwnerPlayerIndex
  UnitTypeId
  Position
  HitPoints
  CurrentOrder
  CarriedResourceType
  CarriedAmount
  IsDead
```

Example building fields:

```text
Building
  Id
  OwnerPlayerIndex
  BuildingTypeId
  Position
  Footprint
  HitPoints
  IsUnderConstruction
  BuildProgressTicks
  IsCapital
  IsDead
```

Do not store object references between entities. Store IDs and player indexes.

## Player State

`PlayerStateContainer` contains `List<PlayerState>`.

Example:

```text
PlayerState
  PlayerIndex
  Resources
  Population
  TechState
  CapitalStatus
  OwnedModifiers
  IsResigned
  IsEliminated
```

Units and buildings store `OwnerPlayerIndex`. They do not reference `PlayerState` objects.

## Commands

All player actions are commands.

Command requirements:

- Serializable.
- Tick-indexed.
- Player-indexed.
- Deterministic.
- Validated by simulation.
- Executed only during a simulation tick.
- Distinguish hard-invalid command rejection from temporary-congestion handling.

Initial command set:

```text
PlaceBuildingCommand
AssignBuildCommand
MoveUnitsCommand
GatherResourceCommand
DepositResourceCommand
TrainUnitCommand
AttackCommand
SetRallyPointCommand
ResearchTechCommand
CreateTradeRouteCommand
ResignCommand
```

UI intent is not a command until serialized and scheduled.

## Command Flow

```text
Local UI Input
  -> Presentation builds CommandRequest
  -> Networking schedules command for future tick
  -> CommandBuffer stores command by tick and player
  -> Simulation reads commands for current tick
  -> CommandValidationSystem accepts or rejects
  -> CommandExecutionSystem writes deterministic intent
  -> Systems resolve outcomes
```

Command rejection must be deterministic. The same invalid command must fail the same way on every peer.

Congestion rule:

- Hard-invalid requests reject (out of bounds, blocked footprint target, illegal ownership, missing prerequisites).
- Temporary traffic congestion should preserve valid long-term intent and be resolved by task/reservation/movement systems, not by command rejection.

## Fixed System Order

Start with a small explicit order. Expand carefully.

Recommended prototype order:

1. CommandValidationSystem
2. CommandExecutionSystem
3. ResignationSystem
4. BuildingPlacementSystem
5. ConstructionSystem
6. TrainingSystem
7. ResourceGatherSystem
8. ResourceDepositSystem
9. TradeRouteSystem
10. TradeMovementSystem
11. TradeIncomeSystem
12. MovementSystem
13. VisibilitySystem
14. TargetingSystem
15. SiegeSetupSystem
16. CombatResolutionSystem
17. AreaDamageSystem
18. DamageSystem
19. DeathMarkSystem
20. CapitalSystem
21. PopulationSystem
22. RankingSystem
23. CleanupSystem
24. ChecksumSystem

Systems should be plain functions or small stateless modules. Avoid inheritance-based system hierarchies.

## Simulation Responsibility Split

Future systems should preserve this split inside `src/sim`:

- Commands validate player actions and set intent only.
- Intent/order state stores each unit's long-term purpose.
- Spatial geometry exposes map bounds, footprints, blockers, rings, and walkability.
- Traffic/slot ownership manages final-purpose reservations, conflicts, and deterministic ownership.
- Movement execution moves toward targets, snaps/arrives, and tracks blocked/no-progress state.
- Worker/economy task resolution performs gather, deposit, and build phases using traffic plus movement state.
- Production/spawn systems train units and choose spawn slots.
- Combat/siege systems will later own attack surround and siege deploy slots.
- Snapshot/presentation systems expose read-only state for Godot, spectators, casters, and debug.

Spatial rules should stay mostly pure geometry. They should not become the worker/task brain.

## Unit Traffic Laws

- No two live units may occupy the same tile.
- No two units may reserve the same final-purpose slot unless the reservation kind explicitly allows sharing.
- Temporary traffic must not clear long-term intent.
- Conflicts resolve deterministically.
- Losers wait cleanly or choose deterministic alternate slots.
- Movement executes movement only; task systems decide gameplay intent.
- Slot ownership must not rely on presentation sprites, click bounds, or colliders.

Terminology:

- `Occupied tile`: a tile currently containing a live unit.
- `Reserved final-purpose slot`: a task endpoint claimed by a unit.
- `Pass-through/path tile`: a travel tile, not a task endpoint.
- `Static blocker`: map bounds, walls, footprints, or explicit sim blocker objects.
- `Dynamic blocker`: a live unit occupying a tile.
- `Reservation conflict`: multiple units attempting to own one final-purpose slot.
- `No-progress timeout`: a bounded retry threshold.
- `Deterministic retarget`: alternate selection through stable ordered candidates and tie-breakers.

Current reservation concepts:

- Resource interaction.
- Drop-off interaction.
- Build interaction.

Future reservation concepts:

- Spawn.
- Move destination.
- Formation.
- Attack surround.
- Siege deploy.
- Rally exit.
- Trade endpoint.

## Determinism Rules

Use:

- Integers for resources, hit points, ticks, costs, and population.
- Fixed-point numbers for positions if sub-tile precision is needed.
- Stable sorted command order.
- Stable entity list iteration.
- Seeded RNG stored in `GameState` or derived from match seed and tick.
- Explicit tie-breakers using entity ID, player index, then command order.

Avoid:

- Floating-point gameplay math unless fully controlled and proven deterministic.
- Wall-clock time.
- Engine physics.
- Random without seed.
- Hash-set or dictionary iteration.
- Platform-dependent sorting.
- Async mutation during tick execution.

## Platform Reproducibility Invariant

Simulation must reproduce the same result across different hardware, operating systems, engine runtimes, and CPU architectures.

Rules:

- Use explicit integer widths in simulation data, such as `int32`, `uint32`, `int64`, and `uint64`.
- Do not rely on language defaults when serialized size or overflow behavior matters.
- Define overflow policy for every deterministic math utility.
- Use a project-owned fixed-point wrapper for non-integer gameplay positions, distances, and speeds.
- Do not use platform math library functions for gameplay results unless wrapped and proven deterministic.
- Do not sort with culture-aware, locale-aware, or platform-dependent comparison.
- All simulation sorting must use explicit comparers with stable tie-breakers.
- Serialized command and replay formats must define byte order.
- Checksums must be computed from canonical serialized state, not object memory layout.
- Tests must run the same command stream more than once in the same process and in a fresh process.

If a feature cannot meet this invariant, keep it out of Simulation Core.

## Lockstep Networking

Lockstep sends commands for future ticks.

Basic model:

```text
Current local tick: 100
Input delay: 6 ticks
New local command scheduled for tick 106
Peer commands for tick 106 must arrive before simulation advances through 106
```

Rules:

- Simulation advances only when required inputs for the tick are available.
- Missing inputs stall simulation.
- Peers periodically compare checksums.
- Desync does not trigger state correction in ranked play.
- Desync diagnostics capture tick, seed, checksum, command list, and recent state hashes.

## Replay Model

Replay stores:

- Game version.
- Ruleset version.
- Match seed.
- Player setup.
- Map generation parameters.
- Command stream.
- Optional periodic checksums for validation.

Replay does not store authoritative state snapshots for playback.

Snapshots may be stored only as debug aids and must not be required to reproduce gameplay.

## Presentation Snapshot

Presentation reads an immutable snapshot.

Snapshot may include:

- Visible entities.
- Player resource counts.
- Population counts.
- Build progress.
- Attack cooldown display values.
- Capital state.
- Map visibility for local player.

Presentation may interpolate positions visually between ticks, but interpolation has no effect on simulation positions.

## Prototype Module Layout

Suggested code layout:

```text
src/
  sim/
    GameState.*
    TickRunner.*
    Commands/
    Data/
    Systems/
    Determinism/
    Checksums/
    Replay/
  net/
    LockstepSession.*
    CommandTransport.*
    InputDelay.*
    DesyncReport.*
  presentation/
    SnapshotReader.*
    InputAdapter.*
    Renderer.*
    UI.*
  tools/
    HeadlessSimRunner.*
    ReplayRunner.*
    ChecksumDiff.*
```

Keep `src/sim` free of imports from `net`, `presentation`, engine SDKs, rendering libraries, and platform UI.

## Feature Addition Checklist

Before adding a gameplay feature, answer:

- What command creates or changes player intent?
- What data is added to `GameState`?
- Which stateless system processes it?
- What is the fixed order relative to existing systems?
- What are the deterministic tie-breakers?
- What checksum or replay test proves it?
- Does presentation remain read-only?
- Does networking still send only commands?
- Does this iterate over all units, buildings, or resources?
- Is that acceptable at 1200+ active units?
- Does it pathfind or repath every tick?
- Does it retarget every tick?
- Does it use unordered iteration?
- Does it create per-unit logs every tick?
- Does it rely on presentation or collider geometry for gameplay?
- Can spectator and caster clients consume it read-only?

If any answer is unclear, narrow the feature.
