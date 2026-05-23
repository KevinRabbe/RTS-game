using RtsGame.Net.Lockstep;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;
using RtsGame.Sim.Systems;
using RtsGame.Stress;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void MovementArrivalSnapsWithoutRawOscillation()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2091, 1);
            int unitId = EntityFactory.CreateUnit(
                state,
                0,
                UnitTypeId.Villager,
                new FixedVector2(Fixed.FromRatio(19, 20), Fixed.FromInt(0)));
            Unit unit = FindUnitById(state, unitId);
            unit.HasMoveTarget = true;
            unit.MoveTarget = FixedVector2.FromInts(1, 0);
            unit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
            var context = new TickCommandContext(new List<CommandEnvelope>());

            new MovementSystem().Run(state, rules, context);
            long snappedX = unit.Position.X.Raw;
            long snappedY = unit.Position.Y.Raw;

            AssertEqual(Fixed.FromInt(1).Raw, snappedX, "movement should snap to target when within one deterministic step");
            AssertEqual(Fixed.FromInt(0).Raw, snappedY, "movement snap should keep y stable");
            AssertEqual(false, unit.HasMoveTarget, "movement should clear target after snap arrival");
            AssertEqual(WorkerTaskPhase.Idle, unit.TaskPhase, "command move should return to idle after arrival");

            new MovementSystem().Run(state, rules, context);
            AssertEqual(snappedX, unit.Position.X.Raw, "arrived unit should not oscillate raw x after snap");
            AssertEqual(snappedY, unit.Position.Y.Raw, "arrived unit should not oscillate raw y after snap");
        }

        private static void WorkerSubTileJitterDoesNotResetNoProgressTimeout()
        {
            GameState state = CreateOccupancyState(2097, 1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 10));
            Unit unit = state.EntityState.Units[state.EntityState.Units.Count - 1];
            unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
            unit.HasMoveTarget = true;
            unit.MoveTarget = FixedVector2.FromInts(12, 10);
            unit.LastMovedTick = state.Tick;
            unit.CurrentResourceNodeId = 77;
            SpatialRules.ReserveInteractionSlot(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                77,
                new SpatialRules.TileCoord(12, 10));

            bool progressRecorded = state.MovementProgressPolicy.ShouldRecordProgressTick(
                unit,
                entersNewTile: false,
                reachesTarget: false);
            AssertEqual(true, progressRecorded, "worker movement may still report sub-tile progress");

            state.Tick = GameData.NoProgressTimeoutTicks + 1;
            AssertEqual(
                true,
                SpatialRules.IsInteractionReservationTimedOut(state, unit, InteractionReservationKind.ResourceNode, 77),
                "slot timeout should use reservation age so sub-tile jitter cannot stall recovery forever");
        }

        private static void UnitOrderedToOccupiedDestinationReceivesNearbySlot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = state.EntityState.Units[0];
            AssertEqual(true, mover.HasMoveTarget, "occupied final tile should choose a nearby destination slot instead of clearing intent");
            AssertEqual(InteractionReservationKind.MoveDestination, mover.ReservedInteractionKind, "occupied final tile should reserve a move destination slot");
            AssertEqual(false, SpatialRules.GetTileX(mover.MoveTarget) == 1 && SpatialRules.GetTileY(mover.MoveTarget) == 0, "move destination should not be the occupied tile");
            AssertNoLiveUnitStacking(state, "occupied final tile command should not stack units");
        }

        private static void TcFrontBlockerAllowsPassAroundProgress()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3014);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            int moverId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(10, 6));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(10, 7));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { moverId }, FixedVector2.FromInts(10, 15))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = FindUnitById(state, moverId);
            AssertEqual(true, mover.HasMoveTarget, "tc-front congestion should preserve move target");
            AssertEqual(false, SpatialRules.GetTileX(mover.Position) == 10 && SpatialRules.GetTileY(mover.Position) == 6, "mover should make local pass-around progress near TC");
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(state.EntityState.Buildings[0], SpatialRules.GetTileX(mover.Position), SpatialRules.GetTileY(mover.Position)), "pass-around should not enter TC footprint");
        }

        private static void ResourceDropoffBlockerPreservesWorkerIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3015);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 6));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 7));
            int resourceId = state.EconomyState.NextResourceNodeId++;
            state.EconomyState.ResourceNodes.Add(new ResourceNode
            {
                Id = resourceId,
                ResourceType = ResourceType.Wood,
                Position = FixedVector2.FromInts(26, 20),
                RemainingAmount = GameData.StartingWoodAmount
            });
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 10));
            Unit worker = FindUnitById(state, workerId);
            worker.CurrentResourceNodeId = resourceId;
            worker.CarriedResourceType = ResourceType.Wood;
            worker.CarriedAmount = GameData.VillagerCarryCapacity;
            worker.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
            worker.HasMoveTarget = true;
            worker.MoveTarget = FixedVector2.FromInts(20, 15);

            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            AssertEqual(resourceId, worker.CurrentResourceNodeId, "local traffic avoidance should preserve resource intent");
            AssertEqual(GameData.VillagerCarryCapacity, worker.CarriedAmount, "local traffic avoidance should not fake deposit");
            AssertEqual(true, worker.HasMoveTarget, "local traffic avoidance should keep dropoff move target");
        }

        private static void MovingUnitCanEnterVacatedTileWithoutStacking()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3040);
            int followerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            int leaderId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            Unit follower = FindUnitById(state, followerId);
            Unit leader = FindUnitById(state, leaderId);
            follower.HasMoveTarget = true;
            follower.MoveTarget = FixedVector2.FromInts(2, 0);
            follower.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
            leader.HasMoveTarget = true;
            leader.MoveTarget = FixedVector2.FromInts(3, 0);
            leader.TaskPhase = WorkerTaskPhase.MovingToCommandMove;

            new MovementSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(1, SpatialRules.GetTileX(follower.Position), "follower should be allowed to enter a tile vacated by a moving leader");
            AssertEqual(2, SpatialRules.GetTileX(leader.Position), "leader should move forward first in the traffic chain");
            AssertNoLiveUnitStacking(state, "vacated-tile follow-through should not stack units");
        }

        private static void TwoUnitsAttemptingSameTileReceiveSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(2, 1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1, 2 }, FixedVector2.FromInts(1, 1))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);
            int arrivedOrReserved = 0;
            var reservedTiles = new HashSet<int>();
            for (int i = 1; i <= 2; i++)
            {
                Unit unit = FindUnitById(state, i);
                if (!unit.HasMoveTarget)
                {
                    arrivedOrReserved++;
                    continue;
                }

                AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, "active mover should keep a move destination reservation unit=" + unit.Id);
                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                AssertEqual(true, reservedTiles.Add(key), "active movers should not share destination reservations");
                arrivedOrReserved++;
            }

            AssertEqual(2, arrivedOrReserved, "two-unit move should either arrive or keep stable destination reservations");
        }

        private static void ThreeUnitsAttemptingSameTileReceiveSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(2, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 2));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1, 2, 3 }, FixedVector2.FromInts(1, 1))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);
            int arrivedOrReserved = 0;
            var reservedTiles = new HashSet<int>();
            for (int i = 1; i <= 3; i++)
            {
                Unit unit = FindUnitById(state, i);
                if (!unit.HasMoveTarget)
                {
                    arrivedOrReserved++;
                    continue;
                }

                AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, "active mover should keep a move destination reservation unit=" + unit.Id);
                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                AssertEqual(true, reservedTiles.Add(key), "active movers should not share destination reservations");
                arrivedOrReserved++;
            }

            AssertEqual(3, arrivedOrReserved, "three-unit move should either arrive or keep stable destination reservations");
        }

        private static void TwoUnitTileSwapFails()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(4);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            state.EntityState.Units[0].LastMovedTick = -1;
            state.EntityState.Units[1].LastMovedTick = -1;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 2 }, FixedVector2.FromInts(0, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertNoLiveUnitStacking(state, "two units should not stack while avoiding a swap");
            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "first unit should keep movement intent while avoiding a swap");
            AssertEqual(true, state.EntityState.Units[1].HasMoveTarget, "second unit should keep movement intent while avoiding a swap");
            AssertEqual(false, SpatialRules.GetTileX(state.EntityState.Units[0].Position) == 1 && SpatialRules.GetTileY(state.EntityState.Units[0].Position) == 0, "first unit should not move into second unit's occupied tile");
            AssertEqual(false, SpatialRules.GetTileX(state.EntityState.Units[1].Position) == 0 && SpatialRules.GetTileY(state.EntityState.Units[1].Position) == 0, "second unit should not move into first unit's occupied tile");
        }

        private static void WallBlockingReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState firstState = CreateWallBlockingState(17);
            GameState secondState = CreateWallBlockingState(17);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))),
                new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand())
            };

            ulong first = RunCommandsFromState(firstState, rules, commands, 3);
            ulong second = RunCommandsFromState(secondState, rules, commands, 3);
            AssertEqual(first, second, "wall blocking replay should be deterministic");
        }

        private static void WallBlockingLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 18, true);
            AddCompletedWall(session.Peers[0].LocalState, 0, FixedVector2.FromInts(2, 0));
            AddCompletedWall(session.Peers[1].LocalState, 0, FixedVector2.FromInts(2, 0));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "wall blocking move tick should advance");
            for (int tick = 1; tick <= 2; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "wall blocking continuation tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "wall blocking lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "wall blocking peer checksums should match");
        }

    }
}
