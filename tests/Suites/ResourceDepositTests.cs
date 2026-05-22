using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Systems;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void VillagerReturnsToDropoffWhenFull()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(204, 1);

            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));

            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            for (int i = 0; i < 40 && state.EntityState.Units[0].CarriedAmount < GameData.VillagerCarryCapacity; i++)

            {

                if (i > 0)

                {

                    AddNoOp(buffer, state.Tick, 0, (uint)i);

                }



                runner.AdvanceOneTick(state, rules, buffer);

            }



            Unit unit = state.EntityState.Units[0];

            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];

            AssertEqual(true, unit.HasMoveTarget, "full villager should receive dropoff movement target");

            AssertEqual(true, SpatialRules.IsUnitInBuildingInteractionRange(new Unit { Position = unit.MoveTarget }, tc), "dropoff target should be on town center interaction ring");

        }

        private static void DropoffMoveTargetUsesTownCenterInteractionRing()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2065, 1);

            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));

            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            for (int i = 0; i < 40 && state.EntityState.Units[0].CarriedAmount < GameData.VillagerCarryCapacity; i++)

            {

                if (i > 0)

                {

                    AddNoOp(buffer, state.Tick, 0, (uint)i);

                }



                runner.AdvanceOneTick(state, rules, buffer);

            }



            Unit unit = state.EntityState.Units[0];

            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];

            int targetX = SpatialRules.GetTileX(unit.MoveTarget);

            int targetY = SpatialRules.GetTileY(unit.MoveTarget);

            AssertEqual(true, unit.HasMoveTarget, "dropoff assignment should set an approach tile");

            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(tc, targetX, targetY), "dropoff approach tile should not be inside TC footprint");

            AssertEqual(true, SpatialRules.IsUnitInBuildingInteractionRange(new Unit { Position = FixedVector2.FromInts(targetX, targetY) }, tc), "dropoff approach tile should be on TC interaction ring");

        }

        private static void MultipleFullFoodCarriersReserveDistinctDropoffSlots()

        {

            AssertMultipleFullCarriersReserveDistinctDropoffSlots(ResourceType.Food, 2083);

        }

        private static void MultipleFullWoodCarriersReserveDistinctDropoffSlots()

        {

            AssertMultipleFullCarriersReserveDistinctDropoffSlots(ResourceType.Wood, 2084);

        }

        private static void MultipleFullGoldCarriersReserveDistinctDropoffSlots()

        {

            AssertMultipleFullCarriersReserveDistinctDropoffSlots(ResourceType.Gold, 2085);

        }

        private static void MultipleFullCarriersDroppingAtSameTcDoNotStack()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateDropoffReservationState(2086, ResourceType.Gold, out _);

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            for (int tick = 0; tick <= 80; tick++)

            {

                AddNoOp(buffer, tick, 0, (uint)(20860 + tick));

                runner.AdvanceOneTick(state, rules, buffer);

                AssertNoLiveUnitStacking(state, "full carriers should not stack while dropping at same TC");

            }

        }

        private static void MultipleFullCarriersDroppingAtSameTcDepositCleanly()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateDropoffReservationState(2095, ResourceType.Wood, out int townCenterId);

            int[] unitIds = new[] { state.EntityState.Units[0].Id, state.EntityState.Units[1].Id, state.EntityState.Units[2].Id };

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            int woodBefore = state.PlayerStates.Players[0].Resources.Wood;

            int duplicateReservationTicks = 0;

            int churnTicks = 0;

            long[] previousReservationKeys = new long[unitIds.Length];

            long[] previousMoveKeys = new long[unitIds.Length];

            for (int i = 0; i < unitIds.Length; i++)

            {

                previousReservationKeys[i] = long.MinValue;

                previousMoveKeys[i] = long.MinValue;

            }



            for (int tick = 0; tick < 240; tick++)

            {

                AddNoOp(buffer, tick, 0, (uint)(20950 + tick));

                runner.AdvanceOneTick(state, rules, buffer);

                AssertNoLiveUnitStacking(state, "full carriers should not stack while depositing cleanly");



                if (!TryReservationsAreDistinct(state, unitIds, InteractionReservationKind.Dropoff, townCenterId))

                {

                    duplicateReservationTicks++;

                }



                for (int i = 0; i < unitIds.Length; i++)

                {

                    Unit unit = FindUnitById(state, unitIds[i]);

                    long reservationKey = EncodeReservationKey(unit);

                    long moveKey = EncodeMoveTargetKey(unit);

                    if (previousReservationKeys[i] != long.MinValue

                        && unit.CarriedAmount > 0

                        && (reservationKey != previousReservationKeys[i] || moveKey != previousMoveKeys[i]))

                    {

                        churnTicks++;

                    }



                    previousReservationKeys[i] = reservationKey;

                    previousMoveKeys[i] = moveKey;

                }

            }



            AssertEqual(0, duplicateReservationTicks, "full carriers should not duplicate final dropoff reservations");

            AssertEqual(true, churnTicks < 20, "dropoff reservations and move targets should not churn under normal 3-carrier traffic churn=" + churnTicks);

            AssertEqual(woodBefore + GameData.VillagerCarryCapacity * 3, state.PlayerStates.Players[0].Resources.Wood, "all full carriers should deposit cleanly at same TC");

            for (int i = 0; i < unitIds.Length; i++)

            {

                Unit unit = FindUnitById(state, unitIds[i]);

                AssertEqual(0, unit.CarriedAmount, "carrier should be empty after clean deposit unit=" + unit.Id);

            }

        }

        private static void StaleDropoffSlotTimeoutClearsAndReassigns()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3096, 1);

            int unitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 6));

            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 10));

            Unit worker = FindUnitById(state, unitId);

            worker.CarriedResourceType = ResourceType.Wood;

            worker.CarriedAmount = GameData.VillagerCarryCapacity;

            worker.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;

            worker.HasMoveTarget = true;

            worker.MoveTarget = FixedVector2.FromInts(20, 8);

            worker.LastMovedTick = 0;

            SpatialRules.ReserveInteractionSlot(state, worker, InteractionReservationKind.Dropoff, tcId, new SpatialRules.TileCoord(20, 8));

            state.Tick = GameData.NoProgressTimeoutTicks + 1;



            AddCompletedWall(state, 0, FixedVector2.FromInts(20, 8));



            new ResourceDepositSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));



            AssertEqual(true, worker.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot || worker.TaskPhase == WorkerTaskPhase.BlockedWaiting, "worker should keep dropoff intent after stale timeout");

            AssertEqual(false, worker.ReservedInteractionKind == InteractionReservationKind.MoveDestination, "dropoff stale recovery should not leave move-destination reservation");

            AssertEqual(false, worker.ReservedInteractionKind == InteractionReservationKind.Dropoff && worker.ReservedInteractionTileX == 20 && worker.ReservedInteractionTileY == 8, "stale blocked dropoff slot should be released or replaced");

        }

        private static void WorkerInDropoffRangeDepositsWithoutMoveRewrite()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = CreateOccupancyState(2089, 1);

            int unitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 10));

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));

            Unit unit = FindUnitById(state, unitId);

            unit.CarriedResourceType = ResourceType.Wood;

            unit.CarriedAmount = GameData.VillagerCarryCapacity;

            unit.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;

            unit.HasMoveTarget = true;

            unit.MoveTarget = FixedVector2.FromInts(35, 35);

            FixedVector2 originalMoveTarget = unit.MoveTarget;



            new ResourceDepositSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));



            AssertEqual(WorkerTaskPhase.Idle, unit.TaskPhase, "worker without resource target should become idle after depositing");

            AssertEqual(false, unit.HasMoveTarget, "worker in dropoff range should stop movement before depositing");

            AssertEqual(originalMoveTarget.X.Raw, unit.MoveTarget.X.Raw, "deposit action should not rewrite move target raw x while already in range");

            AssertEqual(originalMoveTarget.Y.Raw, unit.MoveTarget.Y.Raw, "deposit action should not rewrite move target raw y while already in range");

            AssertEqual(0, unit.CarriedAmount, "worker should deposit carried resources");

            AssertEqual(GameData.VillagerCarryCapacity, state.PlayerStates.Players[0].Resources.Wood, "deposit should update stockpile");

        }

        private static void VillagerDepositsFromDiagonalTownCenterInteractionTile()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = CreateOccupancyState(2077, 1);

            int unitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 12));

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));

            Unit unit = state.EntityState.Units[state.EntityState.EntityLookup[unitId].Index];

            unit.CarriedResourceType = ResourceType.Gold;

            unit.CarriedAmount = GameData.VillagerCarryCapacity;



            var buffer = new CommandBuffer();

            AddNoOp(buffer, 0, 0, 2077);

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(0, unit.CarriedAmount, "diagonal TC interaction tile should deposit");

            AssertEqual(GameData.VillagerCarryCapacity, state.PlayerStates.Players[0].Resources.Gold, "diagonal TC deposit should update stockpile");

        }

        private static void VillagerResumesResourceLoopAfterDeposit()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(205, 1);

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));

            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);

            state.EntityState.Units[1].Position = FixedVector2.FromInts(20, 20);

            state.EntityState.Units[2].Position = FixedVector2.FromInts(21, 20);

            state.EntityState.Units[3].Position = FixedVector2.FromInts(22, 20);

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            for (int tick = 0; tick < 120; tick++)

            {

                if (tick > 0)

                {

                    AddNoOp(buffer, tick, 0, (uint)tick);

                }



                runner.AdvanceOneTick(state, rules, buffer);

            }



            Unit unit = state.EntityState.Units[0];

            AssertEqual(1, unit.CurrentResourceNodeId, "villager should keep same resource assignment after deposit");

            AssertEqual(true, unit.HasMoveTarget || unit.CarriedAmount > 0, "villager should continue looping between resource and dropoff");

        }

        private static void VillagerReturnsToSameFoodTargetAfterDeposit()

        {

            AssertVillagerReturnsToSameTargetAfterDeposit(ResourceType.Food, 2067);

        }

        private static void VillagerReturnsToSameWoodTargetAfterDeposit()

        {

            AssertVillagerReturnsToSameTargetAfterDeposit(ResourceType.Wood, 2068);

        }

        private static void VillagerReturnsToSameGoldTargetAfterDeposit()

        {

            AssertVillagerReturnsToSameTargetAfterDeposit(ResourceType.Gold, 2069);

        }

        private static void VillagerKeepsExplicitlyAssignedGoldNodeAcrossDepositLoop()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(20701, 1);

            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(25, 10));

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));

            int areaId = AddTestResourceArea(state, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(18, 10));

            int nearGoldNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(12, 10), GameData.StartingGoldAmount);

            int assignedGoldNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(24, 10), GameData.StartingGoldAmount);



            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(assignedGoldNodeId, new[] { workerId })));

            runner.AdvanceOneTick(state, rules, buffer);



            for (int tick = 1; tick <= 220; tick++)

            {

                AddNoOp(buffer, tick, 0, (uint)(9700 + tick));

                runner.AdvanceOneTick(state, rules, buffer);

            }



            Unit worker = FindUnitById(state, workerId);

            ResourceNode nearGold = FindResourceNodeById(state, nearGoldNodeId);

            AssertEqual(assignedGoldNodeId, worker.CurrentResourceNodeId, "worker should return to explicitly assigned gold node after dropoff");

            AssertEqual(GameData.StartingGoldAmount, nearGold.RemainingAmount, "worker should not switch to nearer sibling gold node while assigned node remains valid");

        }

        private static void ExplicitGatherRetargetSwitchesAssignedNode()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(20702, 1);

            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(25, 10));

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));

            int areaId = AddTestResourceArea(state, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(18, 10));

            int nearGoldNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(12, 10), GameData.StartingGoldAmount);

            int farGoldNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(24, 10), GameData.StartingGoldAmount);



            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(farGoldNodeId, new[] { workerId })));

            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 1; tick <= 80; tick++)

            {

                AddNoOp(buffer, tick, 0, (uint)(9800 + tick));

                runner.AdvanceOneTick(state, rules, buffer);

            }



            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 900, CommandType.GatherResource), new GatherResourceCommand(nearGoldNodeId, new[] { workerId })));

            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 0; tick < 100; tick++)

            {

                AddNoOp(buffer, state.Tick, 0, (uint)(9900 + tick));

                runner.AdvanceOneTick(state, rules, buffer);

            }



            Unit worker = FindUnitById(state, workerId);

            AssertEqual(nearGoldNodeId, worker.CurrentResourceNodeId, "explicit gather retarget should switch worker to the newly clicked node");

        }

        private static void GatherKeepsAssignedResourceWhenNearerSameTypeExists()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2070, 1);

            int assignedResourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);

            ResourceNode assigned = FindResourceNodeById(state, assignedResourceId);

            state.EconomyState.ResourceNodes.Add(new ResourceNode

            {

                Id = state.EconomyState.NextResourceNodeId++,

                ResourceType = ResourceType.Food,

                Position = state.EntityState.Units[0].Position,

                RemainingAmount = GameData.StartingFoodAmount

            });



            int nearResourceId = state.EconomyState.ResourceNodes[state.EconomyState.ResourceNodes.Count - 1].Id;

            AssertEqual(true, nearResourceId != assignedResourceId, "setup should create distinct near food resource");

            state.EntityState.Units[0].Position = new FixedVector2(assigned.Position.X + Fixed.FromInt(2), assigned.Position.Y);

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(assignedResourceId, new[] { 1 })));

            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 1; tick <= 100; tick++)

            {

                AddNoOp(buffer, tick, 0, (uint)(9000 + tick));

                runner.AdvanceOneTick(state, rules, buffer);

            }



            AssertEqual(assignedResourceId, state.EntityState.Units[0].CurrentResourceNodeId, "worker should keep originally assigned resource id while it remains valid");

        }

        private static void GatherMoveTargetRemainsStableWhileApproaching()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2071, 1);

            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1 })));

            runner.AdvanceOneTick(state, rules, buffer);



            Unit unit = state.EntityState.Units[0];

            if (!unit.HasMoveTarget)

            {

                AssertEqual(

                    true,

                    unit.TaskPhase == WorkerTaskPhase.Gathering

                        || unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode,

                    "worker without move target should already be gathering or hold a valid resource reservation");

                return;

            }



            int targetX = SpatialRules.GetTileX(unit.MoveTarget);

            int targetY = SpatialRules.GetTileY(unit.MoveTarget);

            for (int tick = 1; tick <= 2; tick++)

            {

                AddNoOp(buffer, tick, 0, (uint)(9500 + tick));

                runner.AdvanceOneTick(state, rules, buffer);

                if (!unit.HasMoveTarget)

                {

                    AssertEqual(

                        true,

                        unit.TaskPhase == WorkerTaskPhase.Gathering

                            || unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode,

                        "worker without move target should have transitioned to gathering/reserved state");

                    return;

                }

                AssertEqual(targetX, SpatialRules.GetTileX(unit.MoveTarget), "approach tile x should remain stable while valid");

                AssertEqual(targetY, SpatialRules.GetTileY(unit.MoveTarget), "approach tile y should remain stable while valid");

            }

        }

        private static void WorkerGatherLoopDiagnosticsStayStable()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(2092, 1);

            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Wood);

            ResourceNode wood = FindResourceNodeById(state, resourceId);

            state.EntityState.Units[0].Position = new FixedVector2(wood.Position.X + Fixed.FromInt(1), wood.Position.Y);

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));



            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1 })));



            WorkerDiagnosticSample previous = default;

            bool hasPrevious = false;

            int phaseChanges = 0;

            int reservationChanges = 0;

            int moveTargetChanges = 0;

            int backwardsMoves = 0;

            int deposits = 0;

            int stockpileBefore = state.PlayerStates.Players[0].Resources.Wood;

            for (int tick = 0; tick < 1200; tick++)

            {

                if (tick > 0)

                {

                    AddNoOp(buffer, tick, 0, (uint)(20920 + tick));

                }



                runner.AdvanceOneTick(state, rules, buffer);

                Unit unit = state.EntityState.Units[0];

                WorkerDiagnosticSample current = WorkerDiagnosticSample.Capture(unit);

                if (state.PlayerStates.Players[0].Resources.Wood > stockpileBefore)

                {

                    deposits++;

                    stockpileBefore = state.PlayerStates.Players[0].Resources.Wood;

                }



                if (hasPrevious)

                {

                    if (current.Phase != previous.Phase)

                    {

                        phaseChanges++;

                    }



                    if (current.ReservationKey != previous.ReservationKey)

                    {

                        reservationChanges++;

                    }



                    if (current.MoveTargetKey != previous.MoveTargetKey)

                    {

                        moveTargetChanges++;

                    }



                    if (current.PositionXRaw < previous.PositionXRaw

                        && current.Phase == previous.Phase

                        && current.MoveTargetKey == previous.MoveTargetKey

                        && current.ReservationKey == previous.ReservationKey)

                    {

                        backwardsMoves++;

                    }

                }



                hasPrevious = true;

                previous = current;

                AssertEqual(resourceId, unit.CurrentResourceNodeId, "worker should keep exact wood target through diagnostic loop");

                AssertEqual(true, unit.TaskPhase != WorkerTaskPhase.Idle || unit.CarriedAmount == 0, "worker should not idle while still carrying resources");

            }



            AssertEqual(true, deposits >= 3, "diagnostic loop should include multiple deposits");

            AssertEqual(true, phaseChanges < 400, "task phase should not flip every tick during stable gather/deposit loop changes=" + phaseChanges);

            AssertEqual(true, reservationChanges < 400, "reservation should not churn every tick during stable gather/deposit loop changes=" + reservationChanges);

            AssertEqual(true, moveTargetChanges < 400, "move target should not be rewritten every tick during stable gather/deposit loop changes=" + moveTargetChanges);

            AssertEqual(0, backwardsMoves, "worker raw x should not oscillate backwards while pursuing the same stable target");

        }

        private static void FullGoldCarrierBlockedDropoffTargetRetargetsAndDeposits()

        {

            AssertBlockedCarrierRetargetsAndDeposits(ResourceType.Gold, 2072);

        }

        private static void FullFoodCarrierBlockedDropoffTargetRetargetsAndDeposits()

        {

            AssertBlockedCarrierRetargetsAndDeposits(ResourceType.Food, 2073);

        }

        private static void FullWoodCarrierBlockedDropoffTargetRetargetsAndDeposits()

        {

            AssertBlockedCarrierRetargetsAndDeposits(ResourceType.Wood, 2074);

        }

    }
}
