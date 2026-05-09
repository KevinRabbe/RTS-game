# Development Roadmap

This roadmap turns the RTS plan into staged work toward a playable competitive prototype.

The project follows the architecture constitution:

- Simulation is deterministic, engine-agnostic, and command-driven.
- Simulation runs at 20 ticks per second.
- Networking uses deterministic lockstep, not state synchronization.
- Presentation reads simulation state and never mutates it.
- Features are added with data plus systems, not inheritance-heavy models.

## Product Target

Primary mode:

- 6-player ranked free-for-all.
- Nomad start with 4 villagers and 1 scout.
- First placed Town Center becomes the player's Capital.
- Target match length: 25-35 minutes.

Win feeling:

1. Strategic superiority.
2. Execution in key moments.
3. Survival under pressure.

## Prototype Strategy

Build the prototype in vertical slices. Each slice must remain replayable from command stream alone before the next slice expands gameplay.

Avoid building the full RTS at once. The first playable goal is a small deterministic match that proves:

- Players can place a Capital.
- Villagers gather and deposit resources.
- Units can move, attack, and destroy buildings.
- Capital destruction creates a major penalty without instant defeat.
- A replay can reproduce the match exactly.
- A lockstep test can run the same command stream on multiple peers with matching checksums.

## Milestone 0: Project Foundation

Goal: Establish the architecture guardrails before gameplay code grows.

Deliverables:

- Simulation-only project/module with no engine dependencies.
- Presentation project/module that can read snapshots only.
- Shared command serialization format.
- Deterministic integer or fixed-point math utilities.
- Seeded deterministic RNG.
- Fixed-order simulation tick runner.
- Basic checksum generation for desync detection.
- Minimal two-peer headless lockstep harness.
- Headless simulation test runner.

Acceptance criteria:

- Simulation can run 10,000 empty ticks and produce the same checksum every run.
- No engine types are referenced by simulation code.
- Systems are stateless functions over `GameState`.
- Replay file with no commands reproduces the same state hash.
- Two simulated peers can advance through the same empty command stream with matching checksums.
- Missing input for a lockstep tick stalls advancement instead of guessing.

## Milestone 1: Nomad Start and Capital Placement

Goal: Make the opening structure of the game playable.

Features:

- 6 player slots.
- Structured semi-random spawn sectors.
- Initial units: 4 villagers and 1 scout per player.
- Command: place building blueprint.
- Command: assign villagers to build.
- First completed Town Center becomes Capital.
- Capital grants population bonus.
- Capital cannot be rebuilt.

Systems:

- CommandValidationSystem.
- BuildingPlacementSystem.
- ConstructionSystem.
- CapitalSystem.
- PopulationSystem.
- CleanupSystem.

Acceptance criteria:

- Each player can place exactly one first Capital.
- Later Town Centers are normal Town Centers.
- Destroyed Capital permanently removes its bonus.
- Replay of Capital placement is byte-for-byte deterministic at checksum points.
- Two simulated peers can place Capitals through the lockstep harness with matching checksums.

## Milestone 2: Core Economy Loop

Goal: Create the minimum economy needed to support decisions.

Features:

- Resources: food, wood, gold.
- Villager gather commands.
- Resource nodes for food, wood, and finite gold.
- Drop-off at Town Centers.
- Unit and building costs.
- Shared population cap.
- Train villager command.

Systems:

- ResourceGatherSystem.
- ResourceDepositSystem.
- TrainingSystem.
- CostPaymentSystem.
- PopulationSystem.

Acceptance criteria:

- Villagers gather resources deterministically.
- Gold mines deplete and remain depleted in replay.
- Units cannot train without resources and population room.
- Economy state can be reconstructed from command stream only.

## Milestone 3: Movement, Visibility, and Map Control

Goal: Make scouting and territory matter.

Features:

- Deterministic grid or nav-cell movement.
- Unit move command.
- Fog of war state.
- Scout vision.
- Map sectors with center, flank, choke, and high-value region tags.
- Neutral trade posts and resource clusters placed by seeded map generation.

Systems:

- MovementSystem.
- VisibilitySystem.
- MapControlSystem.

Acceptance criteria:

- Unit movement produces identical final positions across repeated runs.
- Visibility is derived only from simulation state.
- No presentation-layer reveal logic affects gameplay.
- Generated maps are fair but not mirrored.

## Milestone 4: Combat and Building Pressure

Goal: Create the first combat loop.

Features:

- Infantry unit.
- Cavalry unit.
- Basic attack command.
- Target acquisition through commands or deterministic rules.
- Building attack and destruction.
- No friendly fire.
- Capital destruction penalty.
- Player remains alive if other Town Centers exist.

Systems:

- AttackCommandSystem.
- CombatResolutionSystem.
- DamageSystem.
- DeathMarkSystem.
- CapitalLossSystem.
- CleanupSystem.

Acceptance criteria:

- Combat outcome is deterministic with identical command streams.
- Capital destruction causes permanent modifier loss, not instant defeat.
- Destroyed entities are marked dead first and removed only by CleanupSystem.
- Placement ranking and elimination contribution can be recorded.

## Milestone 5: Walls and Siege

Goal: Add breakthrough tools and defensive counterplay.

Features:

- Quick wall blueprint command.
- Walls are vulnerable while building.
- Wall upgrade command.
- Trebuchet or cannon with setup time and long reload.
- Mangonel with area damage and no setup.
- Siege population costs.

Systems:

- WallBlueprintSystem.
- WallUpgradeSystem.
- SiegeSetupSystem.
- SiegeAttackSystem.
- AreaDamageSystem.

Acceptance criteria:

- Siege setup and reload are tick-count based, never time-delta based.
- Mangonel area damage is deterministic and has no friendly fire.
- Walls create meaningful delay but can be broken.
- Siege outcomes replay exactly.

## Milestone 6: Trade and Late Game

Goal: Make late game depend on map control instead of infinite mining.

Features:

- Trade post structures.
- Trade unit training.
- Trade route command between posts.
- Longer physical route gives more income.
- Trade units are vulnerable.
- Trade route validity depends on map access and alive trade endpoints.

Systems:

- TradeRouteSystem.
- TradeMovementSystem.
- TradeIncomeSystem.
- RouteValidationSystem.

Acceptance criteria:

- Trade income is deterministic and based on route length.
- Destroyed endpoints invalidate routes.
- Trade route control creates a reason to fight over center and flanks.
- Late game economy remains possible after gold mines deplete.

## Milestone 7: Lockstep Multiplayer

Goal: Run real multiplayer matches using command lockstep.

Features:

- Future-tick input scheduling.
- Local input delay.
- Per-tick command collection.
- Peer readiness tracking.
- Simulation advances only when all required inputs are available.
- Periodic checksum comparison.
- Desync report tooling.

Networking rules:

- Send commands, never authoritative state.
- Never use networking callbacks to mutate `GameState` directly.
- Network layer queues commands for future simulation ticks.

Acceptance criteria:

- Two or more local clients can run the same match with matching checksums.
- Artificial latency does not change simulation result.
- Missing input stalls simulation instead of guessing.
- Desync reports include tick, checksum, player commands, and seed.

## Milestone 8: Competitive FFA Prototype

Goal: Reach a complete 6-player FFA prototype.

Features:

- 6-player match setup.
- Resignation command.
- Resigned player's units turn neutral and despawn after 60 seconds.
- Placement tracking.
- Elimination contribution tracking.
- Match end detection.
- Basic ranked-result payload.

Acceptance criteria:

- Match can end decisively without requiring full annihilation of every asset.
- Ranking uses placement first and elimination contribution second.
- Replay can reproduce the full match.
- Spectator replay can read command stream without simulation mutation.

## Expansion Backlog

Add only after multiplayer, replay, and deterministic foundations are stable:

- Additional factions.
- Unique faction mechanics.
- Capital rule variants.
- AI bots.
- Spectator tools.
- Larger casual modes.
- 1v1 and 2v2 queues.

## Risk Register

Highest-risk areas:

- Deterministic movement and pathing.
- Lockstep input delay and stall behavior.
- Map generation fairness without mirroring.
- Siege area damage determinism.
- Trade route calculation.
- Replay compatibility after gameplay changes.

Risk policy:

- Prefer simple deterministic approximations over complex non-deterministic realism.
- Prove replay and checksum behavior before expanding feature complexity.
- Keep simulation free from rendering, engine physics, wall-clock time, async callbacks, and unordered iteration.
