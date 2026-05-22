using RtsGame.Net.Lockstep;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Systems;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void GroupMoveAssignsDistinctDestinationSlots()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3020);

            int[] unitIds = CreateLineOfUnits(state, 5, 0, 0, 0, 0, 1);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(10, 10))));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertDistinctReservations(

                state,

                unitIds,

                InteractionReservationKind.MoveDestination,

                SpatialRules.EncodeTileKey(10, 10),

                "group move destination slots");

        }

        private static void TenUnitGroupMoveDoesNotStack()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3021);

            int[] unitIds = CreateLineOfUnits(state, 10, 0, 0, 0, 0, 1);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(12, 12))));



            var runner = new TickRunner();

            for (int i = 0; i < 80; i++)

            {

                runner.AdvanceOneTick(state, rules, buffer);

                AssertNoLiveUnitStacking(state, "ten unit group move should not stack");

            }

        }

        private static void TwentyUnitGroupMoveSettlesOrWaitsWithoutStacking()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState first = CreateOccupancyState(3041);

            GameState second = CreateOccupancyState(3041);

            int[] firstUnitIds = CreateLineOfUnits(first, 20, 0, 0, 4, 0, 1);

            int[] secondUnitIds = CreateLineOfUnits(second, 20, 0, 0, 4, 0, 1);

            var firstBuffer = new CommandBuffer();

            var secondBuffer = new CommandBuffer();

            firstBuffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(firstUnitIds, FixedVector2.FromInts(24, 12))));

            secondBuffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(secondUnitIds, FixedVector2.FromInts(24, 12))));



            var runner = new TickRunner();

            for (int tick = 0; tick < 90; tick++)

            {

                runner.AdvanceOneTick(first, rules, firstBuffer);

                AssertNoLiveUnitStacking(first, "twenty unit group move should not stack under pressure");

                AssertNoDuplicateFinalPurposeReservations(first, "twenty unit group move should not duplicate final destination reservations");

            }



            runner = new TickRunner();

            for (int tick = 0; tick < 90; tick++)

            {

                runner.AdvanceOneTick(second, rules, secondBuffer);

            }



            AssertEqual(first.LastChecksum, second.LastChecksum, "twenty unit group move should remain deterministic");

            int activeOrArrived = 0;

            for (int i = 0; i < firstUnitIds.Length; i++)

            {

                Unit unit = FindUnitById(first, firstUnitIds[i]);

                if (!unit.HasMoveTarget || unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)

                {

                    activeOrArrived++;

                }

            }



            AssertEqual(firstUnitIds.Length, activeOrArrived, "twenty unit group move units should arrive or wait with stable destination slots");

        }

        private static void GroupMoveAvoidsReservedFinalDestinationSlots()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3022);

            int reserverId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(20, 20), false);

            SpatialRules.ReserveInteractionSlot(

                state,

                FindUnitById(state, reserverId),

                InteractionReservationKind.MoveDestination,

                SpatialRules.EncodeTileKey(10, 10),

                new SpatialRules.TileCoord(10, 10));



            int[] unitIds = CreateLineOfUnits(state, 3, 1, 0, 0, 0, 1);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(10, 10))));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            for (int i = 0; i < unitIds.Length; i++)

            {

                Unit unit = FindUnitById(state, unitIds[i]);

                AssertEqual(false, unit.ReservedInteractionTileX == 10 && unit.ReservedInteractionTileY == 10, "group move should avoid another unit's reserved final destination");

            }

        }

        private static void GroupMoveDoesNotCauseEndlessJitter()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3023);

            int[] unitIds = CreateLineOfUnits(state, 6, 0, 0, 0, 0, 1);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(8, 8))));



            var runner = new TickRunner();

            for (int i = 0; i < 180; i++)

            {

                runner.AdvanceOneTick(state, rules, buffer);

                AssertNoLiveUnitStacking(state, "group move should not stack while resolving traffic");

            }



            int arrivedOrWaiting = 0;

            for (int i = 0; i < unitIds.Length; i++)

            {

                Unit unit = FindUnitById(state, unitIds[i]);

                if (!unit.HasMoveTarget || unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)

                {

                    arrivedOrWaiting++;

                }

            }



            AssertEqual(unitIds.Length, arrivedOrWaiting, "group move units should either arrive or keep stable destination reservations");

        }

        private static void DryArabiaTwoVillagerGroundMoveNearTcMakesProgress()

        {

            SimScenarioHarness scenario = CreateDryArabiaMoveReliabilityHarness(33001, 4, out GameState state, out int[] workers, out FixedVector2 tcPos, out _);

            int[] selected = TakeSortedUnits(workers, 2);

            IssueGroupMove(scenario, 0, selected, FixedVector2.FromInts(tcPos.X.FloorToInt() + 4, tcPos.Y.FloorToInt() - 3), 3300100);



            DriveGroupMoveReliabilityTicks(scenario, selected, 220, selected.Length, "dry-arabia-two-villager-ground-move");

        }

        private static void DryArabiaFourVillagerGroundMoveAroundTcMakesProgress()

        {

            SimScenarioHarness scenario = CreateDryArabiaMoveReliabilityHarness(33002, 4, out GameState state, out int[] workers, out FixedVector2 tcPos, out _);

            int[] selected = TakeSortedUnits(workers, 4);

            IssueGroupMove(scenario, 0, selected, FixedVector2.FromInts(tcPos.X.FloorToInt() - 5, tcPos.Y.FloorToInt() + 4), 3300200);



            DriveGroupMoveReliabilityTicks(scenario, selected, 320, selected.Length, "dry-arabia-four-villager-around-tc");

        }

        private static void DryArabiaFiveVillagerGroupMoveNearResourcesStaysBounded()

        {

            SimScenarioHarness scenario = CreateDryArabiaMoveReliabilityHarness(33003, 5, out GameState state, out int[] workers, out FixedVector2 tcPos, out _);

            int[] selected = TakeSortedUnits(workers, 5);

            IssueGroupMove(scenario, 0, selected, FixedVector2.FromInts(tcPos.X.FloorToInt() + 6, tcPos.Y.FloorToInt() + 5), 3300300);



            DriveGroupMoveReliabilityTicks(scenario, selected, 360, 4, "dry-arabia-five-villager-resource-side-move");

        }

        private static void DryArabiaRepeatedGroupMoveReplacementClearsStaleDestinations()

        {

            SimScenarioHarness scenario = CreateDryArabiaMoveReliabilityHarness(33004, 5, out GameState state, out int[] workers, out FixedVector2 tcPos, out _);

            int[] selected = TakeSortedUnits(workers, 5);

            FixedVector2[] targets =

            {

                FixedVector2.FromInts(tcPos.X.FloorToInt() + 5, tcPos.Y.FloorToInt() - 2),

                FixedVector2.FromInts(tcPos.X.FloorToInt() - 5, tcPos.Y.FloorToInt() + 3),

                FixedVector2.FromInts(tcPos.X.FloorToInt() + 4, tcPos.Y.FloorToInt() + 5),

                FixedVector2.FromInts(tcPos.X.FloorToInt() - 4, tcPos.Y.FloorToInt() - 4),

            };



            for (int i = 0; i < targets.Length; i++)

            {

                IssueGroupMove(scenario, 0, selected, targets[i], unchecked((uint)(3300400 + i)));

                DriveGroupMoveReliabilityTicks(scenario, selected, 36, 3, "dry-arabia-repeated-group-move-" + i);

            }



            DriveGroupMoveReliabilityTicks(scenario, selected, 240, 0, "dry-arabia-repeated-group-move-final");

        }

        private static void DryArabiaGroupMoveFollowedByGatherClearsMoveDestinations()

        {

            SimScenarioHarness scenario = CreateDryArabiaMoveReliabilityHarness(33005, 5, out GameState state, out int[] workers, out FixedVector2 tcPos, out int foodId);

            int[] selected = TakeSortedUnits(workers, 5);

            IssueGroupMove(scenario, 0, selected, FixedVector2.FromInts(tcPos.X.FloorToInt() + 5, tcPos.Y.FloorToInt() + 4), 3300500);

            DriveGroupMoveReliabilityTicks(scenario, selected, 40, 3, "dry-arabia-move-before-gather");



            scenario.Step(

                new CommandEnvelope(new CommandHeader(state.Tick, 0, 3300501, CommandType.GatherResource), new GatherResourceCommand(foodId, selected)),

                new CommandEnvelope(new CommandHeader(state.Tick, 1, 3300502, CommandType.NoOp), new NoOpCommand()));



            for (int i = 0; i < selected.Length; i++)

            {

                Unit unit = FindUnitById(state, selected[i]);

                AssertEqual(false, unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination, scenario.Fail("gather should clear stale move destination reservation"));

                AssertEqual(true, unit.CurrentResourceNodeId != 0 || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting, scenario.Fail("gather should establish resource intent"));

            }



            DriveFiveWorkerScenarioTicks(scenario, selected, 220, 900, "dry-arabia-move-followed-by-gather");

        }

        private static void DryArabiaGatherFollowedByGroupMoveClearsResourceReservations()

        {

            SimScenarioHarness scenario = CreateDryArabiaMoveReliabilityHarness(33006, 5, out GameState state, out int[] workers, out FixedVector2 tcPos, out int foodId);

            int[] selected = TakeSortedUnits(workers, 5);

            scenario.Step(

                new CommandEnvelope(new CommandHeader(state.Tick, 0, 3300600, CommandType.GatherResource), new GatherResourceCommand(foodId, selected)),

                new CommandEnvelope(new CommandHeader(state.Tick, 1, 3300601, CommandType.NoOp), new NoOpCommand()));

            DriveFiveWorkerScenarioTicks(scenario, selected, 40, 900, "dry-arabia-gather-before-move");



            IssueGroupMove(scenario, 0, selected, FixedVector2.FromInts(tcPos.X.FloorToInt() - 5, tcPos.Y.FloorToInt() + 5), 3300602);

            for (int i = 0; i < selected.Length; i++)

            {

                Unit unit = FindUnitById(state, selected[i]);

                AssertEqual(false, unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode, scenario.Fail("move should clear stale resource reservation"));

                AssertEqual(0, unit.CurrentResourceNodeId, scenario.Fail("move should clear current resource node"));

                AssertEqual(WorkerTaskPhase.MovingToCommandMove, unit.TaskPhase, scenario.Fail("move should establish command-move intent"));

            }



            DriveGroupMoveReliabilityTicks(scenario, selected, 260, 4, "dry-arabia-gather-followed-by-move");

        }

        private static void WorkerTaskTileSwapKeepsMovementIntent()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(2096, 1);

            int firstId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            int secondId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(1, 0));

            Unit first = FindUnitById(state, firstId);

            Unit second = FindUnitById(state, secondId);

            first.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;

            first.HasMoveTarget = true;

            first.MoveTarget = FixedVector2.FromInts(1, 0);

            first.CurrentResourceNodeId = 10;

            first.LastMovedTick = state.Tick;

            second.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;

            second.HasMoveTarget = true;

            second.MoveTarget = FixedVector2.FromInts(0, 0);

            second.CarriedResourceType = ResourceType.Wood;

            second.CarriedAmount = GameData.VillagerCarryCapacity;

            second.LastMovedTick = state.Tick;



            new MovementSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));



            AssertEqual(0, SpatialRules.GetTileX(first.Position), "first worker should not swap into occupied tile");

            AssertEqual(1, SpatialRules.GetTileX(second.Position), "second worker should not swap into occupied tile");

            AssertNoLiveUnitStacking(state, "worker task swap conflict should not stack units");

            AssertEqual(true, first.HasMoveTarget, "worker task swap conflict should keep first movement target");

            AssertEqual(true, second.HasMoveTarget, "worker task swap conflict should keep second movement target");

            AssertEqual(10, first.CurrentResourceNodeId, "temporary swap congestion should not clear resource intent");

            AssertEqual(GameData.VillagerCarryCapacity, second.CarriedAmount, "temporary swap congestion should not clear carried resources");

            AssertEqual(WorkerTaskPhase.MovingToResourceSlot, first.TaskPhase, "brief swap congestion should not reset first worker phase");

            AssertEqual(WorkerTaskPhase.MovingToDropoffSlot, second.TaskPhase, "brief swap congestion should not reset second worker phase");

        }

        private static void UnitDeathSameTickStillBlocksMovement()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(2);

            GameState state = CreateOccupancyState(5, 2);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));

            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(1, 1));

            state.EntityState.Units[1].HitPoints = GameData.InfantryAttackDamage;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.Attack), new AttackCommand(new[] { 3 }, 2)));

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(Fixed.FromInt(0).Raw, state.EntityState.Units[0].Position.X.Raw, "unit dying later in same tick should still block movement");

        }

        private static void WallDestructionSameTickStillBlocksMovement()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(2);

            GameState state = CreateOccupancyState(6, 2);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));

            int wallId = EntityFactory.CreateWall(state, 1, FixedVector2.FromInts(2, 0));

            Building wall = state.EntityState.Buildings[state.EntityState.EntityLookup[wallId].Index];

            wall.IsUnderConstruction = false;

            wall.HitPoints = GameData.SiegeCannonBuildingDamage;

            int siegeId = EntityFactory.CreateUnit(state, 0, UnitTypeId.SiegeCannon, FixedVector2.FromInts(0, 2));

            Unit siege = state.EntityState.Units[state.EntityState.EntityLookup[siegeId].Index];

            siege.IsSiegeDeployed = true;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))));

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.Attack), new AttackCommand(new[] { 3 }, wallId)));

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(Fixed.FromInt(1).Raw, state.EntityState.Units[0].Position.X.Raw, "wall destroyed later in same tick should still block movement step");

        }

    }
}
