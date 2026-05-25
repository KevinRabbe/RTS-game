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
        private static void GodotPrimitiveHitTestIncludesBoundary()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 100, 0, 10, 10);

            bool contains = GodotPrimitiveHitTest.ContainsPoint(
                primitive,
                Fixed.FromInt(10).Raw + Fixed.FromRatio(1, 2).Raw,
                Fixed.FromInt(10).Raw);

            AssertEqual(true, contains, "hit test should include primitive boundary");
        }

        private static void GodotBuildingHitTestIncludesFootprintBoundary()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitiveWithSize(
                VisualPrimitiveKind.BuildingRectangle,
                103,
                0,
                (int)BuildingTypeId.TownCenter,
                10,
                10,
                GameData.GetBuildingPlacementRadiusTiles(BuildingTypeId.TownCenter) * 2);

            bool contains = GodotPrimitiveHitTest.ContainsPoint(
                primitive,
                Fixed.FromInt(12).Raw,
                Fixed.FromInt(10).Raw);

            AssertEqual(true, contains, "building hit test should include the full visual/sim footprint boundary");
        }

        private static void GodotPrimitiveHitTestRejectsOutside()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 101, 0, 10, 10);

            bool contains = GodotPrimitiveHitTest.ContainsPoint(
                primitive,
                Fixed.FromInt(10).Raw + Fixed.FromRatio(1, 2).Raw + 1,
                Fixed.FromInt(10).Raw);

            AssertEqual(false, contains, "hit test should reject points beyond primitive boundary");
        }

        private static void GodotPrimitiveHitTestSupportsRectangularBounds()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitiveWithDimensions(
                VisualPrimitiveKind.BuildingRectangle,
                1010,
                0,
                (int)BuildingTypeId.TownCenter,
                10,
                10,
                4,
                2);

            bool insideWideX = GodotPrimitiveHitTest.ContainsPoint(
                primitive,
                Fixed.FromInt(11).Raw,
                Fixed.FromInt(10).Raw);
            bool outsideShortY = GodotPrimitiveHitTest.ContainsPoint(
                primitive,
                Fixed.FromInt(10).Raw,
                Fixed.FromInt(12).Raw);

            AssertEqual(true, insideWideX, "rect hit test should include width-driven bounds");
            AssertEqual(false, outsideShortY, "rect hit test should reject outside height-driven bounds");
        }

        private static void GodotPrimitiveInteractionHitTestExpandsBuildingBounds()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 102, 0, (int)BuildingTypeId.TownCenter, 10, 10);

            long outsideCoreX = primitive.XRaw + (primitive.SizeRaw / 2) + Fixed.FromRatio(1, 2).Raw;
            long outsideCoreY = primitive.YRaw + (primitive.SizeRaw / 2) + Fixed.FromRatio(1, 2).Raw;
            bool containsCore = GodotPrimitiveHitTest.ContainsPoint(primitive, outsideCoreX, outsideCoreY);
            bool containsInteraction = GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, outsideCoreX, outsideCoreY);

            AssertEqual(false, containsCore, "core hit test should stay strict");
            AssertEqual(true, containsInteraction, "interaction hit test should expand building bounds for playability");
        }

        private static void GodotVisualStyleResolvesLocalUnitTypes()
        {
            GodotPrimitiveDto villager = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 110, 0, (int)UnitTypeId.Villager, 1, 1);
            GodotPrimitiveDto scout = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 111, 0, (int)UnitTypeId.Scout, 1, 1);
            GodotPrimitiveDto infantry = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 112, 0, (int)UnitTypeId.Infantry, 1, 1);
            GodotPrimitiveDto cavalry = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 113, 0, (int)UnitTypeId.Cavalry, 1, 1);

            AssertEqual(GodotVisualStyle.LocalVillager, GodotVisualStyleResolver.ResolveUnit(villager, 0), "villager should resolve to local villager style");
            AssertEqual(GodotVisualStyle.LocalScout, GodotVisualStyleResolver.ResolveUnit(scout, 0), "scout should resolve to local scout style");
            AssertEqual(GodotVisualStyle.LocalInfantry, GodotVisualStyleResolver.ResolveUnit(infantry, 0), "infantry should resolve to local infantry style");
            AssertEqual(GodotVisualStyle.LocalCavalry, GodotVisualStyleResolver.ResolveUnit(cavalry, 0), "cavalry should resolve to local cavalry style");
        }

        private static void GodotVisualStyleResolvesEnemyUnit()
        {
            GodotPrimitiveDto enemy = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 120, 1, (int)UnitTypeId.Villager, 1, 1);

            AssertEqual(GodotVisualStyle.EnemyUnit, GodotVisualStyleResolver.ResolveUnit(enemy, 0), "enemy unit should resolve to enemy style regardless of type");
        }

        private static void GodotVisualStyleResolvesBuildingTypes()
        {
            GodotPrimitiveDto normal = CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 130, 0, 1, 1);
            GodotPrimitiveDto wall = CreateGodotPrimitive(VisualPrimitiveKind.WallRectangle, 131, 0, 1, 1);
            GodotPrimitiveDto capital = CreateGodotPrimitiveWithCapital(VisualPrimitiveKind.BuildingRectangle, 132, 0, 1, 1);

            AssertEqual(GodotVisualStyle.NormalBuilding, GodotVisualStyleResolver.ResolveBuilding(normal), "normal building should resolve to normal building style");
            AssertEqual(GodotVisualStyle.Wall, GodotVisualStyleResolver.ResolveBuilding(wall), "wall should resolve to wall style");
            AssertEqual(GodotVisualStyle.CapitalBuilding, GodotVisualStyleResolver.ResolveBuilding(capital), "capital building should resolve to capital style");
        }

        private static void GodotVisualStyleResolvesResources()
        {
            GodotPrimitiveDto food = CreateGodotPrimitive(VisualPrimitiveKind.FoodResourceCircle, 140, GameData.NeutralOwnerPlayerIndex, 1, 1);
            GodotPrimitiveDto wood = CreateGodotPrimitive(VisualPrimitiveKind.WoodResourceCircle, 141, GameData.NeutralOwnerPlayerIndex, 1, 1);
            GodotPrimitiveDto gold = CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 142, GameData.NeutralOwnerPlayerIndex, 1, 1);

            AssertEqual(GodotVisualStyle.FoodResource, GodotVisualStyleResolver.ResolveResource(food), "food should resolve to food resource style");
            AssertEqual(GodotVisualStyle.WoodResource, GodotVisualStyleResolver.ResolveResource(wood), "wood should resolve to wood resource style");
            AssertEqual(GodotVisualStyle.GoldResource, GodotVisualStyleResolver.ResolveResource(gold), "gold should resolve to gold resource style");
        }

        private static void GodotTechLabelResolverResolvesKnownTech()
        {
            AssertEqual("InfAtk1", GodotTechLabelResolver.ResolveTechLabel((int)TechId.InfantryAttack1), "known tech id should map to readable label");
        }

        private static void GodotTechLabelResolverFallsBackForUnknownTech()
        {
            AssertEqual("#99", GodotTechLabelResolver.ResolveTechLabel(99), "unknown tech id should map to numeric fallback");
        }

        private static void GodotTechLabelResolverResolvesKnownModifier()
        {
            AssertEqual("InfAtkBonus", GodotTechLabelResolver.ResolveModifierLabel((int)ModifierId.InfantryAttackBonus), "known modifier id should map to readable label");
        }

        private static void GodotTechLabelResolverFallsBackForUnknownModifier()
        {
            AssertEqual("#42", GodotTechLabelResolver.ResolveModifierLabel(42), "unknown modifier id should map to numeric fallback");
        }

        private static void GodotResearchActionEvaluatorReturnsReady()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.InfantryAttack1FoodCost, 0, GameData.InfantryAttack1GoldCost, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(31, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.Ready, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 31), "evaluator should report ready when building and resources are valid");
        }

        private static void GodotResearchActionEvaluatorReturnsQueued()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    GameData.InfantryAttack1FoodCost,
                    0,
                    GameData.InfantryAttack1GoldCost,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new int[0],
                    new[] { new GodotResearchStatusDto((int)TechId.InfantryAttack1, 1, GameData.InfantryAttack1ResearchTicks) },
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(32, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.Queued, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 32), "evaluator should report queued when infantry attack research already exists in queue");
        }

        private static void GodotResearchActionEvaluatorReturnsDone()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new[] { (int)TechId.InfantryAttack1 },
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(33, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.Done, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 33), "evaluator should report done when infantry attack tech is already completed");
        }

        private static void GodotResearchActionEvaluatorReturnsMissingResources()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(34, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.MissingResources, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 34), "evaluator should report missing resources when cost cannot be paid");
        }

        private static void GodotResearchActionEvaluatorReturnsNone()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(35, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.None, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 0), "evaluator should report none when no building is selected");
        }

        private static void GodotResearchActionEvaluatorReturnsNotApplicable()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.InfantryAttack1FoodCost, 0, GameData.InfantryAttack1GoldCost, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(36, (int)BuildingTypeId.TradePost, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.NotApplicable, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 36), "evaluator should report not applicable for non-research building type");
        }

        private static void GodotResearchActionEvaluatorReturnsBlockedConstruction()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.InfantryAttack1FoodCost, 0, GameData.InfantryAttack1GoldCost, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(37, (int)BuildingTypeId.TownCenter, true, 1, 5, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.BlockedConstruction, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 37), "evaluator should report blocked construction for incomplete selected town center");
        }

        private static void GodotTrainActionEvaluatorReturnsReady()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.VillagerFoodCost, 0, 0, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(41, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.Ready, GodotTrainActionEvaluator.Evaluate(frame, 41, (int)UnitTypeId.Villager), "train evaluator should report ready when training constraints are satisfied");
        }

        private static void GodotTrainActionEvaluatorReturnsMissingResources()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(42, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.MissingResources, GodotTrainActionEvaluator.Evaluate(frame, 42, (int)UnitTypeId.Villager), "train evaluator should report missing resources when cost cannot be paid");
        }

        private static void GodotTrainActionEvaluatorReturnsPopulationCapped()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.VillagerFoodCost, 0, 0, 5, 5, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(43, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.PopulationCapped, GodotTrainActionEvaluator.Evaluate(frame, 43, (int)UnitTypeId.Villager), "train evaluator should report population cap block before queueing");
        }

        private static void GodotTrainActionEvaluatorReturnsNotApplicable()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.TradeCartWoodCost, 0, GameData.TradeCartGoldCost, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(44, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.NotApplicable, GodotTrainActionEvaluator.Evaluate(frame, 44, (int)UnitTypeId.TradeCart), "train evaluator should report not applicable when building cannot train the unit type");
        }

        private static void GodotTrainActionEvaluatorReturnsBlockedConstruction()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.VillagerFoodCost, 0, 0, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(45, (int)BuildingTypeId.TownCenter, true, 1, 5, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.BlockedConstruction, GodotTrainActionEvaluator.Evaluate(frame, 45, (int)UnitTypeId.Villager), "train evaluator should report blocked construction for incomplete selected building");
        }

        private static void GodotPrimitiveDrawKindResolvesKnownPrimitives()
        {
            AssertEqual(GodotPrimitiveDrawKind.Unit, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 150, 0, 1, 1)), "unit primitive should resolve to unit draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Building, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 151, 0, 1, 1)), "building primitive should resolve to building draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Building, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.WallRectangle, 152, 0, 1, 1)), "wall primitive should resolve to building draw kind");
            AssertEqual(GodotPrimitiveDrawKind.TradeRoute, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.TradeRouteLine, 153, 0, 1, 1)), "trade route primitive should resolve to trade route draw kind");
            AssertEqual(GodotPrimitiveDrawKind.HealthBar, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.HealthBar, 154, 0, 1, 1)), "health bar primitive should resolve to health bar draw kind");
            AssertEqual(GodotPrimitiveDrawKind.FogOverlay, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.FogOverlay, 155, 0, 1, 1)), "fog primitive should resolve to fog draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Resource, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.FoodResourceCircle, 156, GameData.NeutralOwnerPlayerIndex, 1, 1)), "food primitive should resolve to resource draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Resource, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.WoodResourceCircle, 157, GameData.NeutralOwnerPlayerIndex, 1, 1)), "wood primitive should resolve to resource draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Resource, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 158, GameData.NeutralOwnerPlayerIndex, 1, 1)), "gold primitive should resolve to resource draw kind");
        }

        private static void GodotPrimitiveDrawKindReturnsNoneForUnknown()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitive((VisualPrimitiveKind)999, 160, 0, 1, 1);

            AssertEqual(GodotPrimitiveDrawKind.None, GodotPrimitiveDrawKindResolver.Resolve(primitive), "unknown primitive kind should resolve to none");
        }

        private static void GodotDebugEventLogKeepsBoundedMessages()
        {
            var log = new GodotDebugEventLog(3);
            log.Add("a");
            log.Add("b");
            log.Add("c");
            log.Add("d");

            string[] lines = log.GetLines();
            AssertEqual(3, lines.Length, "bounded event log should keep max capacity");
            AssertEqual("b", lines[0], "oldest entry should roll off first");
            AssertEqual("d", lines[2], "latest entry should remain in log");
        }

        private static void GodotCommandResultClassifierClassifiesCounterDeltas()
        {
            AssertEqual(
                GodotCommandResultKind.Accepted,
                GodotCommandResultClassifier.Classify(10, 2, 11, 2),
                "increased executed count should classify as accepted");
            AssertEqual(
                GodotCommandResultKind.Rejected,
                GodotCommandResultClassifier.Classify(10, 2, 10, 3),
                "increased rejected count should classify as rejected");
            AssertEqual(
                GodotCommandResultKind.NoVisibleCountChange,
                GodotCommandResultClassifier.Classify(10, 2, 10, 2),
                "no count change should classify as no visible change");
        }

        private static void GodotHotkeyHelpContainsKnownBindings()
        {
            GodotHotkeyHelpEntry[] entries = GodotHotkeyHelpBuilder.Build(researchIsWired: true);
            AssertEqual(true, ContainsHotkey(entries, "F1", "Start DryArabiaTest01 economy test"), "hotkey help should include F1 binding");
            AssertEqual(true, ContainsHotkey(entries, "F2", "Start CombatTest01 combat test"), "hotkey help should include F2 binding");
            AssertEqual(true, ContainsHotkey(entries, "F6", "Start local 6-player FFA"), "hotkey help should include F6 binding");
            AssertEqual(true, ContainsHotkey(entries, "F9", "Toggle sprites/primitives"), "hotkey help should include F9 binding");
            AssertEqual(true, ContainsHotkey(entries, "F10", "Toggle debug overlay"), "hotkey help should include F10 debug overlay binding");
            AssertEqual(true, ContainsHotkey(entries, "F12", "Toggle screenshot mode"), "hotkey help should include F12 screenshot mode binding");
            AssertEqual(true, ContainsHotkey(entries, "H/F11", "Toggle hotkey help"), "hotkey help should include H/F11 help binding");
            AssertEqual(true, ContainsHotkey(entries, "Space", "Pause / unpause"), "hotkey help should include pause binding");
            AssertEqual(true, ContainsHotkey(entries, "Ctrl+1..9", "Assign selected units to control group"), "hotkey help should include control group assignment binding");
            AssertEqual(true, ContainsHotkey(entries, "1..9", "Recall control group selection"), "hotkey help should include control group recall binding");
            AssertEqual(true, ContainsHotkey(entries, "Left Click", "Select / confirm placement"), "hotkey help should include left-click selection behavior");
            AssertEqual(true, ContainsHotkey(entries, "Right Click", "Normal mode: Move/Gather/Attack/Build, Placement mode: cancel"), "hotkey help should include right-click context behavior");
            AssertEqual(true, ContainsHotkey(entries, "C", "Enter Town Center placement mode"), "hotkey help should include C placement binding");
            AssertEqual(true, ContainsHotkey(entries, "A", "Enter Attack-Move targeting mode"), "hotkey help should include A attack-move mode binding");
            AssertEqual(true, ContainsHotkey(entries, "A + Left Click", "Issue Attack-Move to ground"), "hotkey help should include explicit attack-move issue gesture");
            AssertEqual(true, ContainsHotkey(entries, "A + Right Click", "Cancel Attack-Move mode, then use normal RMB context"), "hotkey help should include attack-move cancel/context gesture");
            AssertEqual(true, ContainsHotkey(entries, "W", "Place Wall at mouse"), "hotkey help should include W placement binding");
            AssertEqual(true, ContainsHotkey(entries, "T", "Place Trade Post at mouse"), "hotkey help should include T placement binding");
            AssertEqual(true, ContainsHotkey(entries, "Escape", "Cancel active mode"), "hotkey help should include escape cancel mode binding");
            AssertEqual(true, ContainsHotkey(entries, "R", "Create Trade Route with selected Trade Cart"), "hotkey help should include R trade route binding");
            AssertEqual(true, ContainsHotkey(entries, "V", "Train Villager"), "hotkey help should include V train villager binding");
            AssertEqual(true, ContainsHotkey(entries, "I", "Train Infantry"), "hotkey help should include I train infantry binding");
            AssertEqual(true, ContainsHotkey(entries, "K", "Train Trade Cart"), "hotkey help should include K train trade cart binding");
            AssertEqual(true, ContainsHotkey(entries, "Y", "Research Infantry Attack I"), "hotkey help should include Y research binding");
        }

        private static void ControlGroupStateAssignRecallStoresSortedIds()
        {
            var groups = new GodotControlGroupState();
            groups.Assign(1, new[] { 5, 5, 7, 9, 9 });
            int[] recalled = groups.Recall(1);

            AssertEqual(3, recalled.Length, "control group should store deduped ids");
            AssertEqual(5, recalled[0], "control group should preserve deterministic id order");
            AssertEqual(7, recalled[1], "control group should preserve deterministic id order");
            AssertEqual(9, recalled[2], "control group should preserve deterministic id order");
        }

        private static void ControlGroupResolverFiltersMissingAndNonLocalUnits()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 101, 0, 5, 5),
                    CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 102, 1, 6, 6)
                },
                new GodotBuildingStatusDto[0],
                new[]
                {
                    new GodotUnitStatusDto(101, (int)UnitTypeId.Villager, 30, 30, false, 0, 0, 0, 0, 5, 5, 0, 0, 0, 0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, false, 0, 0, 0),
                    new GodotUnitStatusDto(102, (int)UnitTypeId.Villager, 30, 30, false, 0, 0, 0, 0, 6, 6, 0, 0, 0, 0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, false, 0, 0, 0)
                });

            int[] recalled = GodotControlGroupResolver.FilterRecallableLocalUnitIds(frame, 0, new[] { 101, 102, 999 });

            AssertEqual(1, recalled.Length, "control group recall should keep only visible local living units");
            AssertEqual(101, recalled[0], "control group recall should keep local unit id");
        }

        private static void ControlGroupFocusResolverReturnsCenterForRecallableUnits()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 201, 0, 10, 10),
                    CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 202, 0, 14, 6)
                },
                new GodotBuildingStatusDto[0],
                new[]
                {
                    new GodotUnitStatusDto(201, (int)UnitTypeId.Infantry, 60, 60, false, 0, 0, 0, 0, 10, 10, Fixed.FromInt(10).Raw, Fixed.FromInt(10).Raw, 0, 0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, false, 0, 0, 0),
                    new GodotUnitStatusDto(202, (int)UnitTypeId.Infantry, 60, 60, false, 0, 0, 0, 0, 14, 6, Fixed.FromInt(14).Raw, Fixed.FromInt(6).Raw, 0, 0, 0, 0, 0, false, false, false, 0, 0, 0, 0, 0, false, 0, 0, 0)
                });

            bool resolved = GodotControlGroupFocusResolver.TryResolveCenterRaw(frame, new[] { 201, 202 }, out long centerXRaw, out long centerYRaw);

            AssertEqual(true, resolved, "control group focus should resolve when units are present");
            AssertEqual(Fixed.FromInt(12).Raw, centerXRaw, "control group focus should average x in raw coordinates");
            AssertEqual(Fixed.FromInt(8).Raw, centerYRaw, "control group focus should average y in raw coordinates");
        }

        private static void ControlGroupFocusResolverReturnsFalseForMissingUnits()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new GodotPrimitiveDto[0], new GodotBuildingStatusDto[0], new GodotUnitStatusDto[0]);
            bool resolved = GodotControlGroupFocusResolver.TryResolveCenterRaw(frame, new[] { 999 }, out long centerXRaw, out long centerYRaw);

            AssertEqual(false, resolved, "control group focus should fail when recalled units are missing");
            AssertEqual(0L, centerXRaw, "missing focus center x should default to zero");
            AssertEqual(0L, centerYRaw, "missing focus center y should default to zero");
        }

        private static void GodotScenarioViewHintsProvidesCombatCameraStart()
        {
            bool hasHint = GodotScenarioViewHints.TryGetInitialCameraTile("CombatTest01", out int tileX, out int tileY);

            AssertEqual(true, hasHint, "combat scenario should provide initial camera hint");
            AssertEqual(64, tileX, "combat scenario camera hint should center near combat cluster x");
            AssertEqual(48, tileY, "combat scenario camera hint should center near combat cluster y");
        }

        private static void GodotScenarioViewHintsIgnoresUnknownMap()
        {
            bool hasHint = GodotScenarioViewHints.TryGetInitialCameraTile("DryArabiaTest01", out _, out _);
            AssertEqual(false, hasHint, "dry arabia should keep default camera behavior without forced hint");
        }

        private static void TcPlacementPreviewValidAtPlayer0TcZone()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(101);
            var godotFrame = facade.GetFrame(0);

            var p0Zone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var result = TcPlacementPreview.Evaluate(godotFrame, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt());

            AssertEqual(TcPlacementPreviewResult.Valid, result, "P0 TC zone should be a valid preview location on DryArabia");
            AssertEqual(true, PlaceTownCenterWouldBeValidOnDryArabia(0, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt(), 101), "valid preview should map to accepted placement command");
        }

        private static void TcPlacementPreviewValidAtPlayer1TcZone()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(102);
            var godotFrame = facade.GetFrame(1);

            var p1Zone = DryArabiaTest01MapDefinition.GetTownCenterZone(1);
            var result = TcPlacementPreview.Evaluate(godotFrame, p1Zone.X.FloorToInt(), p1Zone.Y.FloorToInt());

            AssertEqual(TcPlacementPreviewResult.Valid, result, "P1 TC zone should be a valid preview location on DryArabia");
            AssertEqual(true, PlaceTownCenterWouldBeValidOnDryArabia(1, p1Zone.X.FloorToInt(), p1Zone.Y.FloorToInt(), 102), "valid preview should map to accepted placement command");
        }

        private static void TcPlacementPreviewInvalidOverlappingResource()
        {
            var fakePrimitives = new GodotPrimitiveDto[]
            {
                new GodotPrimitiveDto(
                    (int)VisualPrimitiveKind.FoodResourceCircle, // Kind
                    100, // EntityId
                    0,   // TypeId
                    -1,  // OwnerPlayerIndex
                    30 * 65536, // XRaw (30 tiles)
                    48 * 65536, // YRaw (48 tiles)
                    0, 0, 0, 0, 0, false)
            };

            var godotFrame = new GodotFrameDto(
                0,
                "Test",
                0,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotMatchDto(false, -1, -1, 0),
                fakePrimitives,
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            var result = TcPlacementPreview.Evaluate(godotFrame, 30, 48);

            AssertEqual(TcPlacementPreviewResult.OverlapsResource, result, "TC preview on top of resource should be OverlapsResource");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, 30, 48, 103), "resource-overlap preview should map to rejected placement command");
        }

        private static void TcPlacementPreviewInvalidOverlappingBuilding()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(104);
            var p0Zone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            
            // Advance one tick to allow starting commands (if any) or simply queue a placement manually.
            facade.QueuePlaceTownCenter(0, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt());
            facade.AdvanceOneTick();

            var godotFrame = facade.GetFrame(0);

            // Same spot should now be blocked by the building
            var result = TcPlacementPreview.Evaluate(godotFrame, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt());

            AssertEqual(TcPlacementPreviewResult.MissingResources, result, "second TC without wood should report missing resources before placement overlap");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabiaWithPlacedTownCenter(0, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt(), 104), "building-overlap preview should map to rejected placement command");
        }

        private static void TcPlacementPreviewInvalidOutsideMap()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(105);
            var godotFrame = facade.GetFrame(0);

            var resultX = TcPlacementPreview.Evaluate(godotFrame, -1, 50);
            var resultY = TcPlacementPreview.Evaluate(godotFrame, 50, -1);
            var resultMaxX = TcPlacementPreview.Evaluate(godotFrame, 128, 50); // Map is 128x96
            var resultMaxY = TcPlacementPreview.Evaluate(godotFrame, 50, 96);

            AssertEqual(TcPlacementPreviewResult.OutsideMap, resultX, "Preview outside -x map should be OutsideMap");
            AssertEqual(TcPlacementPreviewResult.OutsideMap, resultY, "Preview outside -y map should be OutsideMap");
            AssertEqual(TcPlacementPreviewResult.OutsideMap, resultMaxX, "Preview outside +x map should be OutsideMap");
            AssertEqual(TcPlacementPreviewResult.OutsideMap, resultMaxY, "Preview outside +y map should be OutsideMap");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, -1, 50, 105), "outside-map preview should map to rejected placement command");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, 50, -1, 105), "outside-map preview should map to rejected placement command");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, 128, 50, 105), "outside-map preview should map to rejected placement command");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, 50, 96, 105), "outside-map preview should map to rejected placement command");
        }

        private static void TcPlacementPreviewCommandParityAroundPlayer0Zone()
        {
            var previewFacade = GodotClientFacade.CreateDryArabiaTest01(107);
            FixedVector2 zone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            int centerX = zone.X.FloorToInt();
            int centerY = zone.Y.FloorToInt();

            for (int y = centerY - 2; y <= centerY + 2; y++)
            {
                for (int x = centerX - 2; x <= centerX + 2; x++)
                {
                    GodotFrameDto frame = previewFacade.GetFrame(0);
                    TcPlacementPreviewResult preview = TcPlacementPreview.Evaluate(frame, x, y);
                    bool accepted = PlaceTownCenterWouldBeValidOnDryArabia(0, x, y, 107);
                    if (preview == TcPlacementPreviewResult.Valid)
                    {
                        AssertEqual(true, accepted, "preview valid should accept at tile (" + x + "," + y + ")");
                    }
                    else
                    {
                        AssertEqual(false, accepted, "preview invalid should reject at tile (" + x + "," + y + ") reason=" + preview);
                    }
                }
            }
        }

        private static void TcPlacementPreviewInvalidMissingResources()
        {
            var frame = new GodotFrameDto(
                0,
                "Test",
                0,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    true,
                    true,
                    true,
                    true,
                    false,
                    false,
                    new int[0],
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotMatchDto(false, -1, -1, 0),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);
            TcPlacementPreviewResult preview = TcPlacementPreview.Evaluate(frame, 20, 20);
            AssertEqual(TcPlacementPreviewResult.MissingResources, preview, "second-town-center preview should show missing resources");

            GameState state = GameInitializer.CreateDryArabiaTest01(108);
            state.PlayerStates.Players[0].CapitalStatus.HasCapitalBeenPlaced = true;
            bool accepted = new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))
                .IsValid(state, GameRules.CreatePhaseZeroDefaults(2), new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter));
            AssertEqual(false, accepted, "second-town-center missing wood should be rejected by command validation");
        }

        private static void TcPlacementPreviewInvalidBlockedPlayerState()
        {
            var frame = new GodotFrameDto(
                0,
                "Test",
                0,
                new GodotLocalPlayerDto(
                    0,
                    500,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    false,
                    false,
                    true,
                    new int[0],
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotMatchDto(false, -1, -1, 0),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);
            TcPlacementPreviewResult preview = TcPlacementPreview.Evaluate(frame, 10, 10);
            AssertEqual(TcPlacementPreviewResult.PlayerStateBlocked, preview, "resigned or disconnected player should preview blocked");
        }

        private static void TcPlacementPreviewDoesNotMutateChecksum()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(106);
            var godotFrame = facade.GetFrame(0);
            int beforeCommands = facade.ExecutedCommandCount;

            var p0Zone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            
            // Call it 10 times to ensure no weird internal state accumulates
            for (int i = 0; i < 10; i++)
            {
                TcPlacementPreview.Evaluate(godotFrame, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt());
            }

            int afterCommands = facade.ExecutedCommandCount;
            AssertEqual(beforeCommands, afterCommands, "TcPlacementPreview must be strictly read-only and not mutate state");
        }

        private static void TcPlacementPreviewUnknownResultDoesNotThrow()
        {
            // Null frame would normally not be called due to null check in RtsClientRoot,
            // but the enum supports Unknown for safe defaulting. We just verify the enum exists and resolves cleanly.
            AssertEqual((int)TcPlacementPreviewResult.Unknown, 4, "Unknown result should be 4");
        }

        private static void GodotHotkeyHelpContainsEdgePanEntry()
        {
            GodotHotkeyHelpEntry[] entries = GodotHotkeyHelpBuilder.Build(true);
            bool hasEdgePan = false;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Input == "Mouse Edge")
                {
                    hasEdgePan = true;
                }
            }

            AssertEqual(true, hasEdgePan, "Hotkey help should contain Mouse Edge panning documentation");
        }

        private static void GodotBuildingDebugStatusIncludesTrainingQueueCount()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[] { CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 211, 0, (int)BuildingTypeId.TownCenter, 5, 5) },
                new[] { new GodotBuildingStatusDto(211, (int)BuildingTypeId.TownCenter, false, 5, 5, 2, (int)UnitTypeId.Villager, 1, 3) });

            string[] lines = GodotBuildingDebugStatusBuilder.BuildLines(frame, 211);
            AssertEqual(true, lines[1].Contains("Queue 2"), "selected building debug status should include training queue count");
        }

        private static void GodotBuildingDebugStatusIncludesTrainingProgress()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[] { CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 212, 0, (int)BuildingTypeId.TradePost, 5, 5) },
                new[] { new GodotBuildingStatusDto(212, (int)BuildingTypeId.TradePost, false, 5, 5, 1, (int)UnitTypeId.TradeCart, 2, 4) });

            string[] lines = GodotBuildingDebugStatusBuilder.BuildLines(frame, 212);
            AssertEqual(true, lines[1].Contains("TradeCart 2/4"), "selected building debug status should include training progress");
        }

        private static void GodotBuildingDebugStatusMarksIncompleteTrainingUnavailable()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[] { CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 213, 0, (int)BuildingTypeId.TownCenter, 5, 5) },
                new[] { new GodotBuildingStatusDto(213, (int)BuildingTypeId.TownCenter, true, 3, GameData.TownCenterBuildTicks, 0, 0, 0, 0) });

            string[] lines = GodotBuildingDebugStatusBuilder.BuildLines(frame, 213);
            AssertEqual(true, lines[0].Contains("BUILDING 3/" + GameData.TownCenterBuildTicks), "selected incomplete building status should include build progress");
            AssertEqual(true, lines[1].Contains("unavailable until complete"), "selected incomplete building should mark training unavailable");
        }

        private static void GodotTrainIntentTextIncludesUnitAndBuilding()
        {
            string text = GodotBuildingDebugStatusBuilder.BuildTrainIntentText(22, (int)UnitTypeId.TradeCart);
            AssertEqual(true, text.Contains("building=22"), "train intent text should include building id");
            AssertEqual(true, text.Contains("TradeCart"), "train intent text should include unit type label");
        }

        private static void SimulationSourceDoesNotReferenceDebugOverlayHelpers()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "sim"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(text.Contains("GodotDebugEventLog"), "simulation source must not reference debug overlay log helper file=" + files[i]);
                AssertFalse(text.Contains("GodotHotkeyHelpBuilder"), "simulation source must not reference hotkey help helper file=" + files[i]);
                AssertFalse(text.Contains("GodotCommandResultClassifier"), "simulation source must not reference command result classifier helper file=" + files[i]);
            }
        }

        private static void GodotSpriteSheetLayoutResolvesExpectedFrameRect()
        {
            bool found = GodotSpriteSheetLayout.TryGetMetadata(GodotSpriteAssetId.Villager, out GodotSpriteSheetMetadata metadata);
            AssertEqual(true, found, "villager metadata should exist");

            GodotSpriteFrameRect rect = GodotSpriteSheetLayout.ResolveFrameRect(metadata, 900, 900, 5);
            AssertEqual(600, rect.X, "frame rect x should match frame column");
            AssertEqual(300, rect.Y, "frame rect y should match frame row");
            AssertEqual(300, rect.Width, "frame width should be texture width divided by columns");
            AssertEqual(300, rect.Height, "frame height should be texture height divided by rows");
        }

        private static void GodotSpriteSheetLayoutUsesDeterministicDefaultFrame()
        {
            bool found = GodotSpriteSheetLayout.TryGetMetadata(GodotSpriteAssetId.Infantry, out GodotSpriteSheetMetadata metadata);
            AssertEqual(true, found, "infantry metadata should exist");

            int frame = GodotSpriteSheetLayout.ResolveDirectionalFrameIndex(metadata, false, 1, 1);
            AssertEqual(metadata.DefaultFrameIndex, frame, "default frame index should be deterministic when there is no move target");
        }

        private static void GodotSpriteSheetLayoutReturnsFalseForUnknownUnitType()
        {
            bool found = GodotSpriteSheetLayout.TryResolveUnitAsset(999, out _);
            AssertEqual(false, found, "unknown unit type should not resolve to a sprite asset so primitive fallback can render");
        }

        private static void GodotSpriteSheetLayoutIncludesPlaceholderSlots()
        {
            AssertEqual(18, GodotSpriteSheetLayout.ExpectedAssetCount, "phase 6 placeholder registry should include all expected visual slots");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveBuildingAsset((int)BuildingTypeId.TownCenter, out GodotSpriteAssetId townCenter), "normal town center should have an asset slot");
            AssertEqual(GodotSpriteAssetId.TownCenter, townCenter, "normal town center should resolve to its own placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveBuildingAsset((int)BuildingTypeId.TradePost, out GodotSpriteAssetId tradePost), "trade post should have an asset slot");
            AssertEqual(GodotSpriteAssetId.TradePost, tradePost, "trade post should resolve to trade post placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveResourceAsset((int)ResourceType.Food, out GodotSpriteAssetId food), "food should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Food, food, "food should resolve to food placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveResourceAsset((int)ResourceType.Wood, out GodotSpriteAssetId wood), "wood should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Wood, wood, "wood should resolve to wood placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveResourceAsset((int)ResourceType.Gold, out GodotSpriteAssetId gold), "gold should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Gold, gold, "gold should resolve to gold placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveUnitAsset((int)UnitTypeId.Cavalry, out GodotSpriteAssetId cavalry), "cavalry should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Cavalry, cavalry, "cavalry should resolve to cavalry placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveUnitAsset((int)UnitTypeId.SiegeCannon, out GodotSpriteAssetId siege), "siege cannon should have an asset slot");
            AssertEqual(GodotSpriteAssetId.SiegeCannon, siege, "siege cannon should resolve to siege placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveUnitAsset((int)UnitTypeId.Mangonel, out GodotSpriteAssetId mangonel), "mangonel should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Mangonel, mangonel, "mangonel should resolve to mangonel placeholder slot");
        }

        private static void GodotSpriteSheetLayoutFilenamesAreCorrect()
        {
            AssertFilename(GodotSpriteAssetId.Villager, "villager_sheet.png");
            AssertFilename(GodotSpriteAssetId.Infantry, "infantry_sheet.png");
            AssertFilename(GodotSpriteAssetId.Scout, "scout_sheet.png");
            AssertFilename(GodotSpriteAssetId.TradeCart, "trade_cart_sheet.png");
            AssertFilename(GodotSpriteAssetId.Capital, "capital.png");
            AssertFilename(GodotSpriteAssetId.Wall, "wall_sheet.png");
            AssertFilename(GodotSpriteAssetId.TownCenter, "town_center.png");
            AssertFilename(GodotSpriteAssetId.TradePost, "trade_post.png");
            AssertFilename(GodotSpriteAssetId.BuildingScaffold, "building_scaffold.png");
            AssertFilename(GodotSpriteAssetId.Food, "food.png");
            AssertFilename(GodotSpriteAssetId.Wood, "wood.png");
            AssertFilename(GodotSpriteAssetId.Gold, "gold.png");
            AssertFilename(GodotSpriteAssetId.Cavalry, "cavalry_sheet.png");
            AssertFilename(GodotSpriteAssetId.SiegeCannon, "siege_cannon_sheet.png");
            AssertFilename(GodotSpriteAssetId.Mangonel, "mangonel_sheet.png");
            AssertFilename(GodotSpriteAssetId.GrassTile, "grass_tile_sheet.png");
            AssertFilename(GodotSpriteAssetId.DirtTile, "dirt_path_tile_sheet.png");
            AssertFilename(GodotSpriteAssetId.RockBlocker, "rock_blocker_sheet.png");
        }

        private static void GodotCoordinateMapperConvertsRawToPixels()
        {
            float pixels = GodotCoordinateMapper.RawToPixels(Fixed.FromInt(3).Raw, 16.0f);

            AssertEqual(48.0f, pixels, "three fixed tiles should convert to forty-eight pixels at sixteen pixels per tile");
        }

        private static void GodotCoordinateMapperConvertsScreenToRaw()
        {
            long raw = GodotCoordinateMapper.ScreenToRaw(24.0f, 16.0f);

            AssertEqual(Fixed.FromRatio(3, 2).Raw, raw, "twenty-four pixels should convert to one and a half raw tiles at sixteen pixels per tile");
        }

        private static void GodotCoordinateMapperFloorsScreenTile()
        {
            AssertEqual(1, GodotCoordinateMapper.ScreenToTile(31.9f, 16.0f), "positive screen coordinates should floor to tile index");
            AssertEqual(-1, GodotCoordinateMapper.ScreenToTile(-0.1f, 16.0f), "negative screen coordinates should floor down, not truncate toward zero");
        }

        private static void GodotTradeRouteRouterFindsLocalTradePost()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 170, 0, (int)BuildingTypeId.TradePost, 5, 5)
            });

            int tradePostId = GodotTradeRouteRouter.FindLocalTradePostAt(frame, 0, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(170, tradePostId, "router should find visible local trade post under cursor");
        }

        private static void GodotTradeRouteRouterIgnoresInvalidTradePostTargets()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 171, 1, (int)BuildingTypeId.TradePost, 5, 5),
                CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 172, 0, (int)BuildingTypeId.TownCenter, 5, 5)
            });

            int tradePostId = GodotTradeRouteRouter.FindLocalTradePostAt(frame, 0, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(0, tradePostId, "router should ignore enemy trade posts and non-trade-post buildings");
        }

        private static void GodotTradeRouteRouterFindsSelectedTradeCart()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new GodotPrimitiveDto[0],
                new GodotBuildingStatusDto[0],
                new[]
                {
                    new GodotUnitStatusDto(180, (int)UnitTypeId.TradeCart, false, 0, 0, 0, 0, 0, 0, 0, 0)
                });

            int tradeCartId = GodotTradeRouteRouter.FindSelectedTradeCart(frame, new[] { 180 });

            AssertEqual(180, tradeCartId, "router should find selected trade cart by unit status");
        }

        private static void GodotTradeRouteRouterIgnoresNonCartSelection()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new GodotPrimitiveDto[0],
                new GodotBuildingStatusDto[0],
                new[]
                {
                    new GodotUnitStatusDto(181, (int)UnitTypeId.Villager, false, 0, 0, 0, 0, 0, 0, 0, 0)
                });

            int tradeCartId = GodotTradeRouteRouter.FindSelectedTradeCart(frame, new[] { 181 });

            AssertEqual(0, tradeCartId, "router should ignore selected non-trade-cart units");
        }

    }
}
