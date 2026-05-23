using RtsGame.Net.Lockstep;
using RtsGame.Presentation.ClientInput;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Presentation.LocalPlay;
using RtsGame.Presentation.Snapshots;
using RtsGame.Presentation.Visuals;
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
        private static void PresentationSnapshotIncludesVisibleLocalState()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(63, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            state.PlayerStates.Players[0].Resources.Food = 100;
            state.PlayerStates.Players[0].Resources.Wood = 200;
            state.PlayerStates.Players[0].Resources.Gold = 300;
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            AssertEqual(state.Tick, snapshot.Tick, "snapshot should copy tick");
            AssertEqual(0, snapshot.LocalPlayerIndex, "snapshot should copy local player index");
            AssertEqual(true, snapshot.Units.Count > 0, "snapshot should include visible local units");
            AssertEqual(true, snapshot.Buildings.Count > 0, "snapshot should include visible local buildings");
            AssertEqual(100, snapshot.LocalPlayer.Food, "snapshot should copy local food");
            AssertEqual(200, snapshot.LocalPlayer.Wood, "snapshot should copy local wood");
            AssertEqual(300, snapshot.LocalPlayer.Gold, "snapshot should copy local gold");
            AssertEqual(GameData.CapitalPopulationBonus, snapshot.LocalPlayer.PopulationCap, "snapshot should copy local population cap");
            AssertEqual(true, snapshot.LocalPlayer.CapitalBonusActive, "snapshot should copy capital status");
        }

        private static void PresentationSnapshotHidesInvisibleEnemies()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(64, 2);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                AssertFalse(snapshot.Units[i].OwnerPlayerIndex == 1, "snapshot should not include invisible enemy units");
            }
        }

        private static void PresentationSnapshotIncludesVisibleResources()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(80, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            AssertEqual(true, snapshot.Resources.Count >= 3, "snapshot should include visible starting resources");
            AssertEqual(true, HasResource(snapshot, ResourceType.Food), "snapshot should include visible food resource");
            AssertEqual(true, HasResource(snapshot, ResourceType.Wood), "snapshot should include visible wood resource");
            AssertEqual(true, HasResource(snapshot, ResourceType.Gold), "snapshot should include visible gold resource");
        }

        private static void PresentationSnapshotHidesDepletedResources()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(81, 1);
            state.EconomyState.ResourceNodes[0].RemainingAmount = 0;
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            for (int i = 0; i < snapshot.Resources.Count; i++)
            {
                AssertFalse(snapshot.Resources[i].Id == 1, "snapshot should hide depleted resource node");
            }
        }

        private static void PresentationSnapshotIncludesBuildingStatus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(89, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            runner.AdvanceOneTick(state, rules, buffer);
            state.EntityState.Units[0].Position = FixedVector2.FromInts(7, 10);
            state.EntityState.Units[1].Position = FixedVector2.FromInts(13, 10);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2 })));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int tick = 2; tick < 40 && state.EntityState.Buildings[state.EntityState.EntityLookup[6].Index].BuildProgressTicks < 2; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)tick);
                runner.AdvanceOneTick(state, rules, buffer);
            }

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);
            BuildingSnapshot building = FindBuildingSnapshot(snapshot, 6);
            Building simBuilding = state.EntityState.Buildings[state.EntityState.EntityLookup[6].Index];

            AssertEqual(true, building.IsUnderConstruction, "building snapshot should expose construction state");
            AssertEqual(simBuilding.BuildProgressTicks, building.BuildProgressTicks, "building snapshot should expose build progress");
            AssertEqual(true, building.BuildProgressTicks > 0, "building snapshot setup should have active build progress");
            AssertEqual(GameData.TownCenterBuildTicks, building.RequiredBuildTicks, "building snapshot should expose required build ticks");
            AssertEqual(0, building.TrainingQueueCount, "under-construction building should have no training queue");
        }

        private static void PresentationSnapshotIncludesUnitStatus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(91, 1);
            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            runner.AdvanceOneTick(state, rules, buffer);

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);
            UnitSnapshot unit = FindUnitSnapshot(snapshot, 1);

            AssertEqual(1, unit.CurrentResourceNodeId, "unit snapshot should expose gather target");
            AssertEqual(ResourceType.Food, unit.CarriedResourceType, "unit snapshot should expose carried resource type");
            AssertEqual(GameData.VillagerGatherPerTick, unit.CarriedAmount, "unit snapshot should expose carried amount");
            AssertEqual(false, unit.HasMoveTarget, "unit snapshot should expose move target state");
        }

        private static void PresentationSnapshotIncludesTechStatus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateResearchReadyState(165, 1, out int buildingId);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.ResearchTech), new ResearchTechCommand(buildingId, TechId.InfantryAttack1)));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            AssertEqual(1, snapshot.LocalPlayer.ResearchQueue.Count, "snapshot should expose queued research");
            AssertEqual(TechId.InfantryAttack1, snapshot.LocalPlayer.ResearchQueue[0].TechId, "snapshot should expose research tech id");
            AssertEqual(1, snapshot.LocalPlayer.ResearchQueue[0].ProgressTicks, "snapshot should expose research progress");
            AssertEqual(GameData.InfantryAttack1ResearchTicks, snapshot.LocalPlayer.ResearchQueue[0].RequiredTicks, "snapshot should expose research required ticks");
        }

        private static void PresentationSnapshotDoesNotMutateChecksum()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(65, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());
            ulong before = StateChecksum.Compute(state, rules);

            GameSnapshotBuilder.Build(state, 0);
            ulong after = StateChecksum.Compute(state, rules);

            AssertEqual(before, after, "building a presentation snapshot must not mutate simulation state");
        }

    }
}
