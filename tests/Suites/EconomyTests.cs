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
        private static void ResourceProfilesDefineStockpileKindAndNodeType()
        {
            GatherProfile berries = GameData.GetGatherProfile(GatherProfileId.BerryBush);
            GatherProfile trees = GameData.GetGatherProfile(GatherProfileId.Tree);
            GatherProfile smallGold = GameData.GetGatherProfile(GatherProfileId.GoldVeinSmall);
            GatherProfile largeGold = GameData.GetGatherProfile(GatherProfileId.GoldVeinLarge);

            AssertEqual(ResourceType.Food, berries.ResourceType, "berry profile should deposit food");
            AssertEqual(ResourceNodeType.BerryBush, berries.NodeType, "berry profile should define berry bush nodes");
            AssertEqual(ResourceType.Wood, trees.ResourceType, "tree profile should deposit wood");
            AssertEqual(ResourceNodeType.Tree, trees.NodeType, "tree profile should define tree nodes");
            AssertEqual(ResourceType.Gold, smallGold.ResourceType, "small gold profile should deposit gold");
            AssertEqual(ResourceType.Gold, largeGold.ResourceType, "large gold profile should also deposit gold");
            AssertEqual(ResourceNodeType.GoldVeinSmall, smallGold.NodeType, "small gold profile should define small vein nodes");
            AssertEqual(ResourceNodeType.GoldVeinLarge, largeGold.NodeType, "large gold profile should define large vein nodes");
        }

        private static void NomadResourcesCreateAreasAndProfiledNodes()
        {
            GameState state = GameInitializer.CreateNomadStart(601, 1);
            AssertEqual(6, state.EconomyState.ResourceAreas.Count, "nomad one-player map should create three home areas and three center areas");
            AssertEqual(6, state.EconomyState.ResourceNodes.Count, "nomad one-player map should keep one node per resource area");

            ResourceNode food = state.EconomyState.ResourceNodes[0];
            ResourceArea foodArea = FindResourceAreaById(state, food.ResourceAreaId);
            AssertEqual(ResourceAreaType.BerryPatch, foodArea.AreaType, "food node should belong to berry patch area");
            AssertEqual(GatherProfileId.BerryBush, food.GatherProfileId, "food node should use berry profile");
            AssertEqual(ResourceNodeType.BerryBush, food.NodeType, "food node should be a berry bush");

            ResourceNode wood = state.EconomyState.ResourceNodes[1];
            ResourceArea woodArea = FindResourceAreaById(state, wood.ResourceAreaId);
            AssertEqual(ResourceAreaType.Forest, woodArea.AreaType, "wood node should belong to forest area");
            AssertEqual(GatherProfileId.Tree, wood.GatherProfileId, "wood node should use tree profile");
            AssertEqual(ResourceNodeType.Tree, wood.NodeType, "wood node should be a tree");
        }

        private static void PlacementRejectsOverlappingBuilding()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(7, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            runner.AdvanceOneTick(state, rules, buffer);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(10, 10))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "overlapping wall should be rejected");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "overlapping building placement should count as rejected");
        }

        private static void PlacementRejectsResourceOverlap()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(8, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(6, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "town center should not place over a resource node");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "resource overlap placement should count as rejected");
        }

        private static void PlacementRejectsOutsideMap()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(9, 1);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(-1, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "building should not place outside map");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "outside map placement should count as rejected");
        }

        private static void PlacementRejectionReplayDeterminism()
        {
            var replay = new ReplayFile(1, GameRules.CreatePhaseZeroDefaults(1), 10, 1, ReplayInitialState.Nomad);
            replay.Commands.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(6, 0))));
            ReplayResult first = new ReplayRunner().Run(replay, 1);
            ReplayResult second = new ReplayRunner().Run(replay, 1);

            AssertEqual(first.FinalChecksum, second.FinalChecksum, "rejected placement replay checksums should match");
        }

        private static void PlacementRejectionLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 11, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(6, 0))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "rejected placement tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "rejected placement lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "rejected placement peer checksums should match");
            AssertEqual(1, session.Peers[0].LocalState.DebugCounters.RejectedCommandCount, "rejected placement should be counted in lockstep");
        }

        private static void FirstTownCenterIsFree()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(12, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "first town center should place without resources");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Wood, "first town center should not spend wood");
        }

        private static void SecondTownCenterPaysWood()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(13, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(2, state.EntityState.Buildings.Count, "second town center should place when affordable");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Wood, "second town center should spend wood");
        }

        private static void SecondTownCenterRejectsMissingWood()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(14, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "second town center should reject without wood");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "missing town center wood should count as rejected");
        }

        private static void TownCenterExpansionCostStaysMeaningful()
        {
            AssertEqual(true, GameData.TownCenterWoodCost > GameData.TradePostWoodCost, "normal town center should cost more wood than a trade post");
            AssertEqual(true, GameData.TownCenterWoodCost > GameData.WallWoodCost * 20, "normal town center should be meaningfully more expensive than walling");
        }

        private static void FirstTownCenterBecomesCapital()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(1, 2);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));

            var runner = new TickRunner();
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "one TC should be placed");
            AssertEqual(true, state.EntityState.Buildings[0].IsCapital, "first TC should be capital");
            AssertEqual(true, state.EntityState.Buildings[0].IsUnderConstruction, "placed TC should start under construction");
            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus, state.EntityState.Buildings[0].HitPoints, "capital should get hit point bonus");
            AssertEqual(true, state.PlayerStates.Players[0].CapitalStatus.HasCapitalBeenPlaced, "capital status should be set");
            AssertEqual(false, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "capital bonus should wait for completion");
            AssertEqual(0, state.PlayerStates.Players[0].PopulationCap, "capital should not grant population while under construction");

            int buildingId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(buildingId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, buildingId, 240, 2, 2);

            AssertEqual(false, state.EntityState.Buildings[0].IsUnderConstruction, "assigned villagers should complete construction");
            AssertEqual(true, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "capital bonus should be active after completion");
            AssertEqual(GameData.CapitalPopulationBonus, state.PlayerStates.Players[0].PopulationCap, "capital should grant population bonus");
        }

        private static void SecondTownCenterStaysNormal()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));

            var runner = new TickRunner();
            runner.AdvanceOneTick(state, rules, buffer);
            int firstBuildingId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(firstBuildingId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, firstBuildingId, 240, 2, 2);

            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(2, state.EntityState.Buildings.Count, "two TCs should exist");
            AssertEqual(true, state.EntityState.Buildings[0].IsCapital, "first TC should remain capital");
            AssertEqual(false, state.EntityState.Buildings[1].IsCapital, "second TC should be normal");
            AssertEqual(GameData.TownCenterHitPoints, state.EntityState.Buildings[1].HitPoints, "normal TC should not get capital hit points");
            AssertEqual(GameData.CapitalPopulationBonus, state.PlayerStates.Players[0].PopulationCap, "capital bonus should apply only once");
        }

        private static void CompletedNormalTownCenterStaysWeakerThanCapital()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(15, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);
            int normalTownCenterId = state.EntityState.Buildings[1].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 4, CommandType.AssignBuild), new AssignBuildCommand(normalTownCenterId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, normalTownCenterId, 240, state.Tick, 5);

            AssertEqual(false, state.EntityState.Buildings[1].IsUnderConstruction, "normal town center should complete");
            AssertEqual(GameData.TownCenterHitPoints, state.EntityState.Buildings[1].HitPoints, "completed normal town center should use normal hit points");
            AssertEqual(true, state.EntityState.Buildings[0].HitPoints > state.EntityState.Buildings[1].HitPoints, "capital should remain stronger than normal town center");
        }

        private static void CapitalLossRemovesBonus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));

            runner.AdvanceOneTick(state, rules, buffer);
            int buildingId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(buildingId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, buildingId, 240, 2, 2);
            state.EntityState.Buildings[0].IsDead = true;
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "dead capital should be removed by cleanup");
            AssertEqual(false, state.PlayerStates.Players[0].CapitalStatus.IsCapitalAlive, "capital should no longer be alive");
            AssertEqual(false, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "capital bonus should be inactive after loss");
            AssertEqual(0, state.PlayerStates.Players[0].PopulationCap, "capital population bonus should be removed");
        }

        private static void TownCenterAfterCapitalLossStaysNormal()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(16, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.EntityState.Buildings[0].IsDead = true;
            runner.AdvanceOneTick(state, rules, buffer);
            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "replacement town center should place after capital loss");
            AssertEqual(false, state.EntityState.Buildings[0].IsCapital, "capital cannot be rebuilt");
            AssertEqual(GameData.TownCenterHitPoints, state.EntityState.Buildings[0].HitPoints, "post-loss town center should use normal hit points");
            AssertEqual(true, state.PlayerStates.Players[0].CapitalStatus.HasCapitalBeenPlaced, "capital placement history should remain permanent");
            AssertEqual(false, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "capital bonus should not reactivate");
        }

        private static void NormalTownCenterDoesNotInheritCapitalBonus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateBuildingCombatState();
            state.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;
            EntityFactory.CreateTownCenter(state, 1, FixedVector2.FromInts(3, 0));
            state.EntityState.Buildings[1].IsUnderConstruction = false;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "normal town center should remain after capital loss");
            AssertEqual(false, state.EntityState.Buildings[0].IsCapital, "remaining town center should stay normal");
            AssertEqual(0, state.PlayerStates.Players[1].PopulationCap, "capital population bonus should not transfer to normal town center");
            AssertEqual(false, state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "capital bonus should remain inactive");
        }

        private static void CapitalPlacementReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var recorder = new ReplayRecorder(rules, 99, 2, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(3, 8))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(43, 8))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(11, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.AssignBuild), new AssignBuildCommand(12, new[] { 6, 7, 8, 9 })));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 80);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 80);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "capital placement replay should be deterministic");
        }

        private static void CapitalPlacementLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 123, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(3, 8))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(43, 8))));

            bool advanced = session.TryAdvanceOneTick();
            AssertEqual(true, advanced, "capital placement tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(11, new[] { 1, 2, 3, 4 })));
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.AssignBuild), new AssignBuildCommand(12, new[] { 6, 7, 8, 9 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "capital build assignment tick should advance");
            int tick = 2;
            uint sequence = 2;
            while (tick < 220
                && (!session.Peers[0].LocalState.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive
                    || !session.Peers[0].LocalState.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive))
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, sequence, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "capital completion progression tick should advance");
                tick++;
                sequence++;
            }

            AssertEqual(0, session.DesyncReports.Count, "capital placement should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "peer checksums should match after placement");
            AssertEqual(true, session.Peers[0].LocalState.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "player 0 capital should be active");
            AssertEqual(true, session.Peers[0].LocalState.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "player 1 capital should be active");
        }

        private static void EconomyReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var recorder = new ReplayRecorder(rules, 44, 1, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(4, 0, 4, CommandType.NoOp), new NoOpCommand()));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 5);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 5);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "economy replay should be deterministic");
        }

        private static void EconomyLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var session = new LockstepSession(rules, 22, true);
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

            int startFood = session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Food;
            session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "gather assignment tick should advance");
            tick++;
            for (int i = 0; i < 200 && session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Food < startFood + 10; i++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "gather/deposit progression tick should advance");
                tick++;
            }

            AssertEqual(0, session.DesyncReports.Count, "economy lockstep should not desync");
            AssertEqual(true, session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Food >= startFood + 10, "economy lockstep should gather and deposit food");
            Unit villager = session.Peers[0].LocalState.EntityState.Units[0];
            AssertEqual(1, villager.CurrentResourceNodeId, "villager should keep gather assignment in lockstep");
            AssertEqual(
                true,
                villager.HasMoveTarget
                    || villager.CarriedAmount > 0
                    || villager.TaskPhase == WorkerTaskPhase.Gathering
                    || villager.TaskPhase == WorkerTaskPhase.MovingToResourceSlot,
                "villager should be in deterministic gather loop state phase=" + villager.TaskPhase + " hasMove=" + villager.HasMoveTarget + " carry=" + villager.CarriedAmount);
        }

    }
}
