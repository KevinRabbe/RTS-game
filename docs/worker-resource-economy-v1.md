# Worker / Resource Economy V1

## 1. Design Goal

Capital Frontiers needs a stable, deterministic worker economy model before more visual or UX work is layered on top. The goal of this slice is to make worker tasks feel RTS-like without relying on pixel-perfect movement or hardcoded resource behavior.

Players should command workers to gather from a player-facing resource area. The simulation then chooses exact harvestable nodes, reserves deterministic interaction slots, drives task phases, and handles drop-off through data-driven profiles. Movement should execute movement only; task systems should decide what the worker intends to do.

The first target map is DryArabiaTest01. It should support a clean 5-10 minute economy smoke test: place TC, gather food/wood/gold, deposit, deplete resources, train villagers, and keep running without worker jiggle, desyncs, or invariant failures.

This worker economy is also the first implementation of the broader high-pop traffic contract in [High-Pop Simulation Architecture](high-pop-simulation-architecture.md). Worker slots must scale toward 6-player FFA matches with 200+ population per player and 1200+ active units, so fixes should not become one-off villager hacks.

## 2. ResourceArea / ResourceNode Model

`ResourceKind` is what enters the player stockpile:

- `Food`
- `Wood`
- `Gold`

`ResourceArea` is the player-facing gather target. It is what click/hover and group commands should usually identify.

Examples:

- `Forest`
- `BerryPatch`
- `GoldDeposit`

`ResourceNode` is the actual harvestable object inside an area.

Examples:

- `Tree`
- `BerryBush`
- `GoldVeinSmall`
- `GoldVeinLarge`

Important rules:

- Workers target a `ResourceArea` long-term.
- Workers may internally choose a `CurrentResourceNode`.
- The selected node can change only through deterministic continuation rules.
- Small and large gold veins are both `ResourceKind.Gold`; they differ by node type, profile, amount, footprint, visual, and depletion behavior.
- Future resources such as deer, farms, fish, and stone should fit through areas, nodes, and profiles later without rewriting worker movement.

## 3. GatherProfile Model

`GatherProfile` controls behavior. Worker movement must not hardcode tree, berry, or gold rules.

Suggested profile fields:

- `ProfileId`
- `ResourceKind`
- `CarryCapacity`
- `GatherAmountPerTick`
- `GatherTicksPerAmount` or equivalent rate field if slower cadence is needed
- `DropOffCategory`
- `NodeFootprint`
- `VisualRadiusTiles` or equivalent presentation-only visual overhang metadata
- `BlocksMovement`
- `BlocksPlacement`
- `InteractionSlotShape`
- `DepletedBehavior`
- `AutoContinuationMode`
- `AllowedWorkerTypes`
- `VisualProfileId`

Example profiles:

- `BerryBushProfile`: food, small node, blocks movement, continues within berry patch.
- `TreeProfile`: wood, small node, blocks movement, continues within forest.
- `GoldVeinSmallProfile`: gold, small/medium node, blocks movement, continues within gold deposit.
- `GoldVeinLargeProfile`: gold, larger amount or larger footprint, same stockpile kind.

## 4. Worker Task Phases

Workers need explicit task phases so movement, gather, deposit, and build systems do not fight each other.

Suggested phases:

- `Idle`
- `MovingToResourceSlot`
- `Gathering`
- `MovingToDropoffSlot`
- `Depositing`
- `MovingToBuildSlot`
- `Building`
- `MovingToCommandMove`
- `BlockedWaiting`

Each phase owns a narrow responsibility:

- Task systems set intent and phase.
- Movement system advances position toward the current movement target.
- Gather/deposit/build systems perform work only when the worker is in a valid interaction slot/range.
- A worker in a valid interaction range should stop movement and perform the task.

Current implementation notes:

- `Unit.TaskPhase` stores the deterministic worker phase and is included in checksums.
- Gather, deposit, and construction systems set action phases when the unit is already in the matching interaction range.
- Command movement uses `MovingToCommandMove`; task slot movement uses the matching `MovingTo*Slot` phase.
- Temporary movement blockage can set `BlockedWaiting` without clearing the worker's resource or build intent.

## 5. Interaction Slot Reservations

Resource nodes, drop-off buildings, and foundations expose deterministic interaction slots or rings.

Rules:

- Workers reserve one interaction slot for their current task.
- Reserved slots are unavailable to other workers.
- Slot ordering is deterministic and stable.
- Slot selection uses deterministic tie-breaks such as distance, slot order, unit id.
- Workers retain useful slots and release them only for clear reasons.
- No unit stacking.

Slot release reasons:

- Task completes.
- Command is replaced.
- Target area/node/building becomes invalid.
- Resource node is depleted and continuation chooses a different node.
- Drop-off/build target is destroyed or no longer valid.
- Worker has been blocked beyond a deterministic retry threshold.

Temporary congestion should move a worker to `BlockedWaiting` or trigger bounded deterministic retargeting. It should not erase the worker's resource/build intent.

Current implementation notes:

- Each worker stores one deterministic reservation in simulation state: kind, target id, and tile.
- Supported reservation kinds are `ResourceNode`, `Dropoff`, and `BuildSite`.
- These correspond to the high-pop traffic concepts `ResourceInteraction`, `DropoffInteraction`, and `BuildInteraction`.
- Reservation state is included in checksums because it affects future movement and task decisions.
- Slot selection filters out blocked footprint tiles, occupied tiles, and tiles reserved by other live workers.
- Candidate ordering is deterministic: nearest tile from the worker's current tile first, then stable tile order by Y and X.
- Gather, drop-off, and construction systems retain a valid reservation and only re-slot when the target changes, the slot becomes invalid/occupied/reserved by another worker, or bounded no-progress retry allows retargeting.
- When bounded no-progress triggers, the next reservation pass excludes the stale tile first and only falls back to it if no other reachable slot exists. This prevents a worker from releasing and immediately reclaiming the same bad slot forever.

## 6. Movement Contract

Movement executes movement only.

Movement must not decide resource continuation, drop-off choice, gather progress, build progress, or task cancellation except for clearly invalid movement targets.

Rules:

- Movement follows `MoveTarget` or a task-owned slot target.
- Movement arrival uses deterministic tolerance or snap, not raw exact pixel equality.
- No pixel-perfect interaction requirement.
- If a worker is in valid interaction range, task systems should clear movement and do task work.
- Pathfinding uses shared blocker truth from map bounds, building footprints, resource footprints, walls, and live units.
- No stacking.
- Blocked/no-progress handling is bounded and deterministic.

Current implementation notes:

- Movement snaps to the exact `MoveTarget` when the remaining deterministic fixed-point distance is within the unit's per-tick movement step.
- Task systems own interaction actions: being in valid resource, drop-off, or build range clears movement and performs the task instead of chasing exact raw coordinates.
- Reaching a command-move target returns the unit to `Idle`; reaching a task slot leaves the task phase for the gather/deposit/build system to resolve.

The desired no-jiggle contract:

- A worker should not pick a new slot every tick.
- A worker should not oscillate between resource and drop-off without a carry/deposit state change.
- A worker should not clear `ResourceArea`, `CurrentResourceNode`, or `BuildTarget` because another unit temporarily occupies a tile.
- A worker may wait briefly, then deterministically retry or choose another valid reserved slot.

## 7. Drop-Off Contract

Drop-off targets are buildings or future objects that accept one or more `DropOffCategory` values.

Initial expected drop-offs:

- Completed own Town Center accepts food, wood, and gold.
- Future buildings can opt into categories through data.

Rules:

- A full worker chooses a valid drop-off target deterministically, usually nearest valid own TC.
- The whole building footprint is the drop-off target, exposed through interaction slots around that footprint.
- Workers deposit when in a valid drop-off interaction slot/range.
- Workers do not move to the visual center of the TC.
- After deposit, a worker returns to the same long-term `ResourceArea` and, if still valid, the same `CurrentResourceNode`.
- If that node is depleted, continuation rules choose another valid node in the same area.

## 8. Resource Depletion Rules

Resource depletion should be deterministic and data-driven.

Rules:

- Nodes track remaining amount.
- Workers gather only from their assigned/current node while in a valid slot.
- When a node reaches zero, it becomes depleted and releases blockers/slots according to its profile.
- Area continuation chooses the next node using deterministic ordering and reachability.
- If no valid node remains in the area, the worker becomes idle or enters a clear terminal phase.
- The worker should not silently switch to a different nearby same-kind area unless the command/system explicitly allows it.

## 9. Simulation Geometry vs Visual Geometry

These are separate concepts:

- Visual sprite
- Click bounds
- Simulation footprint
- Path blocker footprint
- Interaction slots
- Movement target

Rules:

- Simulation uses tile footprints and interaction slots for worker economy logic.
- Visual sprite colliders must not drive economy behavior.
- Click bounds may be generous so the player can comfortably select resources/buildings.
- Visual sprites may overhang their simulation footprint.
- Pathfinding uses shared blocker truth from footprints and live units.
- Interaction slots should be visually plausible, but they are simulation concepts first.

Current implementation notes:

- `GatherProfile.FootprintRadiusTiles` controls resource simulation footprint.
- `GatherProfile.VisualRadiusTiles` is presentation-facing metadata and must not decide movement/path blockers.
- `SpatialRules.EnumerateResourceFootprintTiles(...)` and `SpatialRules.EnumerateBuildingFootprintTiles(...)` expose deterministic simulation footprints.
- `SpatialRules.EnumerateResourceInteractionTiles(...)` and `SpatialRules.EnumerateBuildingInteractionTiles(...)` derive rings from simulation footprints.
- Placement and pathfinding read the simulation footprint/blocker contract, not Godot primitive sizes or click bounds.

This separation lets a gold sprite look large without forcing workers to path to sprite pixels, and lets a TC look like a large building while exposing stable slots around its footprint.

## 10. DryArabiaTest01 Map Design Rules

DryArabiaTest01 is the first playable economy-loop map, not a full terrain engine.

Rules:

- Use generated tiles as visual terrain/decorative layer.
- Gameplay blockers come from explicit objects: trees, berries, gold, buildings, walls, and map bounds.
- Visual tiles must not create surprise blockers.
- Do not hide blockers with decorative terrain.
- Keep the first TC area clear enough for workers to build, gather, deposit, and train.
- Home resources should be close enough for testing but not so tight that they seal TC interaction slots.
- Food, wood, and gold clusters should have readable visual identity and clear worker lanes.

First playable economy loop:

- Place TC.
- Workers build TC.
- Workers gather food, wood, and gold.
- Workers deposit at main TC.
- Resources deplete.
- TC trains villagers.
- Rectangle select comes later.
- Second TC comes later.
- A 5-10 minute smoke test runs without jiggle, desync, or invariant failures.

## 11. Implementation Phases

Planned issue order:

- Phase 7A: Architecture plan. This document.
- Phase 7B: Data-driven resource areas and nodes.
- Phase 7E: Simulation geometry vs visual geometry.
- Phase 7C: Worker interaction slot reservations.
- Phase 7D: Worker task phases and no-jiggle movement contract.
- Phase 7S: High-pop simulation split and unit traffic architecture.
- Phase 7F: Resource depletion and area continuation.
- Phase 7G: Town Center villager production and spawn slots.
- Phase 7H: Rectangle selection and group gather commands.
- Phase 7I: DryArabiaTest01 playable economy layout.

Implementation guidance:

- Work one issue at a time.
- Keep commits small and reviewable.
- Preserve determinism at every step.
- Keep command validation authoritative.
- Avoid broad refactors until the new model needs them.

## 12. Test Checklist

Core resource model:

- Resource areas expose stable ids, kind, node list, and click target data.
- Resource nodes expose stable ids, node type, profile, amount, footprint, and depleted state.
- Small and large gold both deposit into `ResourceKind.Gold`.
- Food, wood, and gold behavior comes from profiles.

Worker target persistence:

- Worker gather command sets long-term resource area target.
- Worker chooses current node deterministically.
- Worker keeps target through gather, drop-off, deposit, and return.
- Worker does not switch to a nearer same-kind area while assigned area remains valid.

Interaction slots:

- Resource nodes expose deterministic slots.
- Drop-off buildings expose deterministic slots.
- Foundations expose deterministic slots.
- Multiple workers reserve distinct slots where possible.
- Reserved slots are not assigned to other workers.
- Slot release happens for explicit reasons only.

Movement and no-jiggle:

- Worker moving to resource slot does not retarget every tick.
- Worker moving to drop-off slot does not retarget every tick.
- Worker blocked by temporary congestion keeps intent.
- Worker blocked beyond retry threshold retargets deterministically.
- No stacking occurs.
- Movement arrival uses deterministic tolerance/snap.

Gather/deposit/build:

- Worker gathers only in valid resource slot/range.
- Worker deposits only in valid drop-off slot/range.
- Worker builds only in valid foundation slot/range.
- Full worker deposits at nearest valid own drop-off.
- After deposit, worker returns to the same resource area/node if valid.

DryArabiaTest01:

- First TC can be placed and built.
- Starting workers can gather food, wood, and gold.
- Workers deposit at TC.
- Resources deplete and workers continue within area if possible.
- TC trains villagers.
- Long smoke test has no desyncs or invariant failures.

Determinism:

- Same command sequence gives same checksum.
- Slot assignment is stable.
- Resource continuation is stable.
- Stress scenario `chaos-v4` remains green.

High-pop readiness:

- Worker traffic does not rely on presentation colliders or sprite bounds.
- Slot ownership has deterministic conflict resolution.
- Temporary traffic does not clear long-term gather/build intent.
- Resource, drop-off, and build logic avoid per-tick retarget churn.
- Any new worker loop change answers the high-pop scale checklist.
