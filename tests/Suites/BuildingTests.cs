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
        private static void ConstructionPacingDoesNotCompleteInstantly()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateConstructionPacingState(2097, out Building foundation);

            new ConstructionSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(true, foundation.IsUnderConstruction, "one builder should not complete a town center in one tick");
            AssertEqual(1, foundation.BuildProgressTicks, "one builder should add one visible progress tick");
            AssertEqual(true, GameData.TownCenterBuildTicks > foundation.BuildProgressTicks, "town center build time should be visibly longer than first progress tick");
        }

        private static void ConstructionProgressAdvancesGradually()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateConstructionPacingState(2098, out Building foundation);

            for (int i = 0; i < 5; i++)
            {
                new ConstructionSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));
            }

            AssertEqual(5, foundation.BuildProgressTicks, "one builder should advance construction gradually over multiple ticks");
            AssertEqual(true, foundation.IsUnderConstruction, "placeholder TC construction should remain observable after a few ticks");
        }

        private static void TwoBuildersInRangeBuildFasterThanOne()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState oneBuilder = GameInitializer.CreateNomadStart(206, 1);
            GameState twoBuilders = GameInitializer.CreateNomadStart(206, 1);
            int tcOne = EntityFactory.CreateTownCenter(oneBuilder, 0, FixedVector2.FromInts(3, 0));
            int tcTwo = EntityFactory.CreateTownCenter(twoBuilders, 0, FixedVector2.FromInts(3, 0));
            oneBuilder.EntityState.Units[0].Position = FixedVector2.FromInts(0, 0);
            twoBuilders.EntityState.Units[0].Position = FixedVector2.FromInts(0, 0);
            twoBuilders.EntityState.Units[1].Position = FixedVector2.FromInts(0, 1);

            var oneBuffer = new CommandBuffer();
            var twoBuffer = new CommandBuffer();
            var runner = new TickRunner();
            oneBuffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcOne, new[] { 1 })));
            twoBuffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcTwo, new[] { 1, 2 })));
            runner.AdvanceOneTick(oneBuilder, rules, oneBuffer);
            runner.AdvanceOneTick(twoBuilders, rules, twoBuffer);
            AddNoOp(oneBuffer, 1, 0, 1);
            AddNoOp(twoBuffer, 1, 0, 1);
            runner.AdvanceOneTick(oneBuilder, rules, oneBuffer);
            runner.AdvanceOneTick(twoBuilders, rules, twoBuffer);
            AddNoOp(oneBuffer, 2, 0, 2);
            AddNoOp(twoBuffer, 2, 0, 2);
            runner.AdvanceOneTick(oneBuilder, rules, oneBuffer);
            runner.AdvanceOneTick(twoBuilders, rules, twoBuffer);

            Building one = oneBuilder.EntityState.Buildings[oneBuilder.EntityState.EntityLookup[tcOne].Index];
            Building two = twoBuilders.EntityState.Buildings[twoBuilders.EntityState.EntityLookup[tcTwo].Index];
            AssertEqual(true, two.BuildProgressTicks > one.BuildProgressTicks, "two builders in range should progress faster than one");
        }

        private static void BuildMoveTargetUsesFoundationInteractionRing()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(2066, 1);
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(6, 2));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(foundationId, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit unit = state.EntityState.Units[0];
            Building foundation = state.EntityState.Buildings[state.EntityState.EntityLookup[foundationId].Index];
            int targetX = SpatialRules.GetTileX(unit.MoveTarget);
            int targetY = SpatialRules.GetTileY(unit.MoveTarget);
            AssertEqual(true, unit.HasMoveTarget, "build assignment should set an approach tile");
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(foundation, targetX, targetY), "build approach tile should not be inside foundation footprint");
            AssertEqual(true, SpatialRules.IsUnitInBuildingInteractionRange(new Unit { Position = FixedVector2.FromInts(targetX, targetY) }, foundation), "build approach tile should be on foundation interaction ring");
        }

        private static void MultipleBuildersReserveDistinctBuildSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(2087, 1);
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(6, 2));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(foundationId, new[] { 1, 2, 3 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertDistinctReservations(state, new[] { 1, 2, 3 }, InteractionReservationKind.BuildSite, foundationId, "builders should reserve different build slots");
        }

        private static void AssignBuildAcceptsTemporaryCongestionIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(1490, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Unit villager = state.EntityState.Units[0];
            villager.Position = FixedVector2.FromInts(0, 0);
            AddCompletedWall(state, 0, FixedVector2.FromInts(1, 0));
            AddCompletedWall(state, 0, FixedVector2.FromInts(0, 1));
            var header = new CommandHeader(state.Tick, 0, 0, CommandType.AssignBuild);
            var command = new AssignBuildCommand(tcId, new[] { villager.Id });

            CommandValidationReport report = CommandValidationInspector.Evaluate(
                state,
                rules,
                new CommandEnvelope(header, command));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(header, command));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, report.Accepted, "temporarily blocked builder should accept long-term intent");
            AssertEqual(CommandValidationReason.TemporaryCongestionAcceptedIntent, report.Reason, "temporarily blocked builder should report congestion acceptance");
            AssertEqual(tcId, state.EntityState.Units[0].CurrentBuildTargetId, "accepted command should keep build target for wait/retry");
            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "temporary congestion should not count as rejection");
        }

        private static void AssignBuildAcceptsMixedSelectionAndIgnoresNonBuilders()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(1492, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            var header = new CommandHeader(state.Tick, 0, 0, CommandType.AssignBuild);
            // Nomad start unit ids: villagers 1-4 + scout 5. Mixed selection should still assign villagers.
            var command = new AssignBuildCommand(tcId, new[] { 1, 2, 3, 4, 5 });

            CommandValidationReport report = CommandValidationInspector.Evaluate(
                state,
                rules,
                new CommandEnvelope(header, command));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(header, command));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, report.Accepted, "mixed villager + scout build selection should be accepted");
            for (int i = 0; i < 4; i++)
            {
                Unit villager = state.EntityState.Units[i];
                AssertEqual(tcId, villager.CurrentBuildTargetId, "villager should receive build target from mixed selection");
            }

            Unit scout = state.EntityState.Units[4];
            AssertEqual(UnitTypeId.Scout, scout.UnitTypeId, "test expects fifth unit to be scout");
            AssertEqual(0, scout.CurrentBuildTargetId, "non-builder scout should be ignored, not assigned build target");
            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "mixed selection should not count as rejected");
        }

        private static void AssignBuildRejectReasonForCompletedTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(1491, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            tc.IsUnderConstruction = false;
            var header = new CommandHeader(state.Tick, 0, 0, CommandType.AssignBuild);
            var command = new AssignBuildCommand(tcId, new[] { 1 });

            CommandValidationReport report = CommandValidationInspector.Evaluate(
                state,
                rules,
                new CommandEnvelope(header, command));

            AssertEqual(false, report.Accepted, "completed target should reject assign-build");
            AssertEqual(CommandValidationReason.TargetComplete, report.Reason, "completed target should report target complete");
            AssertEqual(tcId, report.TargetEntityId, "report should include build target id");
        }

        private static void BuilderInBuildRangeBuildsWithoutMicroMovement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2090, 1);
            int builderId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 10));
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building foundation = state.EntityState.Buildings[state.EntityState.EntityLookup[foundationId].Index];
            foundation.AssignedBuilderIds.Add(builderId);
            Unit builder = FindUnitById(state, builderId);
            builder.CurrentBuildTargetId = foundationId;
            builder.TaskPhase = WorkerTaskPhase.MovingToBuildSlot;
            builder.HasMoveTarget = true;
            builder.MoveTarget = FixedVector2.FromInts(32, 32);
            FixedVector2 originalMoveTarget = builder.MoveTarget;

            new ConstructionSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(WorkerTaskPhase.Building, builder.TaskPhase, "builder in range should enter building phase");
            AssertEqual(false, builder.HasMoveTarget, "builder in build range should stop movement before building");
            AssertEqual(originalMoveTarget.X.Raw, builder.MoveTarget.X.Raw, "build action should not rewrite move target raw x while already in range");
            AssertEqual(originalMoveTarget.Y.Raw, builder.MoveTarget.Y.Raw, "build action should not rewrite move target raw y while already in range");
            AssertEqual(1, foundation.BuildProgressTicks, "builder in range should progress construction");
        }

        private static void BuilderBlockedApproachRetargetsDeterministically()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2075, 1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(14, 20));
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            Building foundation = state.EntityState.Buildings[state.EntityState.EntityLookup[foundationId].Index];
            List<SpatialRules.TileCoord> tiles = SpatialRules.EnumerateBuildInteractionTiles(state, foundation);
            SpatialRules.TileCoord blockedTile = tiles[0];
            state.EntityState.Units[0].Position = FixedVector2.FromInts(0, 0);
            state.EntityState.Units[0].CurrentBuildTargetId = foundationId;
            foundation.AssignedBuilderIds.Add(state.EntityState.Units[0].Id);
            int blockerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(blockedTile.X, blockedTile.Y), false);
            state.EntityState.Units[0].HasMoveTarget = true;
            state.EntityState.Units[0].MoveTarget = FixedVector2.FromInts(blockedTile.X, blockedTile.Y);
            state.EntityState.Units[0].LastMovedTick = 0;
            state.Tick = GameData.NoProgressTimeoutTicks;

            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            AddNoOp(buffer, state.Tick, 0, 8000);
            runner.AdvanceOneTick(state, rules, buffer);

            Unit builder = state.EntityState.Units[0];
            AssertEqual(true, builder.HasMoveTarget, "builder should keep build intent and retarget");
            AssertEqual(false, SpatialRules.GetTileX(builder.MoveTarget) == blockedTile.X && SpatialRules.GetTileY(builder.MoveTarget) == blockedTile.Y, "builder should retarget away from stale blocked tile");
            AssertEqual(foundationId, builder.CurrentBuildTargetId, "builder should keep build target during congestion recovery");
        }

    }
}
