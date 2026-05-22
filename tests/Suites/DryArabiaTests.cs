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
        private static void DryArabiaResourcesCreateTypedAreas()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(602);
            AssertEqual(9, state.EconomyState.ResourceAreas.Count, "dry arabia should create player home areas plus center contested areas");
            AssertEqual(16, state.EconomyState.ResourceNodes.Count, "dry arabia should keep existing resource node count");

            ResourceNode playerGold = FindResourceNodeById(state, 5);
            ResourceArea playerGoldArea = FindResourceAreaById(state, playerGold.ResourceAreaId);
            AssertEqual(ResourceAreaType.GoldDeposit, playerGoldArea.AreaType, "home gold should be in a gold deposit area");
            AssertEqual(GatherProfileId.GoldVeinSmall, playerGold.GatherProfileId, "home gold should use small gold profile");
            AssertEqual(ResourceNodeType.GoldVeinSmall, playerGold.NodeType, "home gold should be a small vein");

            ResourceNode centerGold = FindResourceNodeById(state, 13);
            AssertEqual(ResourceType.Gold, centerGold.ResourceType, "center gold should still deposit gold");
            AssertEqual(GatherProfileId.GoldVeinLarge, centerGold.GatherProfileId, "center gold should use large gold profile");
            AssertEqual(ResourceNodeType.GoldVeinLarge, centerGold.NodeType, "center gold should be a large vein");
        }

        private static void DryArabiaTestMapInitializesDeterministically()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState first = GameInitializer.CreateDryArabiaTest01(123);
            GameState second = GameInitializer.CreateDryArabiaTest01(123);

            AssertEqual(StateChecksum.Compute(first, rules), StateChecksum.Compute(second, rules), "dry arabia test map initialization should be deterministic");
            AssertEqual(2, first.PlayerStates.Players.Count, "dry arabia test map should create exactly two players");
            AssertEqual(10, first.EntityState.Units.Count, "dry arabia test map should create five units per player");
            AssertEqual(4, CountUnits(first, 0, UnitTypeId.Villager), "player 0 should start with four villagers");
            AssertEqual(1, CountUnits(first, 0, UnitTypeId.Scout), "player 0 should start with one scout");
            AssertEqual(4, CountUnits(first, 1, UnitTypeId.Villager), "player 1 should start with four villagers");
            AssertEqual(1, CountUnits(first, 1, UnitTypeId.Scout), "player 1 should start with one scout");
            AssertEqual(true, CountNeutralTradePosts(first) >= 2, "dry arabia test map should include neutral trade posts");
            AssertEqual(true, HasResourceAt(first, ResourceType.Gold, FixedVector2.FromInts(64, 48)), "dry arabia test map should include center contested gold");
        }

        private static void DryArabiaTestMapHasValidTcPlacementZones()
        {
            AssertDryArabiaTcPlacementAccepted(0);
            AssertDryArabiaTcPlacementAccepted(1);
        }

        private static void DryArabiaTestMapHasNearbyResources()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(125);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                AssertEqual(true, HasNearbyResource(state, tc, ResourceType.Food, 10), "player " + player + " should have nearby food");
                AssertEqual(true, HasNearbyResource(state, tc, ResourceType.Wood, 10), "player " + player + " should have nearby wood");
                AssertEqual(true, HasNearbyResource(state, tc, ResourceType.Gold, 10), "player " + player + " should have nearby gold");
            }
        }

        private static void DryArabiaTestMapResourcesAvoidTcZones()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(126);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
                {
                    long combinedRaw = Fixed.FromInt(GameData.TownCenterPlacementRadiusTiles + GameData.ResourcePlacementRadiusTiles).Raw;
                    long combinedSquaredRaw = checked(combinedRaw * combinedRaw);
                    AssertEqual(
                        true,
                        (tc - state.EconomyState.ResourceNodes[i].Position).LengthSquaredRaw() >= combinedSquaredRaw,
                        "resource " + state.EconomyState.ResourceNodes[i].Id + " should not block player " + player + " TC zone");
                }
            }
        }

        private static void DryArabiaTcZonesHaveOpenTrafficBuffer()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(127);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
                {
                    ResourceNode node = state.EconomyState.ResourceNodes[i];
                    if (node.IsDepleted)
                    {
                        continue;
                    }

                    if (state.EconomyState.ResourceAreas.Count > 0)
                    {
                        ResourceArea area = FindResourceAreaById(state, node.ResourceAreaId);
                        if (area.AreaType == ResourceAreaType.None)
                        {
                            continue;
                        }
                    }

                    long distanceRaw = (tc - node.Position).LengthSquaredRaw();
                    long minBufferRaw = Fixed.FromInt(4).Raw;
                    long minBufferSquaredRaw = checked(minBufferRaw * minBufferRaw);
                    AssertEqual(true, distanceRaw >= minBufferSquaredRaw, "resource " + node.Id + " should keep a wider traffic buffer from player " + player + " TC zone");
                }
            }
        }

        private static void DryArabiaStartingResourceAreasContainNodes()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(128);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                AssertEqual(true, HasResourceAreaWithNodeNear(state, tc, ResourceAreaType.BerryPatch, 12), "player " + player + " should have a nearby berry patch with at least one node");
                AssertEqual(true, HasResourceAreaWithNodeNear(state, tc, ResourceAreaType.Forest, 12), "player " + player + " should have a nearby forest with at least one node");
                AssertEqual(true, HasResourceAreaWithNodeNear(state, tc, ResourceAreaType.GoldDeposit, 12), "player " + player + " should have a nearby gold deposit with at least one node");
            }
        }

        private static void DryArabiaStartingResourceAreasHaveValidInteractionSlots()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(129);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                AssertEqual(true, HasResourceInteractionSlotsNear(state, tc, ResourceAreaType.BerryPatch, 12), "player " + player + " nearby berries should expose interaction slots");
                AssertEqual(true, HasResourceInteractionSlotsNear(state, tc, ResourceAreaType.Forest, 12), "player " + player + " nearby wood should expose interaction slots");
                AssertEqual(true, HasResourceInteractionSlotsNear(state, tc, ResourceAreaType.GoldDeposit, 12), "player " + player + " nearby gold should expose interaction slots");
            }
        }

        private static void DryArabiaTcFoundationHasReachableInteractionRing()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(1261);
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            FixedVector2 tcZone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcZone)));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Building tc = FindLatestPlayerBuilding(state, 0, BuildingTypeId.TownCenter);
            List<SpatialRules.TileCoord> tiles = SpatialRules.EnumerateBuildInteractionTiles(state, tc);
            AssertEqual(true, tiles.Count >= 8, "town center should expose an interaction ring with multiple tiles");

            int reachableCount = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (AnyPlayerVillagerCanReachTile(state, 0, tiles[i].X, tiles[i].Y))
                {
                    reachableCount++;
                }
            }

            AssertEqual(true, reachableCount >= 6, "starting area should keep most build interaction tiles reachable");
        }

        private static void DryArabiaStartingVillagersCanAllReceiveBuildAssignment()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(1262);
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            FixedVector2 tcZone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var place = new CommandBuffer();
            place.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcZone)));
            place.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, place);

            Building tc = FindLatestPlayerBuilding(state, 0, BuildingTypeId.TownCenter);
            int[] villagers = GetPlayerVillagerIds(state, 0);
            var assign = new CommandBuffer();
            assign.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tc.Id, villagers)));
            assign.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 1, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, assign);

            AssertEqual(villagers.Length, tc.AssignedBuilderIds.Count, "assign-build should accept all selected starting villagers");
            AssertEqual(true, tc.AssignedBuilderIds.Count >= 3, "at least three starting villagers should be assigned together");
        }

        private static void DryArabiaDecorativeRockTilesAreNotSimBlockers()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(1263);
            int[,] decorativeRockTiles =
            {
                { 58, 42 },
                { 66, 50 },
                { 62, 46 },
                { 35, 30 },
                { 90, 65 }
            };

            for (int i = 0; i < decorativeRockTiles.GetLength(0); i++)
            {
                int tileX = decorativeRockTiles[i, 0];
                int tileY = decorativeRockTiles[i, 1];
                AssertEqual(false, SpatialRules.IsTileBlockedForUnitMovement(state, tileX, tileY), "decorative terrain tile should not be a sim blocker at (" + tileX + "," + tileY + ")");
            }
        }

        private static void WorkerLoopRemainsUnstuckOverLongDryArabiaRun()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(1463);
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            int[] villagers = GetPlayerVillagerIds(state, 0);
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tcId, villagers)));
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int i = 0; i < 900 && FindUnderConstructionBuildingIdOrZero(state, 0, BuildingTypeId.TownCenter) != 0; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(1000 + i * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(1001 + i * 2));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            int completedTcId = FindCompletedBuildingId(state, 0, BuildingTypeId.TownCenter);
            AssertEqual(true, completedTcId != 0, "town center should complete during long worker loop");

            int foodNodeId = FindNearestResourceNodeId(state, tcPos, ResourceType.Food);
            int woodNodeId = FindNearestResourceNodeId(state, tcPos, ResourceType.Wood);
            int goldNodeId = FindNearestResourceNodeId(state, tcPos, ResourceType.Gold);
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2000, CommandType.GatherResource), new GatherResourceCommand(foodNodeId, new[] { villagers[0] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2001, CommandType.GatherResource), new GatherResourceCommand(woodNodeId, new[] { villagers[1] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2002, CommandType.GatherResource), new GatherResourceCommand(goldNodeId, new[] { villagers[2] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 2003, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int activeWorkerTicks = 0;
            for (int i = 0; i < 6000; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(3000 + i * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(3001 + i * 2));
                runner.AdvanceOneTick(state, rules, buffer);

                for (int v = 0; v < villagers.Length; v++)
                {
                    Unit unit = state.EntityState.Units[state.EntityState.EntityLookup[villagers[v]].Index];
                    if (unit.CurrentResourceNodeId != 0 || unit.CarriedAmount > 0 || unit.HasMoveTarget)
                    {
                        activeWorkerTicks++;
                        break;
                    }
                }
            }

            AssertEqual(true, activeWorkerTicks > 0, "workers should remain in active deterministic movement/gather states over long run");
        }

        private static void DryArabiaTcBuildAssignmentProgressesAndUpdatesPopulation()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(144);
            var runner = new TickRunner();
            var buffer = new CommandBuffer();
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { 1, 2, 3, 4 })));
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int assignedBuilders = 0;
            for (int i = 0; i < 4; i++)
            {
                if (state.EntityState.Units[i].CurrentBuildTargetId == tcId)
                {
                    assignedBuilders++;
                }
            }

            AssertEqual(4, assignedBuilders, "dry arabia build assignment should assign all selected villagers to tc build target");

            for (int tick = 2; tick < 120 && state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index].IsUnderConstruction; tick++)
            {
                buffer.Add(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                buffer.Add(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            AssertEqual(false, tc.IsUnderConstruction, "town center should complete after deterministic approach/build");
            AssertEqual(GameData.CapitalPopulationBonus, state.PlayerStates.Players[0].PopulationCap, "completed capital tc should grant population cap bonus");
            AssertEqual(5, state.PlayerStates.Players[0].PopulationUsed, "population used should remain 5/10 after completion");
        }

        private static void PlayabilityInvariantFiveVillagersBuildAssignmentTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(27101, 1);
            while (state.EntityState.Units.Count < 5)
            {
                int offset = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(8 + offset, 8));
            }

            int[] unitIds = new int[5];
            int foundVillagers = 0;
            for (int i = 0; i < state.EntityState.Units.Count && foundVillagers < 5; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.OwnerPlayerIndex == 0 && unit.UnitTypeId == UnitTypeId.Villager)
                {
                    unitIds[foundVillagers++] = unit.Id;
                }
            }

            AssertEqual(5, foundVillagers, "test setup should provide five villagers");
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            int startRejected = state.DebugCounters.RejectedCommandCount;
            var traces = new Queue<string>();
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(14, 10))));
            runner.AdvanceOneTick(state, rules, buffer);
            int wallId = state.EntityState.Buildings[state.EntityState.Buildings.Count - 1].Id;
            Building foundation = state.EntityState.Buildings[state.EntityState.EntityLookup[wallId].Index];

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(wallId, unitIds)));
            runner.AdvanceOneTick(state, rules, buffer);

            bool sawBuilderIntent = false;
            for (int tick = 0; tick < 180; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(40000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, unitIds, traces, 40);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("build invariant stacking", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("build invariant reservations", traces));
                for (int i = 0; i < unitIds.Length; i++)
                {
                    Unit loopUnit = FindUnitById(state, unitIds[i]);
                    if (loopUnit.CurrentBuildTargetId == wallId)
                    {
                        sawBuilderIntent = true;
                    }
                }
            }

            AssertEqual(true, sawBuilderIntent, BuildTraceFailureMessage("at least one builder should keep build intent during the scenario", traces));
            AssertEqual(true, foundation.BuildProgressTicks > 0, BuildTraceFailureMessage("foundation should gain build progress", traces));
            AssertEqual(true, state.DebugCounters.RejectedCommandCount - startRejected <= 1, BuildTraceFailureMessage("legal build assignment should not spam rejections", traces));
            AssertNoEndlessWorkerPhase(state, unitIds, WorkerTaskPhase.MovingToBuildSlot, 180, BuildTraceFailureMessage("builders stuck in moving-to-build phase", traces));
        }

        private static void PlayabilityInvariantMixedResourceWorkersSharedTcTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(27102);
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            var traces = new Queue<string>();
            int startRejected = state.DebugCounters.RejectedCommandCount;
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);

            for (int i = 0; i < 180; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(41000 + i * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(41001 + i * 2));
                runner.AdvanceOneTick(state, rules, buffer);
                if (!state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index].IsUnderConstruction)
                {
                    break;
                }
            }

            while (GetPlayerVillagerIds(state, 0).Length < 5)
            {
                int index = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(
                    state,
                    0,
                    UnitTypeId.Villager,
                    FixedVector2.FromInts(tcPos.X.FloorToInt() - 3 + (index % 3), tcPos.Y.FloorToInt() + 5 + (index % 2)));
            }
            int[] workers = GetPlayerVillagerIds(state, 0);

            int foodId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            int goldId = FindFirstResourceNodeIdByType(state, ResourceType.Gold);
            int woodId = FindFirstResourceNodeIdByType(state, ResourceType.Wood);
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.GatherResource), new GatherResourceCommand(foodId, new[] { workers[0], workers[1] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.GatherResource), new GatherResourceCommand(goldId, new[] { workers[2], workers[3] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 4, CommandType.GatherResource), new GatherResourceCommand(woodId, new[] { workers[4] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 2, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int startingFood = state.PlayerStates.Players[0].Resources.Food;
            int startingGold = state.PlayerStates.Players[0].Resources.Gold;
            int startingWood = state.PlayerStates.Players[0].Resources.Wood;
            bool sawCarriedResources = false;
            for (int tick = 0; tick < 900; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(42000 + tick * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(42001 + tick * 2));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, workers, traces, 50);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("mixed-resource stacking invariant", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("mixed-resource reservation invariant", traces));
                for (int i = 0; i < workers.Length; i++)
                {
                    Unit loopUnit = FindUnitById(state, workers[i]);
                    if (loopUnit.CarriedAmount > 0)
                    {
                        sawCarriedResources = true;
                    }
                }
            }

            bool anyStockpileProgress =
                state.PlayerStates.Players[0].Resources.Food > startingFood
                || state.PlayerStates.Players[0].Resources.Gold > startingGold
                || state.PlayerStates.Players[0].Resources.Wood > startingWood;
            AssertEqual(true, anyStockpileProgress || sawCarriedResources, BuildTraceFailureMessage("mixed workers should make gather/deposit progress evidence", traces));
            AssertEqual(true, state.DebugCounters.RejectedCommandCount - startRejected <= 2, BuildTraceFailureMessage("legal gather commands should not spam rejections", traces));
            AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToResourceSlot, 900, BuildTraceFailureMessage("workers stuck moving-to-resource", traces));
            AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToDropoffSlot, 900, BuildTraceFailureMessage("workers stuck moving-to-dropoff", traces));
        }

        private static void PlayabilityInvariantTenWorkersAroundTcMovementTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(27103, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            while (state.EntityState.Units.Count < 10)
            {
                int i = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(16 + (i % 5), 24 + (i / 5)));
            }

            int[] unitIds = new int[10];
            for (int i = 0; i < 10; i++)
            {
                unitIds[i] = state.EntityState.Units[i].Id;
            }

            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            var traces = new Queue<string>();
            int startRejected = state.DebugCounters.RejectedCommandCount;
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(20, 18))));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 0; tick < 240; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(43000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, unitIds, traces, 40);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("ten-worker move stacking invariant", traces));
            }

            int arrivedOrWaiting = 0;
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (!unit.HasMoveTarget || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting || unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)
                {
                    arrivedOrWaiting++;
                }
            }

            AssertEqual(true, arrivedOrWaiting >= 8, BuildTraceFailureMessage("most workers should settle or wait cleanly around tc movement", traces));
            AssertEqual(true, state.DebugCounters.RejectedCommandCount - startRejected <= 1, BuildTraceFailureMessage("legal group move should not spam rejections", traces));
        }

        private static void PlayabilityInvariantTrainedVillagersGatherTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateSingleNodeResourceAreaState(27104, GatherProfileId.BerryBush, out _, out _);
            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            state.PlayerStates.Players[0].Resources.Food = 500;
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            var traces = new Queue<string>();

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.TrainUnit), new TrainUnitCommand(tcId, UnitTypeId.Villager)));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 1, CommandType.TrainUnit), new TrainUnitCommand(tcId, UnitTypeId.Villager)));
            runner.AdvanceOneTick(state, rules, buffer);
            int initialCount = state.EntityState.Units.Count;

            for (int tick = 0; tick < 800 && state.EntityState.Units.Count < initialCount + 2; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(44000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(true, state.EntityState.Units.Count >= initialCount + 2, "trained villagers should spawn in bounded ticks");
            int foodId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            int[] trainedIds = new[] { state.EntityState.Units[initialCount].Id, state.EntityState.Units[initialCount + 1].Id };
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.GatherResource), new GatherResourceCommand(foodId, trainedIds)));
            runner.AdvanceOneTick(state, rules, buffer);

            bool intentRetained = false;
            for (int tick = 0; tick < 700; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(45000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, trainedIds, traces, 40);
                for (int i = 0; i < trainedIds.Length; i++)
                {
                    Unit loopUnit = FindUnitById(state, trainedIds[i]);
                    if (loopUnit.CurrentResourceAreaId != 0 || loopUnit.CurrentResourceNodeId != 0 || loopUnit.TaskPhase == WorkerTaskPhase.BlockedWaiting)
                    {
                        intentRetained = true;
                    }
                }
            }

            AssertEqual(true, intentRetained, BuildTraceFailureMessage("trained villagers should retain gather intent even under congestion", traces));
            AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("trained villagers stacking invariant", traces));
        }

        private static void PlayabilityInvariantDepletionContinuationPressureTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTwoNodeResourceAreaState(27105, GatherProfileId.Tree, out int firstNodeId, out int secondNodeId, out int areaId);
            while (state.EntityState.Units.Count < 3)
            {
                int idx = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(3 + idx, 0));
            }

            ResourceNode firstNode = FindResourceNodeById(state, firstNodeId);
            firstNode.RemainingAmount = GameData.VillagerCarryCapacity;
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            int[] unitIds = new int[3];
            for (int i = 0; i < 3; i++)
            {
                unitIds[i] = state.EntityState.Units[i].Id;
            }
            var traces = new Queue<string>();

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.GatherResource), new GatherResourceCommand(firstNodeId, unitIds)));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 0; tick < 700; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(46000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, unitIds, traces, 50);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("depletion continuation stacking invariant", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("depletion continuation reservation invariant", traces));
            }

            ResourceNode secondNode = FindResourceNodeById(state, secondNodeId);
            AssertEqual(true, firstNode.IsDepleted, BuildTraceFailureMessage("first node should deplete under pressure", traces));
            bool areaOrNodeRetained = false;
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                areaOrNodeRetained = areaOrNodeRetained
                    || unit.CurrentResourceAreaId == areaId
                    || unit.CurrentResourceNodeId == secondNodeId
                    || (unit.CarriedResourceType == ResourceType.Wood && unit.CarriedAmount > 0);
            }

            bool secondNodeTouched = secondNode.RemainingAmount < GameData.StartingWoodAmount || secondNode.IsDepleted;
            AssertEqual(true, secondNodeTouched || areaOrNodeRetained, BuildTraceFailureMessage("workers should continue or retain valid area intent after first depletion", traces));
            AssertEqual(true, areaOrNodeRetained, BuildTraceFailureMessage("workers should retain area intent or continue on next node", traces));
        }

        private static void SimulationScenarioHarnessMixedEconomyWorkflow()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(27106);
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var scenario = new SimScenarioHarness(state, rules, 2, 47000);

            scenario.Step(
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)),
                new CommandEnvelope(new CommandHeader(state.Tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);
            scenario.RunTicks(200, () => !state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index].IsUnderConstruction);

            while (GetPlayerVillagerIds(state, 0).Length < 5)
            {
                int index = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(
                    state,
                    0,
                    UnitTypeId.Villager,
                    FixedVector2.FromInts(tcPos.X.FloorToInt() - 3 + (index % 3), tcPos.Y.FloorToInt() + 5 + (index % 2)));
            }

            int[] workers = GetPlayerVillagerIds(state, 0);
            int foodId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            int goldId = FindFirstResourceNodeIdByType(state, ResourceType.Gold);
            int woodId = FindFirstResourceNodeIdByType(state, ResourceType.Wood);
            int startingFood = state.PlayerStates.Players[0].Resources.Food;
            int startingGold = state.PlayerStates.Players[0].Resources.Gold;
            int startingWood = state.PlayerStates.Players[0].Resources.Wood;

            scenario.Step(
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 1, CommandType.GatherResource), new GatherResourceCommand(foodId, new[] { workers[0], workers[1] })),
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.GatherResource), new GatherResourceCommand(goldId, new[] { workers[2], workers[3] })),
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.GatherResource), new GatherResourceCommand(woodId, new[] { workers[4] })),
                new CommandEnvelope(new CommandHeader(state.Tick, 1, 1, CommandType.NoOp), new NoOpCommand()));

            bool sawGatherOrCarry = false;
            bool sawActiveIntent = false;
            for (int tick = 0; tick < 700; tick++)
            {
                scenario.StepNoOps();
                scenario.CaptureWorkerTrace(workers, 60);
                scenario.AssertCoreInvariants("mixed-economy");

                for (int i = 0; i < workers.Length; i++)
                {
                    Unit unit = FindUnitById(state, workers[i]);
                    if (unit.TaskPhase == WorkerTaskPhase.Gathering || unit.CarriedAmount > 0)
                    {
                        sawGatherOrCarry = true;
                    }

                    if (unit.CurrentResourceAreaId != 0
                        || unit.CurrentResourceNodeId != 0
                        || unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot
                        || unit.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot
                        || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting)
                    {
                        sawActiveIntent = true;
                    }
                }
            }

            bool anyStockpileProgress =
                state.PlayerStates.Players[0].Resources.Food > startingFood
                || state.PlayerStates.Players[0].Resources.Gold > startingGold
                || state.PlayerStates.Players[0].Resources.Wood > startingWood;
            AssertEqual(true, sawGatherOrCarry, scenario.Fail("expected at least one worker to gather or carry"));
            AssertEqual(true, anyStockpileProgress || sawActiveIntent, scenario.Fail("expected mixed economy progress or retained active intent"));
            AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToResourceSlot, 900, scenario.Fail("workers stuck moving-to-resource"));
            AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToDropoffSlot, 900, scenario.Fail("workers stuck moving-to-dropoff"));
        }

        private static void SimScenarioFiveWorkerRepeatedResourceSwitchCycles()
        {
            RunFiveWorkerResourceSwitchScenario(31101);
        }

        private static void SimScenarioFiveWorkerMoveGatherHotspotChurn()
        {
            RunFiveWorkerMoveGatherHotspotScenario(31102);
        }

        private static void SimScenarioFiveWorkerBuildGatherMoveGatherCycles()
        {
            RunFiveWorkerBuildGatherMoveGatherScenario(31103);
        }

        private static void SimScenarioFiveWorkersFoodSustainedProgress()
        {
            RunFiveWorkersSingleResourceSustainedProgress(31201, GatherProfileId.BerryBush, ResourceType.Food, "five-workers-food");
        }

        private static void SimScenarioFiveWorkersWoodSustainedProgress()
        {
            RunFiveWorkersSingleResourceSustainedProgress(31202, GatherProfileId.Tree, ResourceType.Wood, "five-workers-wood");
        }

        private static void SimScenarioFiveWorkersGoldSustainedProgress()
        {
            RunFiveWorkersSingleResourceSustainedProgress(31203, GatherProfileId.GoldVeinSmall, ResourceType.Gold, "five-workers-gold");
        }

        private static void SimScenarioDryArabiaTcFrontResourceFlowRegression()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(31240);
            var scenario = new SimScenarioHarness(state, rules, 2, 96000);
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);

            scenario.Step(
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 96001, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)),
                new CommandEnvelope(new CommandHeader(state.Tick, 1, 96002, CommandType.NoOp), new NoOpCommand()));

            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);
            int[] startingBuilders = GetPlayerVillagerIds(state, 0);
            scenario.Step(
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 96003, CommandType.AssignBuild), new AssignBuildCommand(tcId, startingBuilders)),
                new CommandEnvelope(new CommandHeader(state.Tick, 1, 96004, CommandType.NoOp), new NoOpCommand()));
            scenario.RunTicks(220, () => !state.EntityState.EntityLookup.ContainsKey(tcId) || !state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index].IsUnderConstruction);

            while (GetPlayerVillagerIds(state, 0).Length < 5)
            {
                int idx = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(
                    state,
                    0,
                    UnitTypeId.Villager,
                    FixedVector2.FromInts(tcPos.X.FloorToInt() + (idx % 3) - 1, tcPos.Y.FloorToInt() + 3 + (idx % 2)));
            }

            int[] workers = GetPlayerVillagerIds(state, 0);
            Array.Sort(workers);
            if (workers.Length > 5)
            {
                Array.Resize(ref workers, 5);
            }

            int primaryWorkerId = workers[0];
            int[] crowdWorkers = new int[] { workers[1], workers[2], workers[3], workers[4] };
            int foodId = FindFirstResourceNodeIdByType(state, ResourceType.Food);

            int farGoldId = 0;
            int nearGoldId = 0;
            int farDist = int.MinValue;
            int nearDist = int.MaxValue;
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted || node.ResourceType != ResourceType.Gold)
                {
                    continue;
                }

                int dist =
                    Math.Abs(tcPos.X.FloorToInt() - node.Position.X.FloorToInt())
                    + Math.Abs(tcPos.Y.FloorToInt() - node.Position.Y.FloorToInt());
                if (dist > farDist)
                {
                    farDist = dist;
                    farGoldId = node.Id;
                }

                if (dist < nearDist)
                {
                    nearDist = dist;
                    nearGoldId = node.Id;
                }
            }

            AssertEqual(true, farGoldId != 0, scenario.Fail("expected a far gold node"));
            AssertEqual(true, nearGoldId != 0, scenario.Fail("expected a near gold node"));

            int[] primaryOnly = new[] { primaryWorkerId };
            scenario.Step(
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 96005, CommandType.GatherResource), new GatherResourceCommand(farGoldId, primaryOnly)),
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 96006, CommandType.GatherResource), new GatherResourceCommand(foodId, crowdWorkers)),
                new CommandEnvelope(new CommandHeader(state.Tick, 1, 96007, CommandType.NoOp), new NoOpCommand()));

            int startGold = state.PlayerStates.Players[0].Resources.Gold;
            int previousGold = startGold;
            bool sawPrimaryCarry = false;
            bool sawPrimaryReturnToAssigned = false;
            bool allowPrimaryRetargetAfterStall = false;
            bool sawPrimaryIntent = false;

            FixedVector2[] crowdMoveTargets =
            {
                FixedVector2.FromInts(28, 48),
                FixedVector2.FromInts(22, 44),
                FixedVector2.FromInts(30, 52),
                FixedVector2.FromInts(24, 50),
            };

            for (int tick = 0; tick < 1400; tick++)
            {
                if (tick % 120 == 0)
                {
                    FixedVector2 target = crowdMoveTargets[(tick / 120) % crowdMoveTargets.Length];
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(state.Tick, 0, unchecked((uint)(96100 + tick)), CommandType.MoveUnits), new MoveUnitsCommand(crowdWorkers, target)),
                        new CommandEnvelope(new CommandHeader(state.Tick, 1, unchecked((uint)(96101 + tick)), CommandType.NoOp), new NoOpCommand()));
                }
                else if (tick % 120 == 40)
                {
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(state.Tick, 0, unchecked((uint)(96200 + tick)), CommandType.GatherResource), new GatherResourceCommand(foodId, crowdWorkers)),
                        new CommandEnvelope(new CommandHeader(state.Tick, 1, unchecked((uint)(96201 + tick)), CommandType.NoOp), new NoOpCommand()));
                }
                else if (tick % 120 == 80)
                {
                    int gatherGold = ((tick / 120) % 2) == 0 ? nearGoldId : farGoldId;
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(state.Tick, 0, unchecked((uint)(96300 + tick)), CommandType.GatherResource), new GatherResourceCommand(gatherGold, new[] { crowdWorkers[0] })),
                        new CommandEnvelope(new CommandHeader(state.Tick, 1, unchecked((uint)(96301 + tick)), CommandType.NoOp), new NoOpCommand()));
                }
                else
                {
                    scenario.StepNoOps();
                }

                scenario.CaptureWorkerTrace(workers, 160);
                scenario.AssertCoreInvariants("dry-arabia-tc-front-flow");
                AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToResourceSlot, 900, scenario.Fail("workers stuck moving-to-resource"));
                AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToDropoffSlot, 900, scenario.Fail("workers stuck moving-to-dropoff"));
                AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToCommandMove, 900, scenario.Fail("workers stuck moving-to-command-move"));

                Unit primary = FindUnitById(state, primaryWorkerId);
                int blockedTicks = primary.LastMovedTick < 0 ? 0 : state.Tick - primary.LastMovedTick;
                if (blockedTicks >= GameData.NoProgressTimeoutTicks
                    && (primary.TaskPhase == WorkerTaskPhase.MovingToResourceSlot || primary.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot))
                {
                    allowPrimaryRetargetAfterStall = true;
                }

                if (primary.CurrentResourceAreaId != 0 || primary.CurrentResourceNodeId != 0 || primary.CarriedAmount > 0)
                {
                    sawPrimaryIntent = true;
                }

                if (primary.CarriedResourceType == ResourceType.Gold && primary.CarriedAmount > 0)
                {
                    sawPrimaryCarry = true;
                }

                int currentGold = state.PlayerStates.Players[0].Resources.Gold;
                if (currentGold > previousGold && sawPrimaryCarry)
                {
                    if (primary.CurrentResourceNodeId == farGoldId)
                    {
                        sawPrimaryReturnToAssigned = true;
                    }
                }
                previousGold = currentGold;

                if (primary.CurrentResourceNodeId != 0 && primary.CurrentResourceNodeId != farGoldId)
                {
                    ResourceNode assigned = FindResourceNodeById(state, farGoldId);
                    bool farStillValid = !assigned.IsDepleted && assigned.RemainingAmount > 0;
                    AssertEqual(
                        true,
                        !farStillValid || allowPrimaryRetargetAfterStall,
                        scenario.Fail("primary worker switched off assigned far gold without depletion or stale-slot fallback"));
                }

                for (int i = 0; i < workers.Length; i++)
                {
                    Unit unit = FindUnitById(state, workers[i]);
                    if (unit.TaskPhase == WorkerTaskPhase.Idle && !unit.HasMoveTarget)
                    {
                        AssertEqual(
                            true,
                            unit.ReservedInteractionKind != InteractionReservationKind.MoveDestination,
                            scenario.Fail("idle worker should not retain stale move destination reservation"));
                    }
                }
            }

            bool madeGoldProgress = state.PlayerStates.Players[0].Resources.Gold > startGold;
            AssertEqual(true, madeGoldProgress, scenario.Fail("expected bounded gold progress through tc-front flow"));
            AssertEqual(true, sawPrimaryIntent, scenario.Fail("primary worker should retain active gather intent under tc-front pressure"));
            AssertEqual(true, sawPrimaryCarry || madeGoldProgress, scenario.Fail("primary worker should carry gold or contribute to bounded progress"));
            AssertEqual(true, sawPrimaryReturnToAssigned || !sawPrimaryCarry, scenario.Fail("primary worker should return to assigned far gold after dropoff when still valid"));
        }

        private static void SimMatrixResourceStallScenarios()
        {
            RunFiveWorkerResourceSwitchScenario(31101);
            RunFiveWorkerResourceSwitchScenario(31111);
            RunFiveWorkerResourceSwitchScenario(31121);
        }

        private static void SimMatrixDropoffCongestionScenarios()
        {
            RunFiveWorkerMoveGatherHotspotScenario(31102);
            RunFiveWorkerMoveGatherHotspotScenario(31112);
            RunFiveWorkerMoveGatherHotspotScenario(31122);
        }

        private static void SimMatrixCommandReplacementScenarios()
        {
            RunFiveWorkerBuildGatherMoveGatherScenario(31103);
            RunFiveWorkerBuildGatherMoveGatherScenario(31113);
            RunFiveWorkerBuildGatherMoveGatherScenario(31123);
            RepeatedMoveReplacementNearTcHotspotStaysStable();
        }

        private static void SimMatrixSpawnOverlapScenarios()
        {
            PlayabilityInvariantTrainedVillagersGatherTrace();
            BlockedSpawnWaitsUntilSlotOpens();
            MultipleTrainedVillagersUseDifferentSpawnSlots();
        }

        private static void SimMatrixPressureOneTwentyScenarios()
        {
            PathQueryBudgetStaysBoundedUnderPressure();
            PressureWindowBudgetsStayBounded();
            SixPlayerMixedPopulationTrafficRemainsDeterministic();
            SixPlayerTwelveHundredActiveUnitPressureStaysBounded();
        }

    }
}
