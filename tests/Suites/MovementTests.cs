using RtsGame.Net.Lockstep;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void MoveUnitAdvancesDeterministically()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3001, 1);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));



            runner.AdvanceOneTick(state, rules, buffer);



            AssertEqual(Fixed.FromRatio(1, 2).Raw, state.EntityState.Units[0].Position.X.Raw, "villager should move half a tile on first tick");

            AssertEqual(0L, state.EntityState.Units[0].Position.Y.Raw, "villager should stay on same Y axis");

            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "unit should continue moving toward target");

        }

        private static void MoveUnitSnapsToTarget()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = CreateOccupancyState(1);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));



            runner.AdvanceOneTick(state, rules, buffer);

            AddNoOp(buffer, 1, 0, 1);

            runner.AdvanceOneTick(state, rules, buffer);



            AssertEqual(Fixed.FromInt(1).Raw, state.EntityState.Units[0].Position.X.Raw, "unit should snap to target");

            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "unit should clear move target after arrival");

        }

        private static void MoveCommandClearsWorkAssignments()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(1, 1);

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));

            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Units[0].CurrentResourceNodeId, "unit should have gather assignment");



            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(5, 5))));

            runner.AdvanceOneTick(state, rules, buffer);



            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "move should clear gather assignment");

        }

        private static void MoveArrivalClearsDestinationReservation()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3091);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));

            runner.AdvanceOneTick(state, rules, buffer);



            Unit mover = state.EntityState.Units[0];

            AssertEqual(InteractionReservationKind.MoveDestination, mover.ReservedInteractionKind, "move command should reserve destination while moving");

            AddNoOp(buffer, 1, 0, 1);

            runner.AdvanceOneTick(state, rules, buffer);



            AssertEqual(false, mover.HasMoveTarget, "arrived move should clear move target");

            AssertEqual(InteractionReservationKind.None, mover.ReservedInteractionKind, "arrived move should clear destination reservation");

        }

        private static void MoveReplacementClearsDestinationReservation()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3092, 1);

            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { workerId }, FixedVector2.FromInts(6, 0))));

            runner.AdvanceOneTick(state, rules, buffer);

            Unit worker = FindUnitById(state, workerId);

            SpatialRules.ReserveInteractionSlot(

                state,

                worker,

                InteractionReservationKind.MoveDestination,

                SpatialRules.EncodeTileKey(6, 0),

                new SpatialRules.TileCoord(6, 0));



            int foodNodeId = state.EconomyState.NextResourceNodeId++;

            state.EconomyState.ResourceNodes.Add(new ResourceNode

            {

                Id = foodNodeId,

                ResourceType = ResourceType.Food,

                Position = FixedVector2.FromInts(4, 0),

                RemainingAmount = GameData.StartingFoodAmount

            });

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.GatherResource), new GatherResourceCommand(foodNodeId, new[] { workerId })));

            runner.AdvanceOneTick(state, rules, buffer);



            AssertEqual(foodNodeId, worker.CurrentResourceNodeId, "gather replacement should assign resource target");

            AssertEqual(false, worker.ReservedInteractionKind == InteractionReservationKind.MoveDestination, "gather replacement should clear stale move destination reservation");

        }

        private static void UnitDeathClearsDestinationReservation()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3093);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))));

            runner.AdvanceOneTick(state, rules, buffer);

            Unit unit = state.EntityState.Units[0];

            AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, "unit should have move destination reservation before death");



            unit.HitPoints = 0;

            AddNoOp(buffer, 1, 0, 1);

            runner.AdvanceOneTick(state, rules, buffer);



            AssertEqual(true, unit.IsDead, "unit should be marked dead");

            AssertEqual(InteractionReservationKind.None, unit.ReservedInteractionKind, "death should clear move destination reservation");

            AssertEqual(false, unit.HasMoveTarget, "death should clear move target");

        }

        private static void ResignClearsDestinationReservation()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3094, 1);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(6, 0))));

            runner.AdvanceOneTick(state, rules, buffer);

            Unit unit = state.EntityState.Units[0];

            AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, "unit should have move destination reservation before resign");



            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.Resign), new ResignCommand()));

            runner.AdvanceOneTick(state, rules, buffer);



            AssertEqual(true, state.PlayerStates.Players[0].IsResigned, "player should be resigned");

            AssertEqual(InteractionReservationKind.None, unit.ReservedInteractionKind, "resign cleanup should clear move destination reservation");

            AssertEqual(false, unit.HasMoveTarget, "resign cleanup should clear active move target");

        }

        private static void MoveRejectsWallBlockedTarget()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(15, 1);

            EntityFactory.CreateWall(state, 0, FixedVector2.FromInts(2, 0));

            state.EntityState.Buildings[0].IsUnderConstruction = false;

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "move target inside wall should reject");

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "blocked move target should count as rejected");

        }

        private static void MoveRejectReasonForWallBlockedTarget()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(1599, 1);

            EntityFactory.CreateWall(state, 0, FixedVector2.FromInts(2, 0));

            state.EntityState.Buildings[0].IsUnderConstruction = false;

            var header = new CommandHeader(state.Tick, 0, 0, CommandType.MoveUnits);

            var command = new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0));



            CommandValidationReport report = CommandValidationInspector.Evaluate(

                state,

                rules,

                new CommandEnvelope(header, command));



            AssertEqual(false, report.Accepted, "move to blocked wall tile should reject");

            AssertEqual(CommandValidationReason.TargetBlockedByStaticGeometry, report.Reason, "blocked tile should report static geometry reason");

            AssertEqual(2, report.TargetTileX, "report should include target tile x");

            AssertEqual(0, report.TargetTileY, "report should include target tile y");

        }

        private static void MoveRejectsResourceBlockedTarget()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = GameInitializer.CreateNomadStart(151, 1);

            ResourceNode node = FindResourceNodeById(state, 1);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, node.Position)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "move target inside resource footprint should reject");

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "resource-blocked move target should count as rejected");

        }

        private static void MoveRejectsUnreachableOpenTarget()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(152);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            for (int y = 0; y < state.MapState.HeightTiles; y++)

            {

                AddCompletedWall(state, 0, FixedVector2.FromInts(1, y));

            }



            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(3, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "move target behind sealed blocker should reject");

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "unreachable move target should count as rejected");

        }

        private static void MoveAcceptsMultiSelectWhenAtLeastOneUnitCanPath()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(1521);

            int trappedUnitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 10));

            int mobileUnitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            int trapAreaId = AddTestResourceArea(state, GatherProfileId.Tree, FixedVector2.FromInts(10, 10));

            AddTestResourceNodeToArea(state, trapAreaId, GatherProfileId.Tree, FixedVector2.FromInts(9, 10), GameData.StartingWoodAmount);

            AddTestResourceNodeToArea(state, trapAreaId, GatherProfileId.Tree, FixedVector2.FromInts(11, 10), GameData.StartingWoodAmount);

            AddTestResourceNodeToArea(state, trapAreaId, GatherProfileId.Tree, FixedVector2.FromInts(10, 9), GameData.StartingWoodAmount);

            AddTestResourceNodeToArea(state, trapAreaId, GatherProfileId.Tree, FixedVector2.FromInts(10, 11), GameData.StartingWoodAmount);



            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(

                new CommandHeader(0, 0, 0, CommandType.MoveUnits),

                new MoveUnitsCommand(new[] { trappedUnitId, mobileUnitId }, FixedVector2.FromInts(20, 20))));

            runner.AdvanceOneTick(state, rules, buffer);



            Unit trapped = FindUnitById(state, trappedUnitId);

            Unit mobile = FindUnitById(state, mobileUnitId);

            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "group move should be accepted when at least one selected unit can path");

            AssertEqual(false, trapped.HasMoveTarget, "trapped unit without static path should not receive unreachable move target");

            AssertEqual(true, mobile.HasMoveTarget, "reachable unit should still receive move target");

            AssertEqual(WorkerTaskPhase.MovingToCommandMove, mobile.TaskPhase, "reachable unit should keep command move intent");

        }

        private static void MovementPathfindsAroundWall()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var state = CreateOccupancyState(16);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            EntityFactory.CreateWall(state, 0, FixedVector2.FromInts(2, 0));

            state.EntityState.Buildings[0].IsUnderConstruction = false;

            var buffer = new CommandBuffer();

            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))));

            for (int tick = 0; tick < 8; tick++)

            {

                if (tick > 0)

                {

                    AddNoOp(buffer, tick, 0, (uint)tick);

                }



                runner.AdvanceOneTick(state, rules, buffer);

            }



            AssertEqual(true, state.EntityState.Units[0].Position.X.Raw > Fixed.FromInt(1).Raw, "unit should progress past the wall using a side route");

            AssertEqual(false, SpatialRules.IsBlockedByWall(state, state.EntityState.Units[0].Position), "unit should not occupy a wall-blocked position");

            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "unit should continue pathing toward target");

        }

        private static void PathfinderReturnsSameFirstStep()

        {

            GameState first = CreatePathfindingWallState();

            GameState second = CreatePathfindingWallState();



            bool firstFound = DeterministicPathfinder.TryFindNextTile(first, 0, 0, 4, 0, out int firstX, out int firstY);

            bool secondFound = DeterministicPathfinder.TryFindNextTile(second, 0, 0, 4, 0, out int secondX, out int secondY);



            AssertEqual(true, firstFound, "pathfinder should find route around single wall");

            AssertEqual(firstX, secondX, "same path request should return same X step");

            AssertEqual(firstY, secondY, "same path request should return same Y step");

            AssertEqual(1, firstX, "first step should follow frozen east-first neighbor order");

            AssertEqual(0, firstY, "first step should stay on row before rerouting");

        }

        private static void PathfinderWallBlocksPath()

        {

            GameState state = CreateOccupancyState(17);

            AddCompletedWall(state, 0, FixedVector2.FromInts(2, 0));



            bool found = DeterministicPathfinder.TryFindNextTile(state, 0, 0, 2, 0, out int nextX, out int nextY);



            AssertEqual(false, found, "sealed wall barrier should block path");

            AssertEqual(0, nextX, "failed path should keep default X");

            AssertEqual(0, nextY, "failed path should keep default Y");

        }

        private static void PathfinderBlocksBuildingAndResourceTiles()

        {

            GameState state = GameInitializer.CreateDryArabiaTest01(171);

            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);

            FixedVector2 tcZone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcZone)));

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);



            int tcTileX = tcZone.X.FloorToInt();

            int tcTileY = tcZone.Y.FloorToInt();

            bool foundTownCenterTile = DeterministicPathfinder.TryFindNextTile(state, tcTileX - 3, tcTileY, tcTileX, tcTileY, out _, out _);

            AssertEqual(false, foundTownCenterTile, "pathfinder should reject blocked building footprint target tile");



            ResourceNode firstResource = state.EconomyState.ResourceNodes[0];

            int resourceTileX = firstResource.Position.X.FloorToInt();

            int resourceTileY = firstResource.Position.Y.FloorToInt();

            bool foundResourceTile = DeterministicPathfinder.TryFindNextTile(state, resourceTileX - 2, resourceTileY, resourceTileX, resourceTileY, out _, out _);

            AssertEqual(false, foundResourceTile, "pathfinder should reject blocked resource target tile");

        }

        private static void DestroyedWallOpensPathNextTick()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(18);

            int wallId = AddCompletedWall(state, 0, FixedVector2.FromInts(1, 0));

            bool blocked = DeterministicPathfinder.TryFindNextTile(state, 0, 0, 1, 0, out _, out _);

            state.EntityState.Buildings[state.EntityState.EntityLookup[wallId].Index].IsDead = true;

            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());



            bool opened = DeterministicPathfinder.TryFindNextTile(state, 0, 0, 1, 0, out int nextX, out int nextY);



            AssertEqual(false, blocked, "wall tile should initially block direct path");

            AssertEqual(true, opened, "destroyed wall should open path after cleanup");

            AssertEqual(1, nextX, "opened path should step through former wall tile");

            AssertEqual(0, nextY, "opened path should stay on row");

        }

        private static void NoPathReturnsFailureDeterministically()

        {

            GameState first = CreateOccupancyState(19);

            GameState second = CreateOccupancyState(19);

            AddVerticalBarrier(first, 1, 0, GameData.MapHeightTiles);

            AddVerticalBarrier(second, 1, 0, GameData.MapHeightTiles);



            bool firstFound = DeterministicPathfinder.TryFindNextTile(first, 0, 0, 2, 0, out int firstX, out int firstY);

            bool secondFound = DeterministicPathfinder.TryFindNextTile(second, 0, 0, 2, 0, out int secondX, out int secondY);



            AssertEqual(false, firstFound, "no-path request should fail");

            AssertEqual(firstFound, secondFound, "no-path result should be deterministic");

            AssertEqual(firstX, secondX, "no-path X should be deterministic");

            AssertEqual(firstY, secondY, "no-path Y should be deterministic");

        }

        private static void OccupiedNextStepUsesDeterministicAlternate()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3010);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            Unit mover = state.EntityState.Units[0];

            AssertEqual(Fixed.FromInt(0).Raw, mover.Position.X.Raw, "alternate should keep X deterministic");

            AssertEqual(Fixed.FromInt(1).Raw, mover.Position.Y.Raw, "alternate should step around occupied next tile");

            AssertEqual(true, mover.HasMoveTarget, "alternate pass-around should preserve original move target");

        }

        private static void OccupiedNextStepCanUseDiagonalAlternate()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3030);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            Unit mover = state.EntityState.Units[0];

            AssertEqual(true, mover.Position.X.Raw > 0, "diagonal pass-around should make X progress toward the open diagonal tile");

            AssertEqual(true, mover.Position.Y.Raw > 0, "diagonal pass-around should make Y progress toward the open diagonal tile");

            AssertEqual(true, mover.HasMoveTarget, "diagonal pass-around should preserve original move target");

            AssertNoLiveUnitStacking(state, "diagonal pass-around should not stack units");

        }

        private static void AlternateStepAvoidsOccupiedTiles()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3011);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            Unit mover = state.EntityState.Units[0];

            AssertEqual(true, mover.Position.X.Raw > 0, "mover should use an open diagonal instead of occupied direct/cardinal tiles");

            AssertEqual(true, mover.Position.Y.Raw > 0, "mover should use an open diagonal instead of occupied direct/cardinal tiles");

            AssertNoLiveUnitStacking(state, "alternate step should avoid occupied tiles");

            AssertEqual(true, mover.HasMoveTarget, "blocked mover should keep original move target");

        }

        private static void AlternateStepAvoidsStaticBlockers()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3012);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));

            AddCompletedWall(state, 0, FixedVector2.FromInts(0, 1));

            AddCompletedWall(state, 0, FixedVector2.FromInts(1, 1));

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            Unit mover = state.EntityState.Units[0];

            AssertEqual(Fixed.FromInt(0).Raw, mover.Position.X.Raw, "mover should wait when only alternate is statically blocked");

            AssertEqual(Fixed.FromInt(0).Raw, mover.Position.Y.Raw, "mover should not step into wall-blocked alternate");

            AssertEqual(true, mover.HasMoveTarget, "static-blocked alternate should not clear move target");

        }

        private static void TemporaryLiveUnitBlockagePreservesMoveTarget()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState state = CreateOccupancyState(3013);

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(1, 0));

            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));



            new TickRunner().AdvanceOneTick(state, rules, buffer);



            Unit mover = state.EntityState.Units[0];

            AssertEqual(true, mover.HasMoveTarget, "temporary unit congestion should preserve move target");

            AssertEqual(Fixed.FromInt(2).Raw, mover.MoveTarget.X.Raw, "temporary unit congestion should preserve target X");

            AssertEqual(Fixed.FromInt(0).Raw, mover.MoveTarget.Y.Raw, "temporary unit congestion should preserve target Y");

        }

        private static void MovementReplayDeterminism()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            var recorder = new ReplayRecorder(rules, 91, 1, ReplayInitialState.Nomad);

            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1, 5 }, FixedVector2.FromInts(5, 0))));

            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.NoOp), new NoOpCommand()));

            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));

            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.NoOp), new NoOpCommand()));



            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 4);

            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 4);

            AssertEqual(first.FinalChecksum, second.FinalChecksum, "movement replay should be deterministic");

        }

        private static void MovementLockstep()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(2);

            var session = new LockstepSession(rules, 42, true);

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(5, 0))));

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 6 }, FixedVector2.FromInts(45, 0))));

            AssertEqual(true, session.TryAdvanceOneTick(), "move tick should advance");



            for (int tick = 1; tick < 5; tick++)

            {

                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));

                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));

                AssertEqual(true, session.TryAdvanceOneTick(), "movement continuation tick should advance");

            }



            AssertEqual(0, session.DesyncReports.Count, "movement lockstep should not desync");

            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "movement peer checksums should match");

        }

        private static void MovementSolverV2VillagerFlagDefaultsOff()

        {

            AssertFalse(GameData.EnableMovementSolverV2ForVillagers, "movement v2 should stay disabled by default during scaffold milestone");

        }

        private static void MovementEngineV2FlagDefaultsOn()

        {

            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);

            AssertEqual(true, rules.EnableMovementEngineV2, "movement engine v2 should default on after staged rollout");

        }

        private static void MovementChecksumIncludesV2UnitState()

        {

            var rules = GameRules.CreatePhaseZeroDefaults(1);

            GameState baseState = GameInitializer.CreateNomadStart(2001, 1);

            Unit baseUnit = baseState.EntityState.Units[0];

            baseUnit.Velocity = new FixedVector2(new Fixed(1200), new Fixed(-3400));

            baseUnit.LastSteeringDecisionTick = 17;

            baseUnit.CorridorVersion = 3;

            baseUnit.CorridorStepIndex = 5;

            baseUnit.RetargetCooldownUntilTick = 29;

            baseUnit.MovementBlockedReason = MovementBlockReason.NoPath;

            baseUnit.BlockedSinceTick = 15;

            baseUnit.LastMeaningfulProgressTick = 14;

            ulong withState = StateChecksum.Compute(baseState, rules);



            GameState changedState = GameInitializer.CreateNomadStart(2001, 1);

            Unit changedUnit = changedState.EntityState.Units[0];

            changedUnit.Velocity = new FixedVector2(new Fixed(1200), new Fixed(-3400));

            changedUnit.LastSteeringDecisionTick = 17;

            changedUnit.CorridorVersion = 3;

            changedUnit.CorridorStepIndex = 6;

            changedUnit.RetargetCooldownUntilTick = 29;

            changedUnit.MovementBlockedReason = MovementBlockReason.NoPath;

            changedUnit.BlockedSinceTick = 15;

            changedUnit.LastMeaningfulProgressTick = 14;

            ulong changed = StateChecksum.Compute(changedState, rules);



            AssertEqual(false, withState == changed, "movement v2 state must be checksum-covered");

        }

        private static void RulesChecksumIncludesMovementAndGatherV2Flags()

        {

            GameState state = GameInitializer.CreateNomadStart(2011, 1);

            GameRules baseRules = GameRules.CreatePhaseZeroDefaults(1);

            GameRules movementV2Rules = baseRules.WithMovementEngineV2(false);

            GameRules gatherV2Rules = baseRules.WithGatherEngineV2(false);

            AssertEqual(true, baseRules.EnableMovementEngineV2, "base movement engine v2 default should be on");

            AssertEqual(false, movementV2Rules.EnableMovementEngineV2, "movement v2 override should disable movement engine");

            AssertEqual(true, baseRules.EnableGatherEngineV2, "base gather engine v2 default should be on");

            AssertEqual(false, gatherV2Rules.EnableGatherEngineV2, "gather v2 override should disable gather engine");

            ulong baseChecksum = StateChecksum.Compute(state, baseRules);

            ulong movementChecksum = StateChecksum.Compute(state, movementV2Rules);

            ulong gatherChecksum = StateChecksum.Compute(state, gatherV2Rules);



            AssertEqual(false, baseChecksum == movementChecksum, "movement engine v2 flag must be checksum-covered");

            AssertEqual(false, baseChecksum == gatherChecksum, "gather engine v2 flag must be checksum-covered");

        }

        private static void MovementSolverV2VillagerModeRemainsDeterministic()

        {

            GameRules rules = GameRules.CreatePhaseZeroDefaults(1).WithMovementSolverV2Villagers(true);

            GameState first = GameInitializer.CreateNomadStart(2002, 1);

            GameState second = GameInitializer.CreateNomadStart(2002, 1);

            Unit firstUnit = first.EntityState.Units[0];

            Unit secondUnit = second.EntityState.Units[0];



            FixedVector2 target = firstUnit.Position + FixedVector2.FromInts(8, 6);

            firstUnit.MoveTarget = target;

            firstUnit.HasMoveTarget = true;

            firstUnit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;

            secondUnit.MoveTarget = target;

            secondUnit.HasMoveTarget = true;

            secondUnit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;



            var runner = new TickRunner();

            for (int i = 0; i < 30; i++)

            {

                runner.AdvanceOneTick(first, rules, new CommandBuffer());

                runner.AdvanceOneTick(second, rules, new CommandBuffer());

            }



            ulong firstChecksum = StateChecksum.Compute(first, rules);

            ulong secondChecksum = StateChecksum.Compute(second, rules);

            AssertEqual(firstChecksum, secondChecksum, "movement v2 mode must remain deterministic");

        }

        private static void MovementEngineV2ModeRemainsDeterministic()

        {

            GameRules rules = GameRules.CreatePhaseZeroDefaults(1).WithMovementEngineV2(true);

            GameState first = GameInitializer.CreateNomadStart(2012, 1);

            GameState second = GameInitializer.CreateNomadStart(2012, 1);

            Unit firstUnit = first.EntityState.Units[0];

            Unit secondUnit = second.EntityState.Units[0];



            FixedVector2 target = firstUnit.Position + FixedVector2.FromInts(10, 6);

            firstUnit.MoveTarget = target;

            firstUnit.HasMoveTarget = true;

            firstUnit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;

            secondUnit.MoveTarget = target;

            secondUnit.HasMoveTarget = true;

            secondUnit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;



            var runner = new TickRunner();

            for (int i = 0; i < 30; i++)

            {

                runner.AdvanceOneTick(first, rules, new CommandBuffer());

                runner.AdvanceOneTick(second, rules, new CommandBuffer());

            }



            ulong firstChecksum = StateChecksum.Compute(first, rules);

            ulong secondChecksum = StateChecksum.Compute(second, rules);

            AssertEqual(firstChecksum, secondChecksum, "movement engine v2 mode must remain deterministic");

        }

        private static void MovementSolverV2UpdatesCorridorMemoryFields()

        {

            GameRules rules = GameRules.CreatePhaseZeroDefaults(1).WithMovementSolverV2Villagers(true);

            GameState state = GameInitializer.CreateNomadStart(2003, 1);

            Unit unit = state.EntityState.Units[0];

            unit.MoveTarget = unit.Position + FixedVector2.FromInts(10, 8);

            unit.HasMoveTarget = true;

            unit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;



            int initialCorridorVersion = unit.CorridorVersion;

            int initialStep = unit.CorridorStepIndex;

            int initialDecisionTick = unit.LastSteeringDecisionTick;



            var runner = new TickRunner();

            for (int i = 0; i < 16; i++)

            {

                runner.AdvanceOneTick(state, rules, new CommandBuffer());

            }



            AssertEqual(false, unit.CorridorVersion == initialCorridorVersion, "v2 should stamp corridor identity from target tile");

            AssertEqual(true, unit.CorridorStepIndex >= initialStep, "v2 should track corridor step progression");

            AssertEqual(true, unit.LastSteeringDecisionTick >= initialDecisionTick, "v2 should track steering decisions");

        }

    }
}
