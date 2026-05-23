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
        private static void ClientIntentMapsMovementCommand()
        {
            CommandEnvelope envelope = ClientCommandMapper.ToCommandEnvelope(
                ClientCommandIntent.MoveUnits(new[] { 1, 2 }, FixedVector2.FromInts(5, 6)),
                12,
                0,
                7);

            AssertEqual(12, envelope.Header.Tick, "client intent should copy command tick");
            AssertEqual(0, envelope.Header.PlayerIndex, "client intent should copy player index");
            AssertEqual(7u, envelope.Header.Sequence, "client intent should copy sequence");
            AssertEqual(CommandType.MoveUnits, envelope.Header.CommandType, "client movement intent should map to movement command");
            AssertEqual(CommandType.MoveUnits, envelope.Payload.Type, "payload should be movement command");
        }

        private static void ClientIntentSortsSelectedUnitIds()
        {
            ClientCommandIntent move = ClientCommandIntent.MoveUnits(new[] { 4, 1, 3, 2 }, FixedVector2.FromInts(5, 6));
            ClientCommandIntent gather = ClientCommandIntent.GatherResource(10, new[] { 9, 7, 8 });
            ClientCommandIntent build = ClientCommandIntent.AssignBuild(11, new[] { 6, 5, 4 });
            ClientCommandIntent attack = ClientCommandIntent.Attack(new[] { 14, 12, 13 }, 99);

            AssertEqual(1, move.UnitIds[0], "move selected unit ids should be sorted before command mapping");
            AssertEqual(2, move.UnitIds[1], "move selected unit ids should be sorted before command mapping");
            AssertEqual(3, move.UnitIds[2], "move selected unit ids should be sorted before command mapping");
            AssertEqual(4, move.UnitIds[3], "move selected unit ids should be sorted before command mapping");
            AssertEqual(7, gather.UnitIds[0], "gather selected unit ids should be sorted before command mapping");
            AssertEqual(8, gather.UnitIds[1], "gather selected unit ids should be sorted before command mapping");
            AssertEqual(9, gather.UnitIds[2], "gather selected unit ids should be sorted before command mapping");
            AssertEqual(4, build.UnitIds[0], "build selected unit ids should be sorted before command mapping");
            AssertEqual(5, build.UnitIds[1], "build selected unit ids should be sorted before command mapping");
            AssertEqual(6, build.UnitIds[2], "build selected unit ids should be sorted before command mapping");
            AssertEqual(12, attack.UnitIds[0], "attack selected unit ids should be sorted before command mapping");
            AssertEqual(13, attack.UnitIds[1], "attack selected unit ids should be sorted before command mapping");
            AssertEqual(14, attack.UnitIds[2], "attack selected unit ids should be sorted before command mapping");
        }

        private static void ClientIntentMapsResearchCommand()
        {
            CommandEnvelope envelope = ClientCommandMapper.ToCommandEnvelope(
                ClientCommandIntent.ResearchTech(11, TechId.InfantryAttack1),
                13,
                0,
                8);

            AssertEqual(13, envelope.Header.Tick, "client research intent should copy command tick");
            AssertEqual(0, envelope.Header.PlayerIndex, "client research intent should copy player index");
            AssertEqual(8u, envelope.Header.Sequence, "client research intent should copy sequence");
            AssertEqual(CommandType.ResearchTech, envelope.Header.CommandType, "client research intent should map to research command");
            AssertEqual(CommandType.ResearchTech, envelope.Payload.Type, "payload should be research command");
        }

        private static void ClientIntentMapsLocal1v1CommandFlow()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(69, 2);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();

            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)), 0, 0, 0));
            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(50, 10)), 0, 1, 0));
            runner.AdvanceOneTick(state, rules, buffer);

            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.AssignBuild(11, new[] { 1, 2, 3, 4 }), 1, 0, 1));
            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.AssignBuild(12, new[] { 6, 7, 8, 9 }), 1, 1, 1));
            runner.AdvanceOneTick(state, rules, buffer);

            int tick = 2;
            uint sequence = 2;
            while (tick < 100
                && (!state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive
                    || !state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive))
            {
                buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.NoOp(), tick, 0, sequence));
                buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.NoOp(), tick, 1, sequence));
                runner.AdvanceOneTick(state, rules, buffer);
                tick++;
                sequence++;
            }

            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.MoveUnits(new[] { 5 }, FixedVector2.FromInts(12, 12)), tick, 0, sequence));
            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.NoOp(), tick, 1, sequence));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "client flow should complete player 0 capital through normal sim commands");
            AssertEqual(true, state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "client flow should complete player 1 capital through normal sim commands");
            AssertEqual(true, state.EntityState.Units[4].HasMoveTarget, "client movement intent should assign normal sim move target");
            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "client intent flow should not create invalid commands");
        }

        private static void ClientCommandMappingDoesNotMutateChecksum()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(70, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());
            ulong before = StateChecksum.Compute(state, rules);

            ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)), state.Tick, 0, 0);
            ulong after = StateChecksum.Compute(state, rules);

            AssertEqual(before, after, "mapping client intent should not mutate simulation state");
        }

        private static void LocalPlaySessionAdvancesWithAutomaticNoOps()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(71);

            session.QueueIntent(0, ClientCommandIntent.MoveUnits(new[] { 5 }, FixedVector2.FromInts(2, 2)));
            session.AdvanceOneTick();

            AssertEqual(1, session.CurrentTick, "local session should advance one tick");
            AssertEqual(2, session.ExecutedCommandCount, "local session should fill missing player input with noop");
            AssertEqual(0, session.RejectedCommandCount, "automatic noop fill should not reject");
        }

        private static void LocalPlaySessionQueuesWithoutMutatingBeforeTick()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(72);

            session.QueueIntent(0, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)));
            GameSnapshot snapshot = session.GetSnapshot(0);

            AssertEqual(0, session.CurrentTick, "queuing should not advance simulation");
            AssertEqual(false, snapshot.LocalPlayer.HasCapitalBeenPlaced, "queued command should not mutate player capital state before tick execution");
            AssertEqual(0, session.ExecutedCommandCount, "queued command should not execute before tick");
        }

        private static void LocalPlaySessionCompletes1v1Capitals()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(73);

            session.QueueIntent(0, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)));
            session.QueueIntent(1, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(50, 10)));
            session.AdvanceOneTick();
            session.QueueIntent(0, ClientCommandIntent.AssignBuild(11, new[] { 1, 2, 3, 4 }));
            session.QueueIntent(1, ClientCommandIntent.AssignBuild(12, new[] { 6, 7, 8, 9 }));
            session.AdvanceOneTick();
            for (int i = 0; i < 100; i++)
            {
                GameSnapshot playerZeroProgress = session.GetSnapshot(0);
                GameSnapshot playerOneProgress = session.GetSnapshot(1);
                if (playerZeroProgress.LocalPlayer.CapitalBonusActive && playerOneProgress.LocalPlayer.CapitalBonusActive)
                {
                    break;
                }

                session.AdvanceOneTick();
            }

            GameSnapshot playerZero = session.GetSnapshot(0);
            GameSnapshot playerOne = session.GetSnapshot(1);

            AssertEqual(true, playerZero.LocalPlayer.CapitalBonusActive, "player 0 capital should complete through local session commands");
            AssertEqual(true, playerOne.LocalPlayer.CapitalBonusActive, "player 1 capital should complete through local session commands");
            AssertEqual(0, session.RejectedCommandCount, "local 1v1 capital flow should not reject");
        }

        private static void LocalPlaySessionExposesVisualFrame()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(74);

            session.AdvanceOneTick();
            VisualFrame frame = session.GetVisualFrame(0);

            AssertEqual(1, frame.Tick, "visual frame should match local session tick");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.FogOverlay), "local visual frame should include fog primitive");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.UnitSquare), "local visual frame should include visible local units");
        }

        private static void LocalPlaySessionRejectsInvalidIntentThroughSim()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(75);

            session.QueueIntent(0, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)));
            session.AdvanceOneTick();
            session.QueueIntent(0, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(20, 20)));
            session.AdvanceOneTick();

            AssertEqual(1, session.RejectedCommandCount, "invalid local intent should be rejected by simulation validation");
            AssertEqual(true, session.GetSnapshot(0).LocalPlayer.HasCapitalBeenPlaced, "valid first capital should remain placed");
        }

        private static void LocalPlaySessionCreates6PlayerFfa()
        {
            LocalPlaySession session = LocalPlaySession.Create6PlayerFfa(96);

            session.AdvanceOneTick();

            AssertEqual(6, session.PlayerCount, "local FFA session should expose all six players");
            AssertEqual(1, session.CurrentTick, "local FFA session should advance normally");
            AssertEqual(6, session.ExecutedCommandCount, "local FFA session should fill all six player inputs with deterministic noops");
            AssertEqual(0, session.RejectedCommandCount, "automatic six-player noops should not reject");
            AssertEqual(0, session.GetSnapshot(0).LocalPlayerIndex, "local FFA snapshot should support player 0 view");
            AssertEqual(5, session.GetSnapshot(5).LocalPlayerIndex, "local FFA snapshot should support player 5 view");
        }

        private static void LocalPlaySessionCreatesDryArabiaTestMap()
        {
            LocalPlaySession session = LocalPlaySession.CreateDryArabiaTest01(97);

            session.AdvanceOneTick();
            GameSnapshot snapshot = session.GetSnapshot(0);

            AssertEqual(2, session.PlayerCount, "dry arabia local session should be a 1v1 map");
            AssertEqual(DryArabiaTest01MapDefinition.MapName, session.MapName, "dry arabia local session should expose map name");
            AssertEqual(true, HasResource(snapshot, ResourceType.Food), "dry arabia local session should expose visible local food");
            AssertEqual(true, HasResource(snapshot, ResourceType.Wood), "dry arabia local session should expose visible local wood");
            AssertEqual(true, HasResource(snapshot, ResourceType.Gold), "dry arabia local session should expose visible local gold");

            GodotClientFacade facade = GodotClientFacade.CreateDryArabiaTest01(97);
            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            string hud = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(DryArabiaTest01MapDefinition.MapName, facade.MapName, "godot facade should expose dry arabia map name");
            AssertEqual(DryArabiaTest01MapDefinition.MapName, frame.MapName, "godot frame should expose dry arabia map name");
            AssertEqual(true, hud.Contains("Map " + DryArabiaTest01MapDefinition.MapName), "hud should include dry arabia map name");
        }

        private static void LocalPlaySessionCreatesCombatTestMap()
        {
            LocalPlaySession session = LocalPlaySession.CreateCombatTest01(98);
            GameState setup = GameInitializer.CreateCombatTest01(98);

            session.AdvanceOneTick();
            GameSnapshot localSnapshot = session.GetSnapshot(0);

            AssertEqual(2, session.PlayerCount, "combat test local session should be a 1v1 map");
            AssertEqual(CombatTest01MapDefinition.MapName, session.MapName, "combat test local session should expose map name");
            AssertEqual(true, CountOwnedUnitType(setup, 0, UnitTypeId.Infantry) >= 8, "combat test setup should include local infantry group");
            AssertEqual(true, CountOwnedUnitType(setup, 1, UnitTypeId.Infantry) >= 8, "combat test setup should include enemy infantry group");
            AssertEqual(true, HasUnitType(localSnapshot, UnitTypeId.Infantry), "combat test local player should have infantry units");
            AssertEqual(true, HasUnitType(localSnapshot, UnitTypeId.Scout), "combat test local player should have scout unit");

            GodotClientFacade facade = GodotClientFacade.CreateCombatTest01(98);
            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            string hud = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(CombatTest01MapDefinition.MapName, facade.MapName, "godot facade should expose combat test map name");
            AssertEqual(CombatTest01MapDefinition.MapName, frame.MapName, "godot frame should expose combat test map name");
            AssertEqual(true, hud.Contains("Map " + CombatTest01MapDefinition.MapName), "hud should include combat test map name");
        }

        private static void CombatTestScenarioSupportsAttackIntentThroughFacade()
        {
            GodotClientFacade facade = GodotClientFacade.CreateCombatTest01(99);
            int attackerId = 5;
            int targetId = 6;

            facade.QueueAttack(0, new[] { attackerId }, targetId);
            facade.AdvanceOneTick();
            GodotFrameDto after = facade.GetFrame(0);
            AssertEqual(true, after.Match.LastCommandAccepted, "combat test scenario attack command should be accepted through normal facade path");
            AssertEqual((int)CommandType.Attack, after.Match.LastCommandTypeId, "combat test scenario should execute attack command type");
            AssertEqual((int)CommandValidationReason.Accepted, after.Match.LastCommandReasonId, "combat test scenario attack should report accepted reason");
        }

        private static void GodotInteractionRouterPrioritizesAttack()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.FoodResourceCircle, 10, GameData.NeutralOwnerPlayerIndex, 5, 5),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 20, 1, 5, 5)
            });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(GodotInteractionIntentKind.Attack, intent.Kind, "enemy target should take priority over gather when primitives overlap");
            AssertEqual(20, intent.TargetEntityId, "attack intent should expose target entity id");
            AssertEqual(0, intent.ResourceNodeId, "attack intent should not expose a resource id");
        }

        private static bool HasUnitType(GameSnapshot snapshot, UnitTypeId unitTypeId)
        {
            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                if (snapshot.Units[i].OwnerPlayerIndex == snapshot.LocalPlayerIndex
                    && snapshot.Units[i].UnitTypeId == unitTypeId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountOwnedUnitType(GameState state, int ownerPlayerIndex, UnitTypeId unitTypeId)
        {
            int count = 0;
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (!unit.IsDead && unit.OwnerPlayerIndex == ownerPlayerIndex && unit.UnitTypeId == unitTypeId)
                {
                    count++;
                }
            }

            return count;
        }

        private static void GodotInteractionRouterRoutesBuildAssignment()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 21, 0, 5, 5)
                },
                new[]
                {
                    new GodotBuildingStatusDto(21, (int)BuildingTypeId.TownCenter, true, 1, GameData.TownCenterBuildTicks, 0, 0, 0, 0)
                });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(GodotInteractionIntentKind.AssignBuild, intent.Kind, "own under-construction building should route to build assignment");
            AssertEqual(21, intent.TargetEntityId, "build assignment intent should expose target building id");
        }

        private static void GodotInteractionRouterRoutesBuildAssignmentWithExpandedBounds()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 121, 0, 5, 5)
                },
                new[]
                {
                    new GodotBuildingStatusDto(121, (int)BuildingTypeId.TownCenter, true, 1, GameData.TownCenterBuildTicks, 0, 0, 0, 0)
                });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(6).Raw, Fixed.FromInt(6).Raw);

            AssertEqual(GodotInteractionIntentKind.AssignBuild, intent.Kind, "expanded interaction bounds should route build assignment near foundations");
            AssertEqual(121, intent.TargetEntityId, "expanded interaction bounds should preserve target building id");
        }

        private static void GodotInteractionRouterIgnoresCompletedBuildTarget()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 22, 0, 5, 5)
                },
                new[]
                {
                    new GodotBuildingStatusDto(22, (int)BuildingTypeId.TownCenter, false, GameData.TownCenterBuildTicks, GameData.TownCenterBuildTicks, 0, 0, 0, 0)
                });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(GodotInteractionIntentKind.None, intent.Kind, "completed own building should suppress ground move fallback");
            AssertEqual(22, intent.TargetEntityId, "completed own building should remain visible as the resolved target");
        }

        private static void GodotInteractionRouterRoutesResources()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 30, GameData.NeutralOwnerPlayerIndex, 6, 7)
            });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(6).Raw, Fixed.FromInt(7).Raw);

            AssertEqual(GodotInteractionIntentKind.GatherResource, intent.Kind, "resource target should route to gather");
            AssertEqual(30, intent.ResourceNodeId, "gather intent should expose resource node id");
            AssertEqual(0, intent.TargetEntityId, "gather intent should not expose an attack target id");
        }

        private static void GodotInteractionRouterRoutesResourceOverFriendlyCompletedBuilding()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 22, 0, 5, 5),
                    CreateGodotPrimitive(VisualPrimitiveKind.WoodResourceCircle, 31, GameData.NeutralOwnerPlayerIndex, 5, 5)
                },
                new[]
                {
                    new GodotBuildingStatusDto(22, (int)BuildingTypeId.TownCenter, false, GameData.TownCenterBuildTicks, GameData.TownCenterBuildTicks, 0, 0, 0, 0)
                });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(GodotInteractionIntentKind.GatherResource, intent.Kind, "resource target should beat friendly completed building suppression");
            AssertEqual(31, intent.ResourceNodeId, "overlapping resource should remain gatherable");
        }

        private static void GodotInteractionRouterPicksNearestOverlappingResource()
        {
            long clickX = Fixed.FromInt(6).Raw;
            long clickY = Fixed.FromInt(7).Raw;
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 35, GameData.NeutralOwnerPlayerIndex, 7, 7),
                CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 34, GameData.NeutralOwnerPlayerIndex, 6, 7)
            });

            int picked = GodotInteractionRouter.FindResourceAt(frame, clickX, clickY);
            AssertEqual(34, picked, "resource selection should prefer nearest center when hit boxes overlap");
        }

        private static void GodotInteractionRouterRoutesMoveFallback()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 40, 1, 12, 12)
            });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(2).Raw, Fixed.FromInt(3).Raw);

            AssertEqual(GodotInteractionIntentKind.Move, intent.Kind, "empty right click should route to move fallback");
            AssertEqual(0, intent.TargetEntityId, "move intent should not expose an attack target id");
            AssertEqual(0, intent.ResourceNodeId, "move intent should not expose a resource id");
        }

        private static void GodotInteractionRouterIgnoresFriendlyTarget()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 50, 0, 4, 4)
            });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(4).Raw, Fixed.FromInt(4).Raw);

            AssertEqual(GodotInteractionIntentKind.Move, intent.Kind, "friendly targets should not route to attack");
            AssertEqual(0, intent.TargetEntityId, "friendly target should not be exposed as an attack target");
        }

        private static void GodotSelectionRouterPrioritizesLocalUnit()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 60, 0, 8, 8),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 61, 0, 8, 8)
            });

            GodotSelectionResult selection = GodotSelectionRouter.SelectAt(frame, 0, Fixed.FromInt(8).Raw, Fixed.FromInt(8).Raw);

            AssertEqual(GodotSelectionKind.Unit, selection.Kind, "local unit should take selection priority over local building");
            AssertEqual(61, selection.EntityId, "selection should expose selected unit id");
        }

        private static void GodotSelectionRouterSelectsLocalBuilding()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 70, 0, 9, 9)
            });

            GodotSelectionResult selection = GodotSelectionRouter.SelectAt(frame, 0, Fixed.FromInt(9).Raw, Fixed.FromInt(9).Raw);

            AssertEqual(GodotSelectionKind.Building, selection.Kind, "local building should be selectable");
            AssertEqual(70, selection.EntityId, "selection should expose selected building id");
        }

        private static void GodotSelectionRouterIgnoresEnemyPrimitive()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 80, 1, 10, 10)
            });

            GodotSelectionResult selection = GodotSelectionRouter.SelectAt(frame, 0, Fixed.FromInt(10).Raw, Fixed.FromInt(10).Raw);

            AssertEqual(GodotSelectionKind.None, selection.Kind, "enemy primitives should not be selected by local selection router");
            AssertEqual(0, selection.EntityId, "ignored selection should not expose an entity id");
        }

        private static void GodotSelectionRouterRectangleSelectsOwnedUnitsInIdOrder()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 103, 0, 7, 8),
                CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 201, 0, 8, 8),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 101, 0, 5, 5),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 102, 1, 6, 6),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 100, 0, 6, 7),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 104, 0, 20, 20)
            });

            int[] selected = GodotSelectionRouter.SelectUnitsInRectangle(
                frame,
                0,
                Fixed.FromInt(4).Raw,
                Fixed.FromInt(4).Raw,
                Fixed.FromInt(8).Raw,
                Fixed.FromInt(9).Raw);

            AssertEqual(3, selected.Length, "rectangle selection should include only owned units inside the drag box");
            AssertEqual(100, selected[0], "rectangle selection should return deterministic unit id order");
            AssertEqual(101, selected[1], "rectangle selection should return deterministic unit id order");
            AssertEqual(103, selected[2], "rectangle selection should return deterministic unit id order");
        }

        private static void GodotSelectionRouterRectangleNormalizesCorners()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 111, 0, 3, 4),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 112, 0, 12, 12)
            });

            int[] selected = GodotSelectionRouter.SelectUnitsInRectangle(
                frame,
                0,
                Fixed.FromInt(5).Raw,
                Fixed.FromInt(5).Raw,
                Fixed.FromInt(2).Raw,
                Fixed.FromInt(2).Raw);

            AssertEqual(1, selected.Length, "rectangle selection should normalize drag corners");
            AssertEqual(111, selected[0], "normalized rectangle should select the owned unit inside the box");
        }

        private static void GodotSelectionRouterReturnsNone()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 90, 0, 14, 14)
            });

            GodotSelectionResult selection = GodotSelectionRouter.SelectAt(frame, 0, Fixed.FromInt(2).Raw, Fixed.FromInt(2).Raw);

            AssertEqual(GodotSelectionKind.None, selection.Kind, "empty click should not select anything");
            AssertEqual(0, selection.EntityId, "empty selection should not expose an entity id");
        }

    }
}
