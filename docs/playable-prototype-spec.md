# Playable Prototype Spec

This document defines the first playable prototype. It is intentionally small.

The prototype should prove the core loop, Capital system, map control, economy, combat pressure, and deterministic lockstep foundation without building every planned feature.

## Prototype Name

Capital Lockstep Prototype.

## Target Experience

A short 2-6 player match where players:

1. Start nomad with villagers and a scout.
2. Choose where to place their first Town Center.
3. Build an economy around food, wood, and gold.
4. Scout and contest the map.
5. Train basic military.
6. Pressure enemy Town Centers.
7. Experience Capital loss as a major turning point.
8. Finish the match through decisive combat pressure.

Prototype match length target:

- 8-15 minutes.

This is shorter than the final target so iteration stays fast.

## Non-Negotiable Technical Requirements

The prototype must support:

- Fixed 20 TPS simulation.
- Command-driven gameplay.
- Headless simulation runs.
- Replay from command stream.
- Checksums.
- No engine logic in simulation.
- No direct UI mutation of `GameState`.
- Deterministic system order.
- No unordered collection iteration in simulation.

## Minimum Playable Features

### Players

- 2-6 player slots.
- Human local players are enough for first local prototype.
- Networked peers can be added after headless lockstep tests pass.

### Start

- Each player starts with:
  - 4 villagers.
  - 1 scout.
  - No Town Center.
- Spawn sectors are generated from match seed.
- Spawn sectors should be fair but not mirrored.

### Capital

- First completed Town Center becomes Capital.
- Capital has more hit points than normal Town Center.
- Capital grants a population bonus.
- Capital has unique presentation identity.
- Capital cannot be rebuilt.
- Destroying Capital removes bonus permanently.
- Player survives if they own another Town Center.

### Economy

Resources:

- Food.
- Wood.
- Gold.

Resource behavior:

- Food and wood support early growth.
- Gold comes from finite mines.
- Villagers gather and deposit at Town Centers.
- Costs are integer values.

Prototype units:

- Villager.
- Scout.
- Infantry.
- Cavalry.

Prototype buildings:

- Town Center.
- House or population structure if needed.
- Barracks or training structure if needed.

Keep the first prototype minimal. If Capital population bonus alone is enough, postpone extra population buildings.

### Population

- Shared population cap.
- Villager: 1.
- Infantry: 1.
- Scout: 1.
- Cavalry: 2.
- Capital grants moderate bonus.

### Map

Map should contain:

- Player spawn sectors.
- Center with higher-value resources.
- Flanks with defensive geometry.
- Partial choke areas.
- Finite gold mines.

Prototype terrain can be grid-based.

Fairness requirements:

- Every player has viable initial TC locations.
- Every player has comparable access to safe early resources.
- Center access varies but does not decide the match immediately.

### Combat

Prototype combat includes:

- Move command.
- Attack command.
- Hit points.
- Attack range.
- Attack cooldown in ticks.
- No friendly fire.
- Building damage.
- Unit death.
- Building destruction.

Prototype combat can use simple deterministic range checks. Do not add complex physics.

### Siege

First siege slice:

- One anti-building siege unit.
- Setup time in ticks.
- Long reload in ticks.
- High building damage.

Mangonel and wall upgrades can wait until the core combat loop is stable.

### Resignation and Elimination

Prototype includes:

- Resign command.
- Resigned player's units become neutral.
- Neutral resigned units despawn after 60 seconds, which is 1200 ticks at 20 TPS.

Elimination:

- Player is eliminated when they have no Town Centers and no villagers.
- Capital destruction alone does not eliminate.

Ranking:

- Placement primary.
- Elimination contribution secondary.

## Prototype Command Set

Required commands:

```text
PlaceTownCenterCommand
AssignBuildCommand
MoveUnitsCommand
GatherResourceCommand
TrainUnitCommand
AttackCommand
ResignCommand
```

Optional after first playable:

```text
PlaceBarracksCommand
SetRallyPointCommand
CreateTradeRouteCommand
BuildWallCommand
DeploySiegeCommand
```

## Prototype System Order

Use this order until a feature requires a reviewed change:

1. CommandValidationSystem
2. CommandExecutionSystem
3. ResignationSystem
4. BuildingPlacementSystem
5. ConstructionSystem
6. TrainingSystem
7. ResourceGatherSystem
8. ResourceDepositSystem
9. MovementSystem
10. VisibilitySystem
11. TargetingSystem
12. SiegeSetupSystem
13. CombatResolutionSystem
14. DamageSystem
15. DeathMarkSystem
16. CapitalSystem
17. PopulationSystem
18. RankingSystem
19. CleanupSystem
20. ChecksumSystem

## Data-First Unit Definitions

Keep unit definitions in data.

Example:

```text
UnitType
  Id
  Name
  CostFood
  CostWood
  CostGold
  Population
  MaxHitPoints
  MoveSpeedFixed
  AttackDamage
  AttackRangeFixed
  AttackCooldownTicks
  BuildTimeTicks
```

No subclass per unit type.

## Data-First Building Definitions

Example:

```text
BuildingType
  Id
  Name
  CostFood
  CostWood
  CostGold
  PopulationProvided
  MaxHitPoints
  Footprint
  BuildTimeTicks
  CanTrainUnitTypeIds
```

Capital behavior should be data plus `CapitalSystem`, not a separate inherited building class.

## Deterministic Tests

Required before visual polish:

- Empty simulation repeat test.
- Seeded map generation repeat test.
- Capital placement replay test.
- Economy gather replay test.
- Combat replay test.
- Capital destruction replay test.
- Cleanup swap-remove lookup test.
- Command rejection determinism test.
- Headless two-peer lockstep checksum test.

## First Playable Acceptance Criteria

The prototype is considered playable when:

- A player can start nomad and place a Capital.
- Villagers can gather and deposit resources.
- Player can train at least one military unit.
- Military units can destroy enemy buildings.
- Capital loss applies a permanent population penalty.
- Resignation works.
- Match can produce placement results.
- Full match can be replayed from command stream.
- Headless checksum test passes for the same command stream.

## What To Postpone

Do not include in first playable unless the basics are already stable:

- Multiple factions.
- Unique faction mechanics.
- Complex tech trees.
- Advanced formations.
- Full wall blueprint system.
- Trade route economy.
- Ranked matchmaking.
- AI bots.
- Spectator UI.
- Large-player casual mode.

These are important, but they should sit on top of stable deterministic foundations.

## Playability Questions To Test Early

During prototype testing, answer:

- Does Capital placement create meaningful strategic commitment?
- Is Capital loss a turning point without feeling like instant defeat?
- Does the center create pressure without forcing one correct strategy?
- Are flanks defensible without creating stalemates?
- Does finite gold create enough urgency before trade exists?
- Does combat resolve decisively enough for an FFA?
- Does the game reward strategic structure more than reaction speed?
