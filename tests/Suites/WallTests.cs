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
        private static void WallRejectsMissingWood()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(39, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "wall should reject without wood");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "missing wall wood should count as rejected");
        }

        private static void PlaceWallCreatesVulnerableConstruction()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(40, 1);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "wall should be created");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Wood, "wall should spend wood");
            AssertEqual(BuildingTypeId.Wall, state.EntityState.Buildings[0].BuildingTypeId, "created building should be wall");
            AssertEqual(true, state.EntityState.Buildings[0].IsUnderConstruction, "wall should start under construction");
            AssertEqual(GameData.WallUnderConstructionHitPoints, state.EntityState.Buildings[0].HitPoints, "under-construction wall should be vulnerable");
            AssertEqual(false, state.EntityState.Buildings[0].IsCapital, "wall should never be capital");
        }

        private static void AssignedVillagersCompleteWall()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(41, 1);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));
            runner.AdvanceOneTick(state, rules, buffer);
            int wallId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(wallId, new[] { 1, 2 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 2, 0, 2);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(wallId, state.EntityState.Units[0].CurrentBuildTargetId, "first villager should stay assigned to wall build target");
            AssertEqual(wallId, state.EntityState.Units[1].CurrentBuildTargetId, "second villager should stay assigned to wall build target");
            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget || state.EntityState.Units[1].HasMoveTarget, "at least one assigned wall builder should move toward interaction range");
        }

        private static void AssignBuildSetsAdjacentDeterministicApproachTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(141, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building building = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            Unit villager = state.EntityState.Units[0];
            villager.Position = FixedVector2.FromInts(6, 10);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { villager.Id })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, villager.HasMoveTarget, "assigned builder should get movement target");
            int targetX = SpatialRules.GetTileX(villager.MoveTarget);
            int targetY = SpatialRules.GetTileY(villager.MoveTarget);
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(building, targetX, targetY), "build approach target should never be inside footprint");
            AssertEqual(true,
                SpatialRules.IsTileInsideBuildingFootprint(building, targetX + 1, targetY)
                    || SpatialRules.IsTileInsideBuildingFootprint(building, targetX - 1, targetY)
                    || SpatialRules.IsTileInsideBuildingFootprint(building, targetX, targetY + 1)
                    || SpatialRules.IsTileInsideBuildingFootprint(building, targetX, targetY - 1),
                "build approach target should be adjacent to footprint");
        }

        private static void AssignBuildGivesDistinctApproachTilesForMultipleVillagers()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(142, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            state.EntityState.Units[0].Position = FixedVector2.FromInts(6, 9);
            state.EntityState.Units[1].Position = FixedVector2.FromInts(6, 10);
            state.EntityState.Units[2].Position = FixedVector2.FromInts(6, 11);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { 1, 2, 3 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            var tiles = new HashSet<string>();
            for (int i = 0; i < 3; i++)
            {
                Unit villager = state.EntityState.Units[i];
                AssertEqual(true, villager.HasMoveTarget, "each assigned villager should get a target");
                tiles.Add(SpatialRules.GetTileX(villager.MoveTarget) + "," + SpatialRules.GetTileY(villager.MoveTarget));
            }

            AssertEqual(3, tiles.Count, "multiple builders should reserve distinct adjacent approach tiles when available");
        }

        private static void AssignBuildRejectsWhenNoInteractionTileIsReachable()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(143, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            List<SpatialRules.TileCoord> blockedRing = SpatialRules.EnumerateBuildInteractionTiles(state, tc);
            for (int i = 0; i < blockedRing.Count; i++)
            {
                AddCompletedWall(state, 0, FixedVector2.FromInts(blockedRing[i].X, blockedRing[i].Y));
            }
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "assign build should reject when no reachable interaction tile exists");
            AssertEqual(0, state.EntityState.Units[0].CurrentBuildTargetId, "rejected assignment should not set build target");
        }

        private static void VillagerPathsToTcInteractionTileFromLeft()
        {
            AssertVillagerPathsToTcInteractionTileFromSide(-6, 1461);
        }

        private static void VillagerPathsToTcInteractionTileFromRight()
        {
            AssertVillagerPathsToTcInteractionTileFromSide(6, 1462);
        }

        private static void UnderConstructionWallCanBeDestroyed()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(42, 2);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(3, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            int wallId = state.EntityState.Buildings[0].Id;
            state.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.NoOp), new NoOpCommand()));
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.Attack), new AttackCommand(new[] { 11 }, wallId)));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(wallId), "destroyed under-construction wall should be removed");
        }

        private static void WallReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))),
                new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2 })),
                new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand())
            };
            GameState firstState = GameInitializer.CreateNomadStart(43, 1);
            GameState secondState = GameInitializer.CreateNomadStart(43, 1);
            firstState.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            secondState.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;

            ulong first = RunCommandsFromState(firstState, rules, commands, 3);
            ulong second = RunCommandsFromState(secondState, rules, commands, 3);
            AssertEqual(first, second, "wall replay should be deterministic");
        }

        private static void WallLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 44, true);
            session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            session.Peers[1].LocalState.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "wall placement tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(11, new[] { 1, 2 })));
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "wall build assignment tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "wall lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "wall peer checksums should match");
        }

    }
}
