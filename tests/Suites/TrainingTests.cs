using RtsGame.Net.Lockstep;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;
using RtsGame.Sim.Systems;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void TrainVillagerPaysCostAndCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(7, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Food = 50;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnitCount = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Villager)));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.PlayerStates.Players[0].Resources.Food, "training should pay food cost immediately");
            AssertEqual(6, state.PlayerStates.Players[0].PopulationUsed, "training should reserve population immediately");
            AssertEqual(1, state.EntityState.Buildings[0].TrainingQueue.Count, "villager should be queued");

            for (int i = 1; i < GameData.VillagerTrainTicks; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnitCount + 1, state.EntityState.Units.Count, "villager should spawn when training completes");
            AssertEqual(0, state.EntityState.Buildings[0].TrainingQueue.Count, "training queue should be empty after completion");
            AssertEqual(UnitTypeId.Villager, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be villager");
        }

        private static void TrainedVillagerSpawnsOutsideTownCenterFootprint()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1801, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            QueueImmediateVillager(townCenter);

            AdvanceSingleNoOp(state, rules, 0, 0);

            Unit trained = state.EntityState.Units[state.EntityState.Units.Count - 1];
            int tileX = SpatialRules.GetTileX(trained.Position);
            int tileY = SpatialRules.GetTileY(trained.Position);
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(townCenter, tileX, tileY), "trained villager should not spawn inside TC footprint");
            AssertEqual(false, SpatialRules.IsTileBlockedForUnitMovement(state, tileX, tileY), "trained villager should spawn on a walkable tile");
            AssertEqual(0, townCenter.TrainingQueue.Count, "training queue should clear after successful spawn");
        }

        private static void TrainedVillagerAvoidsOccupiedSpawnSlot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1802, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            SpatialRules.TileCoord firstSlot = SpatialRules.EnumerateBuildInteractionTiles(state, townCenter)[0];
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(firstSlot.X, firstSlot.Y), false);
            QueueImmediateVillager(townCenter);

            AdvanceSingleNoOp(state, rules, 0, 0);

            Unit trained = state.EntityState.Units[state.EntityState.Units.Count - 1];
            AssertEqual(false, SpatialRules.GetTileX(trained.Position) == firstSlot.X && SpatialRules.GetTileY(trained.Position) == firstSlot.Y, "spawn should skip occupied first slot");
            AssertEqual(0, townCenter.TrainingQueue.Count, "training queue should clear when another spawn slot is open");
        }

        private static void BlockedSpawnWaitsUntilSlotOpens()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1803, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            var spawnSlots = SpatialRules.EnumerateBuildInteractionTiles(state, townCenter);
            var blockers = new List<int>();
            for (int i = 0; i < spawnSlots.Count; i++)
            {
                blockers.Add(EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(spawnSlots[i].X, spawnSlots[i].Y), false));
            }

            QueueImmediateVillager(townCenter);
            int blockedUnitCount = state.EntityState.Units.Count;

            AdvanceSingleNoOp(state, rules, 0, 0);

            AssertEqual(blockedUnitCount, state.EntityState.Units.Count, "blocked spawn should not create a stacked unit");
            AssertEqual(1, townCenter.TrainingQueue.Count, "completed training should wait while all spawn slots are blocked");
            AssertEqual(1, townCenter.TrainingQueue[0].ProgressTicks, "waiting completed training should stay complete");

            FindUnitById(state, blockers[0]).Position = FixedVector2.FromInts(40, 40);
            AdvanceSingleNoOp(state, rules, 1, 1);

            AssertEqual(blockedUnitCount + 1, state.EntityState.Units.Count, "unit should spawn once an exit slot opens");
            AssertEqual(0, townCenter.TrainingQueue.Count, "queue should clear after delayed spawn");
            Unit trained = state.EntityState.Units[state.EntityState.Units.Count - 1];
            AssertEqual(spawnSlots[0].X, SpatialRules.GetTileX(trained.Position), "delayed spawn should use first newly available deterministic slot X");
            AssertEqual(spawnSlots[0].Y, SpatialRules.GetTileY(trained.Position), "delayed spawn should use first newly available deterministic slot Y");
        }

        private static void MultipleTrainedVillagersUseDifferentSpawnSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1804, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            QueueImmediateVillager(townCenter);
            QueueImmediateVillager(townCenter);

            AdvanceSingleNoOp(state, rules, 0, 0);
            Unit first = state.EntityState.Units[state.EntityState.Units.Count - 1];
            AdvanceSingleNoOp(state, rules, 1, 1);
            Unit second = state.EntityState.Units[state.EntityState.Units.Count - 1];

            AssertEqual(false,
                SpatialRules.GetTileX(first.Position) == SpatialRules.GetTileX(second.Position)
                    && SpatialRules.GetTileY(first.Position) == SpatialRules.GetTileY(second.Position),
                "successive trained villagers should not stack on the same spawn tile");
            AssertEqual(0, townCenter.TrainingQueue.Count, "both queued villagers should spawn when slots are available");
        }

        private static void TrainedVillagerAvoidsReservedSpawnSlot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1805, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            SpatialRules.TileCoord firstSlot = SpatialRules.EnumerateBuildInteractionTiles(state, townCenter)[0];
            int reserverId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(30, 30), false);
            SpatialRules.ReserveInteractionSlot(state, FindUnitById(state, reserverId), InteractionReservationKind.Dropoff, townCenterId, firstSlot);
            QueueImmediateVillager(townCenter);

            AdvanceSingleNoOp(state, rules, 0, 0);

            Unit trained = state.EntityState.Units[state.EntityState.Units.Count - 1];
            AssertEqual(false, SpatialRules.GetTileX(trained.Position) == firstSlot.X && SpatialRules.GetTileY(trained.Position) == firstSlot.Y, "spawn should skip reserved final-purpose slots");
        }

        private static void SpawnSlotSelectionIsDeterministic()
        {
            FixedVector2 first = RunImmediateSpawnAndReturnPosition(1806);
            FixedVector2 second = RunImmediateSpawnAndReturnPosition(1806);

            AssertEqual(first.X.Raw, second.X.Raw, "spawn X should be deterministic");
            AssertEqual(first.Y.Raw, second.Y.Raw, "spawn Y should be deterministic");
        }

        private static void TrainVillagerRejectsMissingResources()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(7, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            int buildingId = state.EntityState.Buildings[0].Id;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Villager)));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings[0].TrainingQueue.Count, "training should reject without food");
            AssertEqual(5, state.PlayerStates.Players[0].PopulationUsed, "rejected training should not reserve population");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "rejected command should be counted");
        }

        private static void TrainVillagerRespectsPopulationCap()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(7, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Food = 500;
            state.PlayerStates.Players[0].PopulationCap = state.PlayerStates.Players[0].PopulationUsed;
            int buildingId = state.EntityState.Buildings[0].Id;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Villager)));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings[0].TrainingQueue.Count, "training should reject when population is capped");
            AssertEqual(500, state.PlayerStates.Players[0].Resources.Food, "rejected population cap training should not spend food");
        }

        private static void TrainingReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var recorder = new ReplayRecorder(rules, 88, 1, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(4, 0, 4, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(5, 0, 5, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(6, 0, 6, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(7, 0, 7, CommandType.TrainUnit), new TrainUnitCommand(6, UnitTypeId.Villager)));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(8, 0, 8, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(9, 0, 9, CommandType.NoOp), new NoOpCommand()));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 10);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 10);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "training replay should be deterministic");
        }

        private static void TrainingLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var session = new LockstepSession(rules, 66, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            AssertEqual(true, session.TryAdvanceOneTick(), "place TC tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2, 3, 4 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "assign build tick should advance");
            int tick = 2;
            uint sequence = 2;
            while (tick < 100 && !session.Peers[0].LocalState.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "capital completion tick should advance");
                tick++;
            }

            session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Food = 50;
            session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.TrainUnit), new TrainUnitCommand(6, UnitTypeId.Villager)));
            AssertEqual(true, session.TryAdvanceOneTick(), "train command tick should advance");
            tick++;
            for (int i = 1; i < GameData.VillagerTrainTicks; i++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "training progression tick should advance");
                tick++;
            }

            AssertEqual(0, session.DesyncReports.Count, "training lockstep should not desync");
            AssertEqual(6, session.Peers[0].LocalState.EntityState.Units.Count, "trained villager should exist");
        }

    }
}
