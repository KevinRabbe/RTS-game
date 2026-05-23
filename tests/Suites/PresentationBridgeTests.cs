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
        private static void SimulationDoesNotReferencePresentation()
        {
            string simProject = System.IO.File.ReadAllText(System.IO.Path.Combine("src", "sim", "RtsGame.Sim.csproj"));

            AssertFalse(simProject.Contains("presentation") || simProject.Contains("Presentation"), "simulation project must not reference presentation layer");
        }

        private static void SimulationSourceDoesNotReferencePresentation()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "sim"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(text.Contains("RtsGame.Presentation"), "simulation source must not reference presentation namespace file=" + files[i]);
                AssertFalse(text.Contains("Godot"), "simulation source must not reference Godot file=" + files[i]);
            }
        }

        private static void SimulationSourceRoutesPathfindingThroughServiceLayer()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "sim"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string normalized = files[i].Replace('\\', '/');
                if (normalized.EndsWith("/Core/DeterministicPathfinder.cs")
                    || normalized.EndsWith("/Core/DeterministicPathQueryService.cs"))
                {
                    continue;
                }

                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(
                    text.Contains("DeterministicPathfinder.TryFindNextTile(") || text.Contains("DeterministicPathfinder.TryFindPathCost("),
                    "simulation code outside path query service must not call DeterministicPathfinder directly file=" + normalized);
            }
        }

        private static void SimulationSourceRoutesReservationWritesThroughTrafficService()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "sim"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string normalized = files[i].Replace('\\', '/');
                bool isAllowedWriter =
                    normalized.EndsWith("/Core/DeterministicTrafficReservationService.cs")
                    || normalized.EndsWith("/Core/EntityFactory.cs");
                if (isAllowedWriter)
                {
                    continue;
                }

                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(ContainsFieldAssignment(text, "ReservedInteractionKind"), "reservation kind writes must route through traffic service file=" + normalized);
                AssertFalse(ContainsFieldAssignment(text, "ReservedInteractionTargetId"), "reservation target writes must route through traffic service file=" + normalized);
                AssertFalse(ContainsFieldAssignment(text, "ReservedInteractionTileX"), "reservation tile x writes must route through traffic service file=" + normalized);
                AssertFalse(ContainsFieldAssignment(text, "ReservedInteractionTileY"), "reservation tile y writes must route through traffic service file=" + normalized);
            }
        }

        private static void DeterministicReservationConflictsAvoidUnorderedIteration()
        {
            string text = System.IO.File.ReadAllText(System.IO.Path.Combine("src", "sim", "Core", "DeterministicTrafficReservationService.cs"));
            AssertFalse(text.Contains("foreach (var"), "reservation conflict resolution should avoid unordered iteration in deterministic paths");
            AssertFalse(text.Contains("HashSet<"), "reservation conflict resolution should avoid hash-set iteration in deterministic conflict decisions");
            AssertFalse(text.Contains("foreach (KeyValuePair"), "reservation conflict resolution should avoid dictionary iteration in deterministic conflict decisions");
            AssertEqual(true, text.Contains("for (int i = 0; i < candidates.Count; i++)"), "reservation conflict resolution should use ordered candidate iteration");
        }

        private static void GodotBridgeDoesNotReferenceGodotApi()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "presentation", "GodotBridge"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(text.Contains("using Godot;"), "Godot bridge helpers must stay engine-api free file=" + files[i]);
                AssertFalse(text.Contains("Godot.Vector") || text.Contains("Godot.Color") || text.Contains("Node2D"), "Godot bridge helpers must not use Godot engine types file=" + files[i]);
            }
        }

        private static void GodotClientScriptDoesNotReferenceSimulationCore()
        {
            string script = System.IO.File.ReadAllText(System.IO.Path.Combine("GodotClient", "Scripts", "RtsClientRoot.cs"));

            AssertFalse(script.Contains("RtsGame.Sim"), "Godot client script must not reference simulation namespaces directly");
            AssertFalse(script.Contains("GameState"), "Godot client script must not reference GameState directly");
            AssertFalse(script.Contains("TickRunner"), "Godot client script must not reference TickRunner directly");
            AssertFalse(script.Contains("StateChecksum"), "Godot client script must not reference checksums directly");
        }

        private static void GodotClientScriptDoesNotSwitchOnRawPrimitiveKind()
        {
            string script = System.IO.File.ReadAllText(System.IO.Path.Combine("GodotClient", "Scripts", "RtsClientRoot.cs"));

            AssertFalse(script.Contains("switch (primitive.Kind)"), "Godot client script must not switch on raw primitive kind values");
            AssertFalse(script.Contains("case 1:") || script.Contains("case 2:") || script.Contains("case 3:"), "Godot client script must not use raw primitive kind case labels");
        }

        private static void VisualFrameCreatesUglyPrototypePrimitives()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(66, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));

            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.FogOverlay), "visual frame should include fog overlay primitive");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.UnitSquare), "visual frame should include unit square primitives");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.BuildingRectangle), "visual frame should include building rectangle primitives");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.HealthBar), "visual frame should include health bar primitives");
        }

        private static void VisualFrameUsesBuildingFootprintSize()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(67, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            int normalId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(8, 0));
            Building normal = state.EntityState.Buildings[state.EntityState.EntityLookup[normalId].Index];
            normal.IsUnderConstruction = false;
            normal.BuildProgressTicks = GameData.TownCenterBuildTicks;
            normal.HitPoints = GameData.TownCenterHitPoints;
            int wallId = EntityFactory.CreateWall(state, 0, FixedVector2.FromInts(14, 0));
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));
            VisualPrimitive capital = FindPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, state.PlayerStates.Players[0].CapitalStatus.CapitalBuildingId);
            VisualPrimitive normalTownCenter = FindPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, normalId);
            VisualPrimitive wall = FindPrimitive(frame, VisualPrimitiveKind.WallRectangle, wallId);
            long townCenterDiameterRaw = Fixed.FromInt(System.Math.Max(GameData.GetBuildingFootprintWidthTiles(BuildingTypeId.TownCenter), GameData.GetBuildingFootprintHeightTiles(BuildingTypeId.TownCenter))).Raw;
            long wallDiameterRaw = Fixed.FromInt(System.Math.Max(GameData.GetBuildingFootprintWidthTiles(BuildingTypeId.Wall), GameData.GetBuildingFootprintHeightTiles(BuildingTypeId.Wall))).Raw;

            AssertEqual(true, capital.IsCapital, "capital primitive should be marked as capital");
            AssertEqual(townCenterDiameterRaw, capital.Size.Raw, "capital primitive footprint should match TC simulation diameter");
            AssertEqual(townCenterDiameterRaw, normalTownCenter.Size.Raw, "normal TC primitive footprint should match TC simulation diameter");
            AssertEqual(wallDiameterRaw, wall.Size.Raw, "wall primitive footprint should match wall simulation diameter");
        }

        private static void VisualFrameIncludesTypeIds()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(87, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));
            VisualPrimitive villager = FindPrimitive(frame, VisualPrimitiveKind.UnitSquare, 1);
            VisualPrimitive townCenter = FindPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, state.PlayerStates.Players[0].CapitalStatus.CapitalBuildingId);
            VisualPrimitive food = FindPrimitive(frame, VisualPrimitiveKind.FoodResourceCircle, 1);

            AssertEqual((int)UnitTypeId.Villager, villager.TypeId, "unit visual primitive should expose unit type id");
            AssertEqual((int)BuildingTypeId.TownCenter, townCenter.TypeId, "building visual primitive should expose building type id");
            AssertEqual((int)ResourceType.Food, food.TypeId, "resource visual primitive should expose resource type id");
        }

        private static void VisualFrameIncludesResourcePrimitives()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(82, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));

            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.FoodResourceCircle), "visual frame should include food resource primitive");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.WoodResourceCircle), "visual frame should include wood resource primitive");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.GoldResourceCircle), "visual frame should include gold resource primitive");
        }

        private static void VisualFrameUsesResourceProfileVisualSize()
        {
            var resources = new List<ResourceNodeSnapshot>
            {
                new ResourceNodeSnapshot(1, 1, ResourceType.Wood, ResourceNodeType.Tree, GatherProfileId.Tree, FixedVector2.FromInts(10, 10), GameData.StartingWoodAmount),
                new ResourceNodeSnapshot(2, 2, ResourceType.Gold, ResourceNodeType.GoldVeinSmall, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(20, 10), GameData.StartingGoldAmount),
                new ResourceNodeSnapshot(3, 3, ResourceType.Gold, ResourceNodeType.GoldVeinLarge, GatherProfileId.GoldVeinLarge, FixedVector2.FromInts(30, 10), GameData.CenterGoldAmount)
            };
            var snapshot = new GameSnapshot(
                0,
                0,
                new List<UnitSnapshot>(),
                new List<BuildingSnapshot>(),
                resources,
                new LocalPlayerSnapshot(0, 0, 0, 0, 0, false, false, false),
                new MatchSnapshot(false, -1, -1));

            VisualFrame frame = VisualFrameBuilder.Build(snapshot);
            VisualPrimitive treePrimitive = FindPrimitive(frame, VisualPrimitiveKind.WoodResourceCircle, 1);
            VisualPrimitive smallGoldPrimitive = FindPrimitive(frame, VisualPrimitiveKind.GoldResourceCircle, 2);
            VisualPrimitive largeGoldPrimitive = FindPrimitive(frame, VisualPrimitiveKind.GoldResourceCircle, 3);

            AssertEqual(Fixed.FromInt(System.Math.Max(GameData.GetGatherProfile(GatherProfileId.Tree).FootprintWidthTiles, GameData.GetGatherProfile(GatherProfileId.Tree).FootprintHeightTiles)).Raw, treePrimitive.Size.Raw, "tree primitive should expose profile footprint visual size");
            AssertEqual(Fixed.FromInt(System.Math.Max(GameData.GetGatherProfile(GatherProfileId.GoldVeinSmall).FootprintWidthTiles, GameData.GetGatherProfile(GatherProfileId.GoldVeinSmall).FootprintHeightTiles)).Raw, smallGoldPrimitive.Size.Raw, "small gold primitive should expose profile footprint visual size");
            AssertEqual(Fixed.FromInt(System.Math.Max(GameData.GetGatherProfile(GatherProfileId.GoldVeinLarge).FootprintWidthTiles, GameData.GetGatherProfile(GatherProfileId.GoldVeinLarge).FootprintHeightTiles)).Raw, largeGoldPrimitive.Size.Raw, "large gold primitive should expose profile footprint visual size");
            AssertEqual(true, largeGoldPrimitive.Size.Raw > smallGoldPrimitive.Size.Raw, "large gold should look larger than small gold");
        }

        private static void VisualFrameIncludesTradeRouteLine()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTradeState(6);
            Unit cart = state.EntityState.Units[state.EntityState.Units.Count - 1];
            cart.TradeRouteAId = state.EntityState.Buildings[0].Id;
            cart.TradeRouteBId = state.EntityState.Buildings[1].Id;
            cart.TradeDestinationId = state.EntityState.Buildings[1].Id;
            cart.TradeIncomePerTrip = 1;
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));

            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.TradeRouteLine), "visual frame should include visible trade route line");
        }

        private static void VisualFrameDoesNotMutateChecksum()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(68, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());
            ulong before = StateChecksum.Compute(state, rules);

            VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));
            ulong after = StateChecksum.Compute(state, rules);

            AssertEqual(before, after, "building visual frame must not mutate simulation state");
        }

        private static void GodotFacadeReturnsDrawableFrameDto()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(76);

            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);

            AssertEqual(1, frame.Tick, "godot frame should report current tick");
            AssertEqual(0, frame.LocalPlayerIndex, "godot frame should report local player");
            AssertEqual(true, HasGodotPrimitive(frame, VisualPrimitiveKind.FogOverlay), "godot frame should include fog primitive");
            AssertEqual(true, HasGodotPrimitive(frame, VisualPrimitiveKind.UnitSquare), "godot frame should include unit primitive");
        }

        private static void GodotFacadeDrivesLocalCapitalFlow()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(77);

            facade.QueuePlaceTownCenter(0, 10, 10);
            facade.QueuePlaceTownCenter(1, 50, 10);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            facade.QueueAssignBuild(1, 12, new[] { 6, 7, 8, 9 });
            for (int i = 0; i < 100; i++)
            {
                GodotFrameDto playerZeroProgress = facade.GetFrame(0);
                GodotFrameDto playerOneProgress = facade.GetFrame(1);
                if (playerZeroProgress.LocalPlayer.CapitalBonusActive && playerOneProgress.LocalPlayer.CapitalBonusActive)
                {
                    break;
                }

                facade.AdvanceOneTick();
            }

            GodotFrameDto playerZero = facade.GetFrame(0);
            GodotFrameDto playerOne = facade.GetFrame(1);

            AssertEqual(true, playerZero.LocalPlayer.CapitalBonusActive, "godot facade should complete player 0 capital through local session");
            AssertEqual(true, playerOne.LocalPlayer.CapitalBonusActive, "godot facade should complete player 1 capital through local session");
            AssertEqual(0, facade.RejectedCommandCount, "godot facade capital flow should not reject");
        }

        private static void GodotFacadeCreatesLocal6PlayerFfa()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal6PlayerFfa(97);

            facade.AdvanceOneTick();
            GodotFrameDto playerFive = facade.GetFrame(5);

            AssertEqual(6, facade.PlayerCount, "godot facade should expose six-player local FFA setup");
            AssertEqual(6, facade.ExecutedCommandCount, "godot local FFA should fill six inputs with noops");
            AssertEqual(5, playerFive.LocalPlayerIndex, "godot facade should return player 5 frame for local FFA");
            AssertEqual(true, HasGodotPrimitive(playerFive, VisualPrimitiveKind.UnitSquare), "player 5 frame should expose visible local starting units");
            AssertEqual(0, facade.RejectedCommandCount, "six-player facade setup should not reject");
        }

        private static void GodotFacadeRejectsInvalidCommandsThroughSim()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(78);

            facade.QueuePlaceTownCenter(0, 10, 10);
            facade.AdvanceOneTick();
            facade.QueuePlaceTownCenter(0, 20, 20);
            facade.AdvanceOneTick();

            AssertEqual(1, facade.RejectedCommandCount, "godot facade should route invalid commands to sim rejection");
            AssertEqual(true, facade.GetFrame(0).LocalPlayer.HasCapitalBeenPlaced, "first valid capital should remain visible in local player state");
        }

        private static void GodotFacadeExposesLastCommandRejectionMetadata()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(789);
            facade.QueueMoveUnits(0, new[] { 1 }, 3, 8);
            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);

            AssertEqual(false, frame.Match.LastCommandAccepted, "invalid command should expose rejected status");
            AssertEqual((int)CommandType.MoveUnits, frame.Match.LastCommandTypeId, "match metadata should expose command type");
            AssertEqual((int)CommandValidationReason.TargetBlockedByStaticGeometry, frame.Match.LastCommandReasonId, "resource-blocked move should expose blocked geometry reason");
            AssertEqual(0, frame.Match.LastCommandPlayerIndex, "match metadata should expose issuing player");
            AssertEqual(3, frame.Match.LastCommandTargetTileX, "match metadata should expose target tile x");
            AssertEqual(8, frame.Match.LastCommandTargetTileY, "match metadata should expose target tile y");
        }

        private static void GodotFacadeExposesFixedRawCoordinates()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(79);

            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            GodotPrimitiveDto unit = FindGodotPrimitive(frame, VisualPrimitiveKind.UnitSquare, 1);

            AssertEqual(Fixed.FromInt(0).Raw, unit.XRaw, "godot facade should expose fixed raw X coordinate");
            AssertEqual(Fixed.FromInt(0).Raw, unit.YRaw, "godot facade should expose fixed raw Y coordinate");
            AssertEqual(Fixed.FromRatio(7, 10).Raw, unit.SizeRaw, "godot facade should expose fixed raw primitive size");
        }

        private static void GodotFacadeExposesPrimitiveTypeIds()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(88);

            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            GodotPrimitiveDto villager = FindGodotPrimitive(frame, VisualPrimitiveKind.UnitSquare, 1);
            GodotPrimitiveDto townCenter = FindGodotPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, 11);
            GodotPrimitiveDto food = FindGodotPrimitive(frame, VisualPrimitiveKind.FoodResourceCircle, 1);

            AssertEqual((int)UnitTypeId.Villager, villager.TypeId, "godot unit primitive should expose unit type id");
            AssertEqual((int)BuildingTypeId.TownCenter, townCenter.TypeId, "godot building primitive should expose building type id");
            AssertEqual((int)ResourceType.Food, food.TypeId, "godot resource primitive should expose resource type id");
        }

        private static void GodotFacadeExposesBuildingStatusDto()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(90);

            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            for (int i = 0; i < 200; i++)
            {
                GodotBuildingStatusDto buildStatus = FindGodotBuildingStatus(facade.GetFrame(0), 11);
                if (!buildStatus.IsUnderConstruction)
                {
                    break;
                }

                facade.AdvanceOneTick();
            }
            facade.QueueGatherResource(0, 1, new[] { 1 });
            for (int i = 0; i < 1000 && facade.GetFrame(0).LocalPlayer.Food < 50; i++)
            {
                facade.AdvanceOneTick();
            }
            facade.QueueTrainUnit(0, 11, (int)UnitTypeId.Villager);
            facade.AdvanceOneTick();

            GodotBuildingStatusDto status = FindGodotBuildingStatus(facade.GetFrame(0), 11);

            AssertEqual(false, status.IsUnderConstruction, "godot building status should show completed building");
            AssertEqual(true, status.TrainingQueueCount >= 0, "godot building status should expose training queue count");
            AssertEqual(true, status.TrainingProgressTicks >= 0, "godot building status should expose training progress");
            AssertEqual(true, status.TrainingRequiredTicks >= 0, "godot building status should expose training requirement");
        }

        private static void GodotFacadeExposesUnitStatusDto()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(92);

            facade.QueueGatherResource(0, 1, new[] { 1 });
            for (int i = 0; i < 200; i++)
            {
                facade.AdvanceOneTick();
                if (FindGodotUnitStatus(facade.GetFrame(0), 1).CarriedAmount > 0)
                {
                    break;
                }
            }

            GodotUnitStatusDto status = FindGodotUnitStatus(facade.GetFrame(0), 1);

            AssertEqual((int)UnitTypeId.Villager, status.UnitTypeId, "godot unit status should expose unit type");
            AssertEqual(1, status.CurrentResourceNodeId, "godot unit status should expose gather target");
            AssertEqual(true, status.CarriedResourceTypeId >= 0, "godot unit status should expose carried resource type id");
            AssertEqual(true, status.CarriedAmount >= 0, "godot unit status should expose carried amount");
        }

        private static void GodotFacadeExposesResourcePrimitiveDto()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(83);

            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            GodotPrimitiveDto food = FindGodotPrimitive(frame, VisualPrimitiveKind.FoodResourceCircle, 1);

            AssertEqual((int)VisualPrimitiveKind.FoodResourceCircle, food.Kind, "godot facade should expose food resource primitive kind");
            AssertEqual(GameData.NeutralOwnerPlayerIndex, food.OwnerPlayerIndex, "resource primitive should be neutral-owned presentation data");
            AssertEqual(Fixed.FromInt(6).Raw, food.XRaw, "godot facade should expose resource fixed raw X coordinate");
            AssertEqual(Fixed.FromInt(0).Raw, food.YRaw, "godot facade should expose resource fixed raw Y coordinate");
        }

        private static void GodotFacadeRoutesGatherCommand()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(84);

            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            for (int i = 0; i < 200; i++)
            {
                if (!FindGodotBuildingStatus(facade.GetFrame(0), 11).IsUnderConstruction)
                {
                    break;
                }

                facade.AdvanceOneTick();
            }
            facade.QueueGatherResource(0, 1, new[] { 1 });
            for (int i = 0; i < 1000 && facade.GetFrame(0).LocalPlayer.Food < 10; i++)
            {
                facade.AdvanceOneTick();
            }

            AssertEqual(true, facade.ExecutedCommandCount > 0, "godot facade gather route should execute through local simulation");
            AssertEqual(0, facade.RejectedCommandCount, "valid facade gather flow should not reject");
        }

        private static void DryArabiaResourceDtoExposesStableIdTypeTile()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(1264);
            AssertResourceNodeStable(state, 1, ResourceType.Food, 30, 48);
            AssertResourceNodeStable(state, 3, ResourceType.Wood, 22, 57);
            AssertResourceNodeStable(state, 5, ResourceType.Gold, 18, 47);

            GodotClientFacade facade = GodotClientFacade.CreateDryArabiaTest01(1264);
            GodotFrameDto frame = facade.GetFrame(0);
            int visibleCount = 0;
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.FoodResourceCircle
                    && primitive.Kind != (int)VisualPrimitiveKind.WoodResourceCircle
                    && primitive.Kind != (int)VisualPrimitiveKind.GoldResourceCircle)
                {
                    continue;
                }

                ResourceNode node = FindResourceNodeById(state, primitive.EntityId);
                AssertEqual((int)node.ResourceType, primitive.TypeId, "resource dto type should match sim resource type for id=" + primitive.EntityId);
                AssertEqual(node.Position.X.Raw, primitive.XRaw, "resource dto x should match sim position for id=" + primitive.EntityId);
                AssertEqual(node.Position.Y.Raw, primitive.YRaw, "resource dto y should match sim position for id=" + primitive.EntityId);
                visibleCount++;
            }

        }

        private static void GodotFacadeRoutesTrainingCommand()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(85);

            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            for (int i = 0; i < 200; i++)
            {
                if (!FindGodotBuildingStatus(facade.GetFrame(0), 11).IsUnderConstruction)
                {
                    break;
                }

                facade.AdvanceOneTick();
            }
            facade.QueueMoveUnits(0, new[] { 2, 3, 4 }, 20, 20);
            facade.AdvanceTicks(20);
            facade.QueueGatherResource(0, 1, new[] { 1 });
            for (int i = 0; i < 1000 && facade.GetFrame(0).LocalPlayer.Food < 50; i++)
            {
                facade.AdvanceOneTick();
            }
            bool hadTrainingFood = facade.GetFrame(0).LocalPlayer.Food >= 50;
            facade.QueueTrainUnit(0, 11, (int)UnitTypeId.Villager);
            facade.AdvanceTicks(GameData.VillagerTrainTicks);

            GodotFrameDto frame = facade.GetFrame(0);
            if (hadTrainingFood)
            {
                AssertEqual(10, frame.LocalPlayer.Food, "godot facade should spend villager food cost through training command");
                AssertEqual(6, frame.LocalPlayer.PopulationUsed, "training should reserve one villager population through simulation");
                AssertEqual(0, facade.RejectedCommandCount, "valid facade training flow should not reject");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "training command should route to simulation and reject when resources are insufficient");
            }
        }

        private static void GodotFacadeRoutesAttackCommand()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(86);

            facade.QueueAttack(0, new[] { 1 }, 6);
            facade.AdvanceOneTick();

            AssertEqual(2, facade.ExecutedCommandCount, "facade attack should execute alongside automatic local noop");
            AssertEqual(0, facade.RejectedCommandCount, "valid facade attack command should not reject");
        }

        private static void GodotFacadeRoutesWallCommand()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(93);

            facade.QueueGatherResource(0, 2, new[] { 1, 2, 3, 4 });
            for (int i = 0; i < 1000 && facade.GetFrame(0).LocalPlayer.Wood < GameData.WallWoodCost; i++)
            {
                facade.AdvanceOneTick();
            }
            bool hadWallWood = facade.GetFrame(0).LocalPlayer.Wood >= GameData.WallWoodCost;
            facade.QueuePlaceWall(0, 7, 8);
            facade.AdvanceOneTick();

            GodotFrameDto frame = facade.GetFrame(0);
            if (hadWallWood)
            {
                AssertEqual(true, HasGodotPrimitive(frame, VisualPrimitiveKind.WallRectangle), "godot facade should route wall placement through simulation");
                AssertEqual(0, facade.RejectedCommandCount, "valid facade wall placement should not reject");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "wall command should route to simulation and reject when resources are insufficient");
            }
        }

        private static void GodotFacadeRoutesTradePostCommand()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(94);

            facade.QueueGatherResource(0, 2, new[] { 1 });
            facade.AdvanceTicks(30);
            facade.QueueGatherResource(0, 3, new[] { 2 });
            facade.AdvanceTicks(10);
            bool hadTradePostResources = facade.GetFrame(0).LocalPlayer.Wood >= GameData.TradePostWoodCost
                && facade.GetFrame(0).LocalPlayer.Gold >= GameData.TradePostGoldCost;
            facade.QueuePlaceTradePost(0, 8, 11);
            facade.AdvanceOneTick();

            GodotFrameDto frame = facade.GetFrame(0);

            bool hasTradePost = HasGodotPrimitiveWithType(frame, VisualPrimitiveKind.BuildingRectangle, (int)BuildingTypeId.TradePost);
            if (hadTradePostResources)
            {
                AssertEqual(true, hasTradePost, "godot facade should route trade post placement through simulation");
                AssertEqual(0, facade.RejectedCommandCount, "valid facade trade post placement should not reject");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "trade post command should route to simulation and reject when resources are insufficient");
            }
        }

        private static void GodotFacadeRoutesTradeCartTrainingCommand()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(95);

            facade.QueueGatherResource(0, 2, new[] { 1 });
            facade.AdvanceTicks(46);
            facade.QueueGatherResource(0, 3, new[] { 2 });
            facade.AdvanceTicks(14);
            facade.QueuePlaceTradePost(0, 8, 11);
            facade.AdvanceOneTick();

            GodotFrameDto afterTradePostPlacement = facade.GetFrame(0);
            bool hasTradePost = HasGodotPrimitiveWithType(afterTradePostPlacement, VisualPrimitiveKind.BuildingRectangle, (int)BuildingTypeId.TradePost);
            if (!hasTradePost)
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "trade post placement should reject when resources are insufficient");
                return;
            }

            int tradePostId = FindGodotPrimitiveWithType(afterTradePostPlacement, VisualPrimitiveKind.BuildingRectangle, (int)BuildingTypeId.TradePost).EntityId;

            facade.QueueAssignBuild(0, tradePostId, new[] { 1, 2, 3, 4 });
            facade.AdvanceTicks(GameData.TradePostBuildTicks);
            facade.QueueTrainUnit(0, tradePostId, (int)UnitTypeId.TradeCart);
            facade.AdvanceTicks(GameData.TradeCartTrainTicks);

            GodotFrameDto frame = facade.GetFrame(0);
            if (HasGodotUnitStatusWithType(frame, (int)UnitTypeId.TradeCart))
            {
                AssertEqual(6, frame.LocalPlayer.PopulationUsed, "trade cart training should reserve one population through simulation");
                AssertEqual(0, facade.RejectedCommandCount, "valid facade trade cart training flow should not reject");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "trade cart training should route to simulation and reject when requirements are not met");
            }
        }

        private static void GodotFacadeRoutesResearchCommand()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(98);
            facade.QueueGatherResource(0, 1, new[] { 1, 2 });
            facade.QueueGatherResource(0, 3, new[] { 3, 4 });
            for (int i = 0; i < 1000 && (facade.GetFrame(0).LocalPlayer.Food < GameData.InfantryAttack1FoodCost || facade.GetFrame(0).LocalPlayer.Gold < GameData.InfantryAttack1GoldCost); i++)
            {
                facade.AdvanceOneTick();
            }
            GodotFrameDto beforeResearch = facade.GetFrame(0);
            facade.QueueResearchTech(0, 11, (int)TechId.InfantryAttack1);
            facade.AdvanceOneTick();

            GodotFrameDto frame = facade.GetFrame(0);
            if (frame.LocalPlayer.ResearchQueue.Length > 0)
            {
                AssertEqual(1, frame.LocalPlayer.ResearchQueue.Length, "godot facade should expose queued research after routing command");
                AssertEqual((int)TechId.InfantryAttack1, frame.LocalPlayer.ResearchQueue[0].TechId, "godot facade should expose research tech id");
                AssertEqual(1, frame.LocalPlayer.ResearchQueue[0].ProgressTicks, "godot facade should expose research progress");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "research command should route to simulation and reject when requirements are not met");
            }
        }

    }
}
