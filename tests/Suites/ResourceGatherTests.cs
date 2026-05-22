using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Systems;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void GatherWaitsForCompletedTownCenter()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(1, 1);

            var buffer = new CommandBuffer();

            var runner = new TickRunner();



            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            runner.AdvanceOneTick(state, rules, buffer);

            runner.AdvanceOneTick(state, rules, buffer);



            AssertEqual(0, state.EntityState.Units[0].CarriedAmount, "villager should not gather until reaching resource interaction range");

            AssertEqual(0, state.PlayerStates.Players[0].Resources.Food, "food should not deposit before TC completion");

            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "villager should be moving toward resource");

        }

        private static void VillagersGatherAndDepositFood()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(1, 1);

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(12, 12));

            ResourceNode foodNode = FindResourceNodeById(state, 1);

            int foodTileX = SpatialRules.GetTileX(foodNode.Position);

            int foodTileY = SpatialRules.GetTileY(foodNode.Position);

            state.EntityState.Units[0].Position = FixedVector2.FromInts(foodTileX - 2, foodTileY);

            state.EntityState.Units[1].Position = FixedVector2.FromInts(foodTileX - 2, foodTileY + 1);

            int foodBefore = state.PlayerStates.Players[0].Resources.Food;

            var buffer = new CommandBuffer();

            var runner = new TickRunner();



            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2 })));

            runner.AdvanceOneTick(state, rules, buffer);

            for (int i = 0; i < 1000 && state.PlayerStates.Players[0].Resources.Food < foodBefore + 10; i++)

            {

                AddNoOp(buffer, state.Tick, 0, (uint)(1 + i));

                runner.AdvanceOneTick(state, rules, buffer);

            }



            AssertEqual(true, state.PlayerStates.Players[0].Resources.Food >= foodBefore + 10, "at least one villager should complete gather and deposit loop");

            AssertEqual(true, state.EconomyState.ResourceNodes[0].RemainingAmount <= GameData.StartingFoodAmount - GameData.VillagerCarryCapacity, "food node should lose at least one full carried amount");

            AssertEqual(true, state.EntityState.Units[0].CarriedAmount == 0 || state.EntityState.Units[1].CarriedAmount == 0, "at least one villager should have deposited and emptied carry");

            AssertEqual(true, state.EntityState.Units[1].CurrentResourceNodeId == 1, "second villager should keep gather assignment");

        }

        private static void GatherRejectsCarriedDifferentResource()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2, 1);

            state.EntityState.Units[0].CarriedResourceType = ResourceType.Food;

            state.EntityState.Units[0].CarriedAmount = 5;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(3, new[] { 1 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "villager should not accept incompatible gather order");

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "incompatible gather order should count as rejected");

        }

        private static void GatherAcceptsMixedSelectionWithIncompatibleCarry()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(201, 1);

            int woodNodeId = FindFirstResourceNodeIdByType(state, ResourceType.Wood);

            state.EntityState.Units[0].CarriedResourceType = ResourceType.Gold;

            state.EntityState.Units[0].CarriedAmount = 2;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.GatherResource), new GatherResourceCommand(woodNodeId, new[] { 1, 2 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "mixed gather selection should accept when at least one villager is eligible");

            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "incompatible carrier should keep current assignment");

            AssertEqual(woodNodeId, state.EntityState.Units[1].CurrentResourceNodeId, "eligible villager should receive wood gather assignment");

        }

        private static void GatherRejectReasonForDepletedResourceIsTargetComplete()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateTwoNodeResourceAreaState(3199, GatherProfileId.Tree, out int firstNodeId, out _, out _);

            FindResourceNodeById(state, firstNodeId).RemainingAmount = 0;

            var header = new CommandHeader(state.Tick, 0, 0, CommandType.GatherResource);

            var command = new GatherResourceCommand(firstNodeId, new[] { 1 });



            CommandValidationReport report = CommandValidationInspector.Evaluate(

                state,

                rules,

                new CommandEnvelope(header, command));



            AssertEqual(false, report.Accepted, "depleted node gather should reject");

            AssertEqual(CommandValidationReason.TargetComplete, report.Reason, "depleted node gather should report target complete");

            AssertEqual(firstNodeId, report.TargetEntityId, "gather rejection should include target resource id");

        }

        private static void DepletedResourceClearsGatherAssignment()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(3, 1);

            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);

            state.EconomyState.ResourceNodes[0].RemainingAmount = GameData.VillagerGatherPerTick;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(0, state.EconomyState.ResourceNodes[0].RemainingAmount, "resource node should deplete");

            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "depleted node should clear gather assignment");

            AssertEqual(0, state.EntityState.Units[0].CurrentResourceAreaId, "exhausted area should clear long-term gather assignment");

        }

        private static void TreeDepletionReducesAmountAndUnblocksFootprint()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateTwoNodeResourceAreaState(3101, GatherProfileId.Tree, out int firstNodeId, out _, out int areaId);

            ResourceNode first = FindResourceNodeById(state, firstNodeId);

            Unit worker = state.EntityState.Units[0];

            worker.Position = new FixedVector2(first.Position.X + Fixed.FromInt(1), first.Position.Y);

            first.RemainingAmount = GameData.VillagerGatherPerTick;



            AssertEqual(true, SpatialRules.IsTileBlockedByResource(state, SpatialRules.GetTileX(first.Position), SpatialRules.GetTileY(first.Position)), "active tree should block its footprint");

            RunGatherCommand(state, rules, firstNodeId, worker.Id);



            AssertEqual(0, first.RemainingAmount, "tree gather should reduce remaining amount to zero");

            AssertEqual(true, first.IsDepleted, "tree should be marked depleted");

            AssertEqual(false, SpatialRules.IsTileBlockedByResource(state, SpatialRules.GetTileX(first.Position), SpatialRules.GetTileY(first.Position)), "depleted tree should unblock its footprint");

            AssertEqual(areaId, worker.CurrentResourceAreaId, "worker should keep long-term forest target while another node remains");

        }

        private static void DepletedResourceRejectsGatherCommand()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateTwoNodeResourceAreaState(3102, GatherProfileId.Tree, out int firstNodeId, out _, out _);

            FindResourceNodeById(state, firstNodeId).RemainingAmount = 0;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(firstNodeId, new[] { 1 })));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "depleted node should reject gather command");

            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "rejected depleted node should not assign gather target");

        }

        private static void ForestContinuationChoosesAnotherTree()

        {

            AssertResourceContinuationChoosesAnotherNode(GatherProfileId.Tree, ResourceType.Wood, 3103);

        }

        private static void BerryPatchContinuationChoosesAnotherBush()

        {

            AssertResourceContinuationChoosesAnotherNode(GatherProfileId.BerryBush, ResourceType.Food, 3104);

        }

        private static void GoldDepositContinuationChoosesAnotherVein()

        {

            AssertResourceContinuationChoosesAnotherNode(GatherProfileId.GoldVeinSmall, ResourceType.Gold, 3105);

        }

        private static void ResourceAreaExhaustionIdlesWorkerCleanly()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateSingleNodeResourceAreaState(3106, GatherProfileId.BerryBush, out int nodeId, out _);

            ResourceNode node = FindResourceNodeById(state, nodeId);

            Unit worker = state.EntityState.Units[0];

            worker.Position = new FixedVector2(node.Position.X + Fixed.FromInt(1), node.Position.Y);

            node.RemainingAmount = GameData.VillagerGatherPerTick;



            RunGatherCommand(state, rules, nodeId, worker.Id);



            AssertEqual(0, worker.CurrentResourceNodeId, "exhausted area should clear current node");

            AssertEqual(0, worker.CurrentResourceAreaId, "exhausted area should clear long-term area target");

            AssertEqual(false, worker.HasMoveTarget, "exhausted area should not leave stale movement");

            AssertEqual(WorkerTaskPhase.Idle, worker.TaskPhase, "exhausted area should idle worker cleanly");

        }

        private static void GatherCommandKeepsSelectedFoodTargetId()

        {

            AssertGatherCommandKeepsSelectedResourceTarget(ResourceType.Food, 2061);

        }

        private static void GatherCommandKeepsSelectedWoodTargetId()

        {

            AssertGatherCommandKeepsSelectedResourceTarget(ResourceType.Wood, 2062);

        }

        private static void GatherCommandKeepsSelectedGoldTargetId()

        {

            AssertGatherCommandKeepsSelectedResourceTarget(ResourceType.Gold, 2063);

        }

        private static void GatherCommandSetsMovementTowardResource()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(201, 1);

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(12, 12));

            ResourceNode foodNode = FindResourceNodeById(state, 1);

            int foodTileX = SpatialRules.GetTileX(foodNode.Position);

            int foodTileY = SpatialRules.GetTileY(foodNode.Position);

            state.EntityState.Units[0].Position = FixedVector2.FromInts(foodTileX - 3, foodTileY);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            Unit unit = state.EntityState.Units[0];

            AssertEqual(1, unit.CurrentResourceNodeId, "gather assignment should be set");

            AssertEqual(

                true,

                unit.HasMoveTarget

                    || unit.TaskPhase == WorkerTaskPhase.Gathering

                    || unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode,

                "gather command should set movement, gather immediately, or hold a valid resource reservation");

        }

        private static void GatherMoveTargetUsesResourceInteractionRing()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2064, 1);

            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);

            ResourceNode node = FindResourceNodeById(state, resourceId)!;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            Unit unit = state.EntityState.Units[0];

            if (unit.HasMoveTarget)

            {

                int targetX = SpatialRules.GetTileX(unit.MoveTarget);

                int targetY = SpatialRules.GetTileY(unit.MoveTarget);

                AssertEqual(false, SpatialRules.IsTileInsideResourceFootprint(node, targetX, targetY), "resource approach tile should not be inside resource footprint");

                AssertEqual(true, SpatialRules.IsTileAdjacentToResourceFootprint(node, targetX, targetY), "resource approach tile should be adjacent to footprint");

                return;

            }



            int unitTileX = SpatialRules.GetTileX(unit.Position);

            int unitTileY = SpatialRules.GetTileY(unit.Position);

            if (SpatialRules.IsTileAdjacentToResourceFootprint(node, unitTileX, unitTileY))

            {

                return;

            }



            AssertEqual(InteractionReservationKind.ResourceNode, unit.ReservedInteractionKind, "when move target is absent and unit is not adjacent it should still hold a resource reservation");

            AssertEqual(node.Id, unit.ReservedInteractionTargetId, "resource reservation should remain on the requested node");

            AssertEqual(true, SpatialRules.IsTileAdjacentToResourceFootprint(node, unit.ReservedInteractionTileX, unit.ReservedInteractionTileY), "reserved resource slot should stay on the interaction ring");

        }

        private static void MultipleWorkersReserveDistinctResourceSlots()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2081, 1);

            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1, 2, 3 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertDistinctReservations(state, new[] { 1, 2, 3 }, InteractionReservationKind.ResourceNode, resourceId, "resource gatherers should reserve different slots");

        }

        private static void MultipleWorkersOnSameResourceDoNotStack()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2082, 1);

            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1, 2, 3 })));

            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 1; tick <= 80; tick++)

            {

                AddNoOp(buffer, tick, 0, (uint)(20820 + tick));

                runner.AdvanceOneTick(state, rules, buffer);

                AssertNoLiveUnitStacking(state, "workers on same resource should not stack while moving/gathering");

            }



            AssertEqual(resourceId, state.EntityState.Units[0].CurrentResourceNodeId, "first worker should keep resource target");

            AssertEqual(resourceId, state.EntityState.Units[1].CurrentResourceNodeId, "second worker should keep resource target");

            AssertEqual(resourceId, state.EntityState.Units[2].CurrentResourceNodeId, "third worker should keep resource target");

        }

        private static void StaleResourceSlotReservationChoosesAlternate()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(2093, 1);

            ResourceNode node = CreateTestResourceNode(state, GatherProfileId.BerryBush, FixedVector2.FromInts(10, 10), GameData.StartingFoodAmount);

            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 7));

            Unit worker = FindUnitById(state, workerId);

            SpatialRules.TileCoord staleTile = new SpatialRules.TileCoord(10, 9);

            worker.CurrentResourceNodeId = node.Id;

            worker.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;

            worker.HasMoveTarget = true;

            worker.MoveTarget = FixedVector2.FromInts(staleTile.X, staleTile.Y);

            worker.LastMovedTick = 0;

            SpatialRules.ReserveInteractionSlot(state, worker, InteractionReservationKind.ResourceNode, node.Id, staleTile);

            state.Tick = GameData.NoProgressTimeoutTicks;



            new ResourceGatherSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));



            AssertEqual(node.Id, worker.CurrentResourceNodeId, "stale slot retarget should preserve exact resource target");

            AssertEqual(InteractionReservationKind.ResourceNode, worker.ReservedInteractionKind, "worker should keep a resource reservation");

            AssertEqual(node.Id, worker.ReservedInteractionTargetId, "worker should keep reservation on the same node");

            AssertEqual(true, worker.HasMoveTarget, "worker should keep movement intent after stale resource slot retarget");

            bool changedSlot = worker.ReservedInteractionTileX != staleTile.X || worker.ReservedInteractionTileY != staleTile.Y;

            AssertEqual(true, changedSlot, "timed-out resource reservation should prefer another valid slot before reusing stale tile");

            AssertEqual(true, SpatialRules.IsTileAdjacentToResourceFootprint(node, worker.ReservedInteractionTileX, worker.ReservedInteractionTileY), "alternate resource slot should be adjacent to the resource footprint");

        }

        private static void StaleResourceSlotReservationFallsBackWhenOnlySlotRemains()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(20931, 1);

            ResourceNode node = CreateTestResourceNode(state, GatherProfileId.BerryBush, FixedVector2.FromInts(10, 10), GameData.StartingFoodAmount);

            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 7));

            Unit worker = FindUnitById(state, workerId);

            SpatialRules.TileCoord staleTile = new SpatialRules.TileCoord(10, 9);

            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, node);

            for (int i = 0; i < interactionTiles.Count; i++)

            {

                SpatialRules.TileCoord tile = interactionTiles[i];

                if (tile.X == staleTile.X && tile.Y == staleTile.Y)

                {

                    continue;

                }



                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(tile.X, tile.Y));

            }



            worker.CurrentResourceNodeId = node.Id;

            worker.CurrentResourceAreaId = node.ResourceAreaId;

            worker.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;

            worker.HasMoveTarget = true;

            worker.MoveTarget = FixedVector2.FromInts(staleTile.X, staleTile.Y);

            worker.LastMovedTick = 0;

            SpatialRules.ReserveInteractionSlot(state, worker, InteractionReservationKind.ResourceNode, node.Id, staleTile);

            state.Tick = GameData.NoProgressTimeoutTicks + 1;



            new ResourceGatherSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));



            AssertEqual(node.Id, worker.CurrentResourceNodeId, "fallback should preserve resource node intent");

            AssertEqual(InteractionReservationKind.ResourceNode, worker.ReservedInteractionKind, "fallback should keep a resource reservation");

            AssertEqual(node.Id, worker.ReservedInteractionTargetId, "fallback should keep reservation target");

            AssertEqual(staleTile.X, worker.ReservedInteractionTileX, "fallback should reuse the only viable slot");

            AssertEqual(staleTile.Y, worker.ReservedInteractionTileY, "fallback should reuse the only viable slot");

            AssertEqual(true, worker.HasMoveTarget, "fallback should keep movement toward viable slot");

            AssertEqual(WorkerTaskPhase.MovingToResourceSlot, worker.TaskPhase, "worker should continue moving toward the viable slot");

        }

        private static void StaleResourceSlotRetargetCanSwitchToSiblingNode()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(20932, 1);

            int areaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(12, 10));

            int firstNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(10, 10), GameData.StartingFoodAmount);

            int secondNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(14, 10), GameData.StartingFoodAmount);

            ResourceNode firstNode = FindResourceNodeById(state, firstNodeId);

            ResourceNode secondNode = FindResourceNodeById(state, secondNodeId);



            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 7));

            Unit worker = FindUnitById(state, workerId);

            SpatialRules.TileCoord staleTile = new SpatialRules.TileCoord(10, 9);

            List<SpatialRules.TileCoord> firstInteractionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, firstNode);

            for (int i = 0; i < firstInteractionTiles.Count; i++)

            {

                SpatialRules.TileCoord tile = firstInteractionTiles[i];

                if (tile.X == staleTile.X && tile.Y == staleTile.Y)

                {

                    continue;

                }



                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(tile.X, tile.Y));

            }



            worker.CurrentResourceAreaId = areaId;

            worker.CurrentResourceNodeId = firstNode.Id;

            worker.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;

            worker.HasMoveTarget = true;

            worker.MoveTarget = FixedVector2.FromInts(staleTile.X, staleTile.Y);

            worker.LastMovedTick = 0;

            SpatialRules.ReserveInteractionSlot(state, worker, InteractionReservationKind.ResourceNode, firstNode.Id, staleTile);

            state.Tick = GameData.NoProgressTimeoutTicks + 1;



            new ResourceGatherSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));



            AssertEqual(true, worker.CurrentResourceNodeId == firstNode.Id || worker.CurrentResourceNodeId == secondNode.Id, "worker should keep a valid node assignment");

            AssertEqual(secondNode.Id, worker.CurrentResourceNodeId, "retarget pass should switch to sibling node when preferred node only has evicted stale slot");

            AssertEqual(InteractionReservationKind.ResourceNode, worker.ReservedInteractionKind, "retarget should keep resource reservation");

            AssertEqual(secondNode.Id, worker.ReservedInteractionTargetId, "reservation target should move to sibling node");

            AssertEqual(true, worker.HasMoveTarget, "worker should continue moving after sibling retarget");

            AssertEqual(true, SpatialRules.IsTileAdjacentToResourceFootprint(secondNode, worker.ReservedInteractionTileX, worker.ReservedInteractionTileY), "reserved sibling tile should be adjacent to sibling node footprint");

        }

        private static void BlockedWaitingGatherCanFallbackToSiblingNodeWithoutReservation()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(20933, 1);

            int areaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(12, 10));

            int firstNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(10, 10), GameData.StartingFoodAmount);

            int secondNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(14, 10), GameData.StartingFoodAmount);

            ResourceNode firstNode = FindResourceNodeById(state, firstNodeId);

            ResourceNode secondNode = FindResourceNodeById(state, secondNodeId);



            List<SpatialRules.TileCoord> firstInteractionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, firstNode);

            for (int i = 0; i < firstInteractionTiles.Count; i++)

            {

                SpatialRules.TileCoord tile = firstInteractionTiles[i];

                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(tile.X, tile.Y));

            }



            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 7));

            Unit worker = FindUnitById(state, workerId);

            worker.CurrentResourceAreaId = areaId;

            worker.CurrentResourceNodeId = firstNode.Id;

            worker.TaskPhase = WorkerTaskPhase.BlockedWaiting;

            worker.HasMoveTarget = false;

            worker.LastMovedTick = 0;

            worker.LastReservationFailureTick = state.Tick - GameData.ReservationRetargetCadenceTicks - 1;

            worker.LastReservationFailureReason = ReservationAttemptFailureReason.SlotUnavailable;

            state.Tick = GameData.NoProgressTimeoutTicks + GameData.ReservationRetargetCadenceTicks + 1;



            new ResourceGatherSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));



            AssertEqual(secondNode.Id, worker.CurrentResourceNodeId, "blocked waiting worker without reservation should fallback to sibling node");

            AssertEqual(InteractionReservationKind.ResourceNode, worker.ReservedInteractionKind, "fallback should assign a resource reservation");

            AssertEqual(secondNode.Id, worker.ReservedInteractionTargetId, "reservation target should follow sibling node");

            AssertEqual(WorkerTaskPhase.MovingToResourceSlot, worker.TaskPhase, "worker should resume moving toward sibling slot");

            AssertEqual(true, worker.HasMoveTarget, "worker should resume movement after fallback");

            AssertEqual(true, SpatialRules.IsTileAdjacentToResourceFootprint(secondNode, worker.ReservedInteractionTileX, worker.ReservedInteractionTileY), "reserved tile should be adjacent to sibling node footprint");

        }

        private static void InteractionReservationScoringPrefersLessCongestedTile()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3121, 1);

            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 10));

            Unit worker = FindUnitById(state, workerId);

            var tiles = new List<SpatialRules.TileCoord>

            {

                new SpatialRules.TileCoord(12, 10),

                new SpatialRules.TileCoord(10, 12)

            };



            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 9), false);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(13, 10), false);

            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());



            bool reserved = SpatialRules.TryReserveNearestReachableInteractionTile(

                state,

                worker,

                InteractionReservationKind.ResourceNode,

                99,

                tiles,

                out SpatialRules.TileCoord selected);



            AssertEqual(true, reserved, "scoring should reserve one of the candidate slots");

            AssertEqual(10, selected.X, "scoring should prefer less congested slot x");

            AssertEqual(12, selected.Y, "scoring should prefer less congested slot y");

        }

        private static void MultiWorkerResourceTrafficMakesProgress()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(2094, 1);

            ResourceNode node = CreateTestResourceNode(state, GatherProfileId.Tree, FixedVector2.FromInts(12, 10), GameData.StartingWoodAmount);

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(6, 10));

            int first = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(8, 8));

            int second = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(8, 9));

            int third = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(8, 10));



            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            int woodBefore = state.PlayerStates.Players[0].Resources.Wood;

            buffer.Add(new CommandEnvelope(

                new CommandHeader(0, 0, 0, CommandType.GatherResource),

                new GatherResourceCommand(node.Id, new[] { first, second, third })));



            int duplicateReservationTicks = 0;

            int movingForeverTicks = 0;

            int progressTicks = 0;

            for (int tick = 0; tick < 420; tick++)

            {

                if (tick > 0)

                {

                    AddNoOp(buffer, tick, 0, (uint)(20940 + tick));

                }



                runner.AdvanceOneTick(state, rules, buffer);

                AssertNoLiveUnitStacking(state, "multi-worker resource traffic should not stack");



                if (!TryReservationsAreDistinct(state, new[] { first, second, third }, InteractionReservationKind.ResourceNode, node.Id))

                {

                    duplicateReservationTicks++;

                }



                bool anyMovingToResource = false;

                bool anyGathering = false;

                for (int i = 0; i < state.EntityState.Units.Count; i++)

                {

                    Unit unit = state.EntityState.Units[i];

                    if (unit.CurrentResourceNodeId != node.Id)

                    {

                        continue;

                    }



                    anyMovingToResource = anyMovingToResource || unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot;

                    anyGathering = anyGathering || unit.TaskPhase == WorkerTaskPhase.Gathering;

                    AssertEqual(node.Id, unit.CurrentResourceNodeId, "resource traffic should preserve selected node target");

                }



                if (anyGathering || state.PlayerStates.Players[0].Resources.Wood > woodBefore)

                {

                    progressTicks++;

                }



                if (tick > 120 && anyMovingToResource && progressTicks == 0)

                {

                    movingForeverTicks++;

                }

            }



            AssertEqual(0, duplicateReservationTicks, "workers should not reserve duplicate resource slots during traffic");

            AssertEqual(0, movingForeverTicks, "workers should not stay MovingToResourceSlot forever while no gather/deposit progress happens");

            AssertEqual(true, state.PlayerStates.Players[0].Resources.Wood > woodBefore, "multi-worker resource traffic should eventually deposit wood");

        }

        private static void WorkerInResourceRangeGathersWithoutMoveRewrite()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2088, 1);

            ResourceNode node = FindResourceNodeById(state, 1);

            Unit unit = state.EntityState.Units[0];

            unit.Position = new FixedVector2(node.Position.X + Fixed.FromInt(1), node.Position.Y);

            unit.CurrentResourceNodeId = node.Id;

            unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;

            unit.HasMoveTarget = true;

            unit.MoveTarget = FixedVector2.FromInts(40, 40);

            FixedVector2 originalMoveTarget = unit.MoveTarget;



            new ResourceGatherSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));



            AssertEqual(WorkerTaskPhase.Gathering, unit.TaskPhase, "worker in resource range should enter gathering phase");

            AssertEqual(false, unit.HasMoveTarget, "worker in resource range should stop movement before gathering");

            AssertEqual(originalMoveTarget.X.Raw, unit.MoveTarget.X.Raw, "gather action should not rewrite move target raw x while already in range");

            AssertEqual(originalMoveTarget.Y.Raw, unit.MoveTarget.Y.Raw, "gather action should not rewrite move target raw y while already in range");

            AssertEqual(GameData.VillagerGatherPerTick, unit.CarriedAmount, "worker should gather immediately from valid interaction range");

        }

        private static void VillagerDoesNotGatherOutsideResourceRange()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(202, 1);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(0, state.EntityState.Units[0].CarriedAmount, "villager should not gather until in resource interaction range");

        }

        private static void VillagerGathersInResourceInteractionRange()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(203, 1);

            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(GameData.VillagerGatherPerTick, state.EntityState.Units[0].CarriedAmount, "villager should gather when in interaction range");

        }

        private static void VillagerGathersFromDiagonalResourceInteractionTile()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2076, 1);

            ResourceNode node = FindResourceNodeById(state, 1);

            state.EntityState.Units[0].Position = new FixedVector2(node.Position.X + Fixed.FromInt(1), node.Position.Y + Fixed.FromInt(1));

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(GameData.VillagerGatherPerTick, state.EntityState.Units[0].CarriedAmount, "diagonal resource interaction tile should gather");

        }

        private static void GroupGatherDoesNotCollapseOntoOneInteractionSlot()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateGroupGatherState(3024, 5, out int resourceId);

            int[] unitIds = new[] { 1, 2, 3, 4, 5 };

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, unitIds)));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertDistinctReservations(

                state,

                unitIds,

                InteractionReservationKind.ResourceNode,

                resourceId,

                "group gather resource slots");

        }

        private static void GroupGatherWorkersMakeProgressOrWaitCleanly()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateGroupGatherState(3025, 5, out int resourceId);

            int[] unitIds = new[] { 1, 2, 3, 4, 5 };

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, unitIds)));



            int woodBefore = state.PlayerStates.Players[0].Resources.Wood;

            var runner = new TickRunner();

            for (int i = 0; i < 500; i++)

            {

                runner.AdvanceOneTick(state, rules, buffer);

                AssertNoLiveUnitStacking(state, "group gather should not stack while moving/gathering/dropping off");

            }



            bool anyProgress = state.PlayerStates.Players[0].Resources.Wood > woodBefore;

            for (int i = 0; i < unitIds.Length && !anyProgress; i++)

            {

                Unit unit = FindUnitById(state, unitIds[i]);

                anyProgress = unit.CarriedAmount > 0 || unit.TaskPhase == WorkerTaskPhase.Gathering || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting;

            }



            AssertEqual(true, anyProgress, "group gather workers should gather/deposit or wait cleanly without losing intent");

        }

        private static void GroupGatherResolvesClickedNodeToResourceAreaDistribution()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateGroupBerryAreaState(3026, 5, out int areaId, out int[] nodeIds, out int[] unitIds);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(nodeIds[0], unitIds)));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            int distributedWorkers = 0;

            var targetedNodes = new HashSet<int>();

            for (int i = 0; i < unitIds.Length; i++)

            {

                Unit unit = FindUnitById(state, unitIds[i]);

                AssertEqual(areaId, unit.CurrentResourceAreaId, "clicked node should resolve to owning resource area for unit " + unit.Id);

                AssertEqual(true, unit.CurrentResourceNodeId != 0, "group gather should keep a concrete node target for unit " + unit.Id);

                targetedNodes.Add(unit.CurrentResourceNodeId);

                if (unit.HasMoveTarget || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting)

                {

                    distributedWorkers++;

                }

            }



            AssertEqual(true, distributedWorkers >= 3, "most workers should receive movement/waiting gather intent immediately");

            AssertEqual(true, targetedNodes.Count >= 2, "group gather should distribute workers across multiple nodes in the same area");

        }

        private static void GroupGatherAvoidsSingleNodeCollapseWhenSiblingNodesExist()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateGroupBerryAreaState(3027, 5, out _, out int[] nodeIds, out int[] unitIds);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(nodeIds[0], unitIds)));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            int nodeOneReservations = 0;

            int nodeTwoReservations = 0;

            int duplicateTileReservations = 0;

            var reservedTiles = new HashSet<long>();

            for (int i = 0; i < unitIds.Length; i++)

            {

                Unit unit = FindUnitById(state, unitIds[i]);

                if (unit.ReservedInteractionKind != InteractionReservationKind.ResourceNode)

                {

                    continue;

                }



                if (unit.ReservedInteractionTargetId == nodeIds[0])

                {

                    nodeOneReservations++;

                }

                else if (unit.ReservedInteractionTargetId == nodeIds[1])

                {

                    nodeTwoReservations++;

                }



                long tileKey = ((long)unit.ReservedInteractionTileX << 32) ^ (uint)unit.ReservedInteractionTileY;

                if (!reservedTiles.Add(tileKey))

                {

                    duplicateTileReservations++;

                }

            }



            AssertEqual(true, nodeOneReservations > 0, "clicked node should still receive some workers");

            AssertEqual(true, nodeTwoReservations > 0, "sibling node should receive workers under group pressure");

            AssertEqual(0, duplicateTileReservations, "workers should not reserve the same interaction slot tile");

        }

        private static void DryArabiaBerryGroupGatherMakesBoundedFoodProgress()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(2);

            GameState state = GameInitializer.CreateDryArabiaTest01(3028);

            int[] villagers = GetPlayerVillagerIds(state, 0);

            int[] workers = new[] { villagers[0], villagers[1], villagers[2], villagers[3] };

            int berryNodeId = FindNearbyResourceNodeId(state, DryArabiaTest01MapDefinition.GetTownCenterZone(0), ResourceType.Food);

            int foodBefore = state.PlayerStates.Players[0].Resources.Food;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(berryNodeId, workers)));

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));



            var runner = new TickRunner();

            for (int tick = 0; tick < 500; tick++)

            {

                runner.AdvanceOneTick(state, rules, buffer);

                AssertNoLiveUnitStacking(state, "dry arabia berry group gather should avoid stacking while making progress");

            }



            bool progressed = state.PlayerStates.Players[0].Resources.Food > foodBefore;

            for (int i = 0; i < workers.Length && !progressed; i++)

            {

                Unit worker = FindUnitById(state, workers[i]);

                progressed = worker.CarriedResourceType == ResourceType.Food

                    || worker.CarriedAmount > 0

                    || worker.TaskPhase == WorkerTaskPhase.Gathering

                    || worker.TaskPhase == WorkerTaskPhase.BlockedWaiting;

            }



            AssertEqual(true, progressed, "dry arabia berry group gather should make bounded food progress or maintain active gather intent");

        }

        private static void DryArabiaFourWorkerSameResourceAvoidsSingleWorkerStarvation()

        {

            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);

            GameState state = GameInitializer.CreateDryArabiaTest01(30281);

            var scenario = new SimScenarioHarness(state, rules, 2, 3028100);

            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);



            scenario.Step(

                new CommandEnvelope(new CommandHeader(state.Tick, 0, 3028101, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)),

                new CommandEnvelope(new CommandHeader(state.Tick, 1, 3028102, CommandType.NoOp), new NoOpCommand()));



            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);

            int[] builders = GetPlayerVillagerIds(state, 0);

            scenario.Step(

                new CommandEnvelope(new CommandHeader(state.Tick, 0, 3028103, CommandType.AssignBuild), new AssignBuildCommand(tcId, builders)),

                new CommandEnvelope(new CommandHeader(state.Tick, 1, 3028104, CommandType.NoOp), new NoOpCommand()));

            scenario.RunTicks(260, () => !state.EntityState.EntityLookup.ContainsKey(tcId) || !state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index].IsUnderConstruction);



            int[] workers = GetPlayerVillagerIds(state, 0);

            Array.Sort(workers);

            AssertEqual(true, workers.Length >= 4, scenario.Fail("expected at least four workers at start"));

            int[] selected = new[] { workers[0], workers[1], workers[2], workers[3] };

            int foodId = FindNearbyResourceNodeId(state, tcPos, ResourceType.Food);

            AssertEqual(true, foodId != 0, scenario.Fail("expected nearby food node"));



            scenario.Step(

                new CommandEnvelope(new CommandHeader(state.Tick, 0, 3028105, CommandType.GatherResource), new GatherResourceCommand(foodId, selected)),

                new CommandEnvelope(new CommandHeader(state.Tick, 1, 3028106, CommandType.NoOp), new NoOpCommand()));



            int startFood = state.PlayerStates.Players[0].Resources.Food;

            var productiveWorkers = new HashSet<int>();

            for (int i = 0; i < 1100; i++)

            {

                scenario.StepNoOps();

                scenario.CaptureWorkerTrace(selected, 120);

                scenario.AssertCoreInvariants("dry-arabia-four-worker-same-resource");

                AssertNoEndlessWorkerPhase(state, selected, WorkerTaskPhase.MovingToResourceSlot, 900, scenario.Fail("workers stuck moving-to-resource"));

                AssertNoEndlessWorkerPhase(state, selected, WorkerTaskPhase.MovingToDropoffSlot, 900, scenario.Fail("workers stuck moving-to-dropoff"));



                for (int u = 0; u < selected.Length; u++)

                {

                    Unit worker = FindUnitById(state, selected[u]);

                    if (worker.TaskPhase == WorkerTaskPhase.Gathering || worker.CarriedAmount > 0)

                    {

                        productiveWorkers.Add(worker.Id);

                    }

                }

            }



            int gainedFood = state.PlayerStates.Players[0].Resources.Food - startFood;

            AssertEqual(true, gainedFood >= 40, scenario.Fail("four-worker same-resource scenario should produce bounded food progress"));

            AssertEqual(true, productiveWorkers.Count >= 2, scenario.Fail("scenario should not collapse to a single productive worker"));

        }

    }
}
