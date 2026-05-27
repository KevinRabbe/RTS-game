using RtsGame.Presentation.GodotBridge;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void GodotHudTextIncludesEconomyAndSelection()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                42,
                new GodotLocalPlayerDto(100, 80, 30, 6, 20, true, true, true),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 1, 2 }, 11, 5, true);

            AssertEqual(true, text.Contains("Tick 42"), "hud should include tick");
            AssertEqual(true, text.Contains("Food 100"), "hud should include food");
            AssertEqual(true, text.Contains("Wood 80"), "hud should include wood");
            AssertEqual(true, text.Contains("Gold 30"), "hud should include gold");
            AssertEqual(true, text.Contains("Pop 6/20"), "hud should include population");
            AssertEqual(true, text.Contains("Rej 0"), "hud should include rejected command count");
            AssertEqual(true, text.Contains("Selected 1,2"), "hud should include selected units");
            AssertEqual(true, text.Contains("Building 11"), "hud should include selected building");
            AssertEqual(true, text.Contains("Resource 5"), "hud should include hovered resource");
            AssertEqual(true, text.Contains("Paused"), "hud should include pause state");
            AssertEqual(true, text.Contains("Press H for hotkeys"), "hud should include control hint text");
        }

        private static void GodotHudTextBuildLinesReturnsTwoLines()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string[] lines = GodotHudTextBuilder.BuildLines(frame, new int[0], 0, 0, false);

            AssertEqual(2, lines.Length, "hud text should return two lines for runtime readability");
            AssertEqual(true, lines[0].Contains("Tick 1"), "first hud line should include match/economy summary");
            AssertEqual(true, lines[1].Contains("Selected none"), "second hud line should include interaction summary");
        }

        private static void GodotHudTextIncludesUnitGatherStatus()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(3, (int)UnitTypeId.Villager, false, 0, 0, 0, 9, (int)ResourceType.Food, 10, 0, 0)
                },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 3 }, 0, 0, false);

            AssertEqual(true, text.Contains("Gather 9 Carry 10"), "hud should include selected unit gather status");
        }

        private static void GodotHudTextIncludesSelectedUnitTypeAndPhase()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(
                        7,
                        (int)UnitTypeId.Villager,
                        35,
                        GameData.VillagerHitPoints,
                        false,
                        0,
                        0,
                        0,
                        0,
                        10,
                        10,
                        Fixed.FromInt(10).Raw,
                        Fixed.FromInt(10).Raw,
                        (int)WorkerTaskPhase.Gathering,
                        0,
                        0,
                        0,
                        0,
                        true,
                        false,
                        false,
                        0,
                        0,
                        0,
                        0,
                        0)
                },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 7 }, 0, 0, false);

            AssertEqual(true, text.Contains("U7:Villager"), "hud should include selected primary unit id/type");
            AssertEqual(true, text.Contains("HP 35/" + GameData.VillagerHitPoints), "hud should include selected primary unit health");
            AssertEqual(true, text.Contains("Phase Gathering"), "hud should include selected primary unit task phase");
        }

        private static void GodotHudTextIncludesCombatAttackTargetAndCooldown()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(
                        18,
                        (int)UnitTypeId.Infantry,
                        42,
                        GameData.InfantryHitPoints,
                        false,
                        0,
                        0,
                        0,
                        0,
                        10,
                        10,
                        Fixed.FromInt(10).Raw,
                        Fixed.FromInt(10).Raw,
                        (int)WorkerTaskPhase.MovingToAttackSlot,
                        0,
                        0,
                        0,
                        0,
                        false,
                        false,
                        false,
                        0,
                        0,
                        0,
                        25,
                        4)
                },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 18 }, 0, 0, false);

            AssertEqual(true, text.Contains("Attack 25 CD 4"), "hud should include selected combat attack target and cooldown");
            AssertEqual(true, text.Contains("HP 42/" + GameData.InfantryHitPoints), "hud should include selected combat health");
        }

        private static void GodotHudTextIncludesAttackMoveDestination()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                12,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(
                        19,
                        (int)UnitTypeId.Infantry,
                        60,
                        GameData.InfantryHitPoints,
                        true,
                        Fixed.FromInt(8).Raw,
                        Fixed.FromInt(8).Raw,
                        0,
                        0,
                        10,
                        10,
                        Fixed.FromInt(10).Raw,
                        Fixed.FromInt(10).Raw,
                        (int)WorkerTaskPhase.MovingToCommandMove,
                        0,
                        0,
                        0,
                        0,
                        false,
                        false,
                        false,
                        11,
                        0,
                        0,
                        0,
                        0,
                        true,
                        Fixed.FromInt(14).Raw,
                        Fixed.FromInt(22).Raw,
                        18)
                },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 19 }, 0, 0, false);

            AssertEqual(true, text.Contains("AttackMove (14,22)"), "hud should include selected unit attack-move destination tile");
            AssertEqual(true, text.Contains("Acquire @18"), "hud should include selected unit next attack-move acquisition tick");
        }

        private static void GodotHudTextIncludesAttackMoveDestinationWhileEngagingTarget()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                15,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(
                        29,
                        (int)UnitTypeId.Infantry,
                        58,
                        GameData.InfantryHitPoints,
                        true,
                        Fixed.FromInt(20).Raw,
                        Fixed.FromInt(20).Raw,
                        0,
                        0,
                        10,
                        10,
                        Fixed.FromInt(10).Raw,
                        Fixed.FromInt(10).Raw,
                        (int)WorkerTaskPhase.MovingToAttackSlot,
                        0,
                        0,
                        0,
                        0,
                        false,
                        false,
                        false,
                        0,
                        0,
                        0,
                        88,
                        3,
                        true,
                        Fixed.FromInt(30).Raw,
                        Fixed.FromInt(42).Raw,
                        17)
                },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 29 }, 0, 0, false);

            AssertEqual(true, text.Contains("Attack 88 CD 3"), "hud should keep current attack target and cooldown while engaging");
            AssertEqual(true, text.Contains("AM(30,42)"), "hud should keep attack-move destination visible while engaging temporary target");
        }

        private static void GodotHudTextIncludesBuildingTrainingStatus()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[]
                {
                    new GodotBuildingStatusDto(11, (int)BuildingTypeId.TownCenter, false, 0, 0, 1, (int)UnitTypeId.Villager, 4, GameData.VillagerTrainTicks)
                });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 11, 0, false);

            AssertEqual(true, text.Contains("Train Villager 4/" + GameData.VillagerTrainTicks), "hud should include selected building training status");
        }

        private static void GodotHudTextIncludesBuildingTypeLabel()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[]
                {
                    new GodotBuildingStatusDto(19, (int)BuildingTypeId.TradePost, 120, GameData.TradePostHitPoints, false, 0, 0, 0, 0, 0, 0)
                });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 19, 0, false);

            AssertEqual(true, text.Contains("TradePost HP 120/" + GameData.TradePostHitPoints + " Ready"), "hud should include selected building type, hp, and state");
        }

        private static void GodotHudTextIncludesResearchActionReady()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.InfantryAttack1FoodCost, 0, GameData.InfantryAttack1GoldCost, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(21, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 21, 0, false);

            AssertEqual(true, text.Contains("Y:Research Ready"), "hud should show ready research action for selected town center");
        }

        private static void GodotHudTextIncludesResearchActionQueued()
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
                new[] { new GodotBuildingStatusDto(22, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 22, 0, false);

            AssertEqual(true, text.Contains("Y:Research Queued"), "hud should show queued research action for selected town center");
        }

        private static void GodotHudTextIncludesResearchActionDone()
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
                new[] { new GodotBuildingStatusDto(23, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 23, 0, false);

            AssertEqual(true, text.Contains("Y:Research Done"), "hud should show completed research action for selected town center");
        }

        private static void GodotHudTextIncludesResearchActionBlockedBuild()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(24, (int)BuildingTypeId.TownCenter, true, 1, 5, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 24, 0, false);

            AssertEqual(true, text.Contains("Y:Research Blocked(Build)"), "hud should show blocked research action for under-construction town center");
        }

        private static void GodotHudTextIncludesResearchActionCost()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(25, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 25, 0, false);

            AssertEqual(true, text.Contains("Y:Research Cost " + GameData.InfantryAttack1FoodCost + "F/" + GameData.InfantryAttack1GoldCost + "G"), "hud should show research cost action when resources are missing");
        }

        private static void GodotHudTextIncludesTrainActionStatusForTownCenter()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.VillagerFoodCost, 0, 0, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(26, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 26, 0, false);

            AssertEqual(true, text.Contains("V:Ready"), "town center should show villager train action as ready with enough food");
            AssertEqual(true, text.Contains("I:Cost"), "town center should show infantry train action as cost when missing food");
            AssertEqual(true, text.Contains("K:N/A"), "town center should show trade cart train action as not applicable");
        }

        private static void GodotHudTextIncludesTrainActionStatusForTradePost()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, GameData.TradeCartWoodCost, GameData.TradeCartGoldCost, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(27, (int)BuildingTypeId.TradePost, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 27, 0, false);

            AssertEqual(true, text.Contains("V:N/A"), "trade post should show villager train action as not applicable");
            AssertEqual(true, text.Contains("I:N/A"), "trade post should show infantry train action as not applicable");
            AssertEqual(true, text.Contains("K:Ready"), "trade post should show trade cart train action as ready with enough resources");
        }

        private static void GodotHudTextIncludesResearchStatus()
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
                    new int[0],
                    new[] { new GodotResearchStatusDto((int)TechId.InfantryAttack1, 2, GameData.InfantryAttack1ResearchTicks) },
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Research InfAtk1 2/" + GameData.InfantryAttack1ResearchTicks), "hud should include active research status");
        }

        private static void GodotHudTextIncludesQueuedResearchCount()
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
                    new int[0],
                    new[]
                    {
                        new GodotResearchStatusDto((int)TechId.InfantryAttack1, 2, GameData.InfantryAttack1ResearchTicks),
                        new GodotResearchStatusDto(99, 0, 10)
                    },
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Research InfAtk1 2/" + GameData.InfantryAttack1ResearchTicks + " +1"), "hud should include queued research count suffix");
        }

        private static void GodotHudTextIncludesModifierStatus()
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
                    new[] { new GodotModifierStatusDto((int)ModifierId.InfantryAttackBonus, GameData.InfantryAttack1DamageBonus) }),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Mod InfAtkBonus=" + GameData.InfantryAttack1DamageBonus), "hud should include first active modifier status");
        }

        private static void GodotHudTextIncludesModifierCount()
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
                    new[]
                    {
                        new GodotModifierStatusDto((int)ModifierId.InfantryAttackBonus, GameData.InfantryAttack1DamageBonus),
                        new GodotModifierStatusDto(99, 2)
                    }),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Mod InfAtkBonus=" + GameData.InfantryAttack1DamageBonus + " +1"), "hud should include active modifier count suffix");
        }

        private static void GodotHudTextIncludesCompletedTechLabel()
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
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Tech InfAtk1"), "hud should include completed tech label when research queue is empty");
        }

        private static void GodotHudTextIncludesCompletedTechCount()
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
                    new[] { (int)TechId.InfantryAttack1, 99 },
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Tech #99 (2)"), "hud should include completed tech count suffix");
        }

        private static void GodotHudTextIncludesRejectedCommandCount()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0],
                3);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Rej 3"), "hud should include rejected command count from match dto");
        }

        private static void GodotHudTextIncludesLastCommandStatus()
        {
            GodotFrameDto frame = new GodotFrameDto(
                1,
                "DryArabiaTest01",
                0,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotMatchDto(
                    false,
                    -1,
                    -1,
                    12,
                    2,
                    (int)CommandType.GatherResource,
                    (int)CommandValidationReason.TargetComplete,
                    false,
                    0,
                    5,
                    20,
                    42,
                    3,
                    1),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Cmd GatherResource rej"), "hud should include last command type and result");
            AssertEqual(true, text.Contains("r" + (int)CommandValidationReason.TargetComplete), "hud should include last command reason id");
        }

        private static void GodotHudTextIncludesLastCommandReasonLabel()
        {
            GodotFrameDto frame = new GodotFrameDto(
                1,
                "DryArabiaTest01",
                0,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotMatchDto(
                    false,
                    -1,
                    -1,
                    1,
                    1,
                    (int)CommandType.GatherResource,
                    (int)CommandValidationReason.UnitCannotPerformAction,
                    false,
                    0,
                    4,
                    19,
                    57,
                    1,
                    3),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);
            AssertEqual(true, text.Contains("r7(UnitCannotPerformAction)"), "hud should include reason label for last command");
        }

        private static void GodotHudTextIncludesReadableNonCombatAttackRejectionHint()
        {
            GodotFrameDto frame = new GodotFrameDto(
                1,
                "DryArabiaTest01",
                0,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotMatchDto(
                    false,
                    -1,
                    -1,
                    2,
                    1,
                    (int)CommandType.Attack,
                    (int)CommandValidationReason.UnitCannotPerformAction,
                    false,
                    0,
                    4,
                    19,
                    57,
                    1,
                    3),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);
            AssertEqual(true, text.Contains("[NonCombatCannotAttack]"), "hud should include readable non-combat attack rejection hint");
        }

        private static void GodotHudTextIncludesReadableNonCombatAttackMoveRejectionHint()
        {
            GodotFrameDto frame = new GodotFrameDto(
                1,
                "CombatTest01",
                0,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotMatchDto(
                    false,
                    -1,
                    -1,
                    2,
                    1,
                    (int)CommandType.AttackMove,
                    (int)CommandValidationReason.UnitCannotPerformAction,
                    false,
                    0,
                    4,
                    19,
                    57,
                    1,
                    3),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);
            AssertEqual(true, text.Contains("[NonCombatCannotAttackMove]"), "hud should include readable non-combat attack-move rejection hint");
        }

        private static void GodotHudTextHandlesMissingStatus()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 99 }, 77, 0, false);

            AssertEqual(true, text.Contains("Selected 99"), "hud should still include selected unit id when status is missing");
            AssertEqual(true, text.Contains("Building 77"), "hud should still include selected building id when status is missing");
            AssertEqual(false, text.Contains("Gather"), "missing unit status should not invent gather text");
            AssertEqual(false, text.Contains("Train"), "missing building status should not invent training text");
        }

        private static void GodotSelectedStatusHidesIdleNoProgressTicks()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new GodotPrimitiveDto[0],
                new GodotBuildingStatusDto[0],
                new[]
                {
                    new GodotUnitStatusDto(
                        11,
                        (int)UnitTypeId.Villager,
                        false,
                        0,
                        0,
                        0,
                        0,
                        19,
                        44,
                        Fixed.FromInt(19).Raw,
                        Fixed.FromInt(44).Raw,
                        (int)WorkerTaskPhase.Idle,
                        (int)InteractionReservationKind.MoveDestination,
                        SpatialRules.EncodeTileKey(20, 44),
                        20,
                        44,
                        false,
                        false,
                        false,
                        10,
                        0,
                        0,
                        0,
                        0)
                });

            string[] lines = GodotSelectedStatusBuilder.BuildLines(frame, new[] { 11 }, 0, 0);

            AssertEqual(true, lines[1].Contains("NoProgress -"), "idle unit without active move should hide stale no-progress counter");
        }

        private static void GodotSelectedStatusShowsBlockedWaitingNoProgressTicks()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                19,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(
                        12,
                        (int)UnitTypeId.Villager,
                        25,
                        GameData.VillagerHitPoints,
                        false,
                        0,
                        0,
                        0,
                        0,
                        19,
                        44,
                        Fixed.FromInt(19).Raw,
                        Fixed.FromInt(44).Raw,
                        (int)WorkerTaskPhase.BlockedWaiting,
                        0,
                        0,
                        0,
                        0,
                        false,
                        false,
                        false,
                        10,
                        0,
                        0,
                        0,
                        0)
                },
                new GodotBuildingStatusDto[0]);

            string[] lines = GodotSelectedStatusBuilder.BuildLines(frame, new[] { 12 }, 0, 0);

            AssertEqual(true, lines[1].Contains("HP 25/" + GameData.VillagerHitPoints), "selected status should include health values");
            AssertEqual(true, lines[1].Contains("AttackCooldown -"), "selected status should include combat cooldown field");
            AssertEqual(true, lines[1].Contains("NoProgress 9"), "blocked waiting unit should expose no-progress ticks for stall diagnosis");
        }

        private static void GodotSelectedStatusIncludesAttackMoveDebugFields()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                22,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(
                        44,
                        (int)UnitTypeId.Infantry,
                        54,
                        GameData.InfantryHitPoints,
                        true,
                        Fixed.FromInt(9).Raw,
                        Fixed.FromInt(6).Raw,
                        0,
                        0,
                        6,
                        6,
                        Fixed.FromInt(6).Raw,
                        Fixed.FromInt(6).Raw,
                        (int)WorkerTaskPhase.MovingToCommandMove,
                        (int)InteractionReservationKind.AttackSlot,
                        88,
                        7,
                        6,
                        false,
                        false,
                        false,
                        20,
                        0,
                        0,
                        88,
                        2,
                        true,
                        Fixed.FromInt(18).Raw,
                        Fixed.FromInt(30).Raw,
                        24)
                },
                new GodotBuildingStatusDto[0]);

            string[] lines = GodotSelectedStatusBuilder.BuildLines(frame, new[] { 44 }, 0, 0);

            AssertEqual(true, lines[1].Contains("AttackMoveTarget Tile(18,30)"), "selected status should include attack-move destination tile");
            AssertEqual(true, lines[1].Contains("AttackMoveAcquire @24"), "selected status should include attack-move acquisition cadence field");
            AssertEqual(true, lines[1].Contains("AttackTarget 88"), "selected status should include current attack target");
        }

        private static void GodotControlGroupFeedbackFormatsAssignText()
        {
            string text = GodotControlGroupFeedbackFormatter.BuildAssignedText(1, 5);
            AssertEqual("Group 1 assigned: 5 units", text, "control group assign feedback should be concise and stable");
        }

        private static void GodotControlGroupFeedbackFormatsAddedText()
        {
            string text = GodotControlGroupFeedbackFormatter.BuildAddedText(1, 7);
            AssertEqual("Group 1 added: 7 units", text, "control group additive assign feedback should be concise and stable");
        }

        private static void GodotControlGroupFeedbackFormatsRecallText()
        {
            string text = GodotControlGroupFeedbackFormatter.BuildRecalledText(2, 3);
            AssertEqual("Group 2 recalled: 3 units", text, "control group recall feedback should be concise and stable");
        }

        private static void GodotControlGroupFeedbackFormatsEmptyText()
        {
            string text = GodotControlGroupFeedbackFormatter.BuildRecalledText(3, 0);
            AssertEqual("Group 3 empty", text, "empty control group recall should be clearly visible");
        }

        private static void GodotHudTextIncludesSelectedTypeSummaryHomogeneous()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(21, (int)UnitTypeId.Villager, false, 0, 0, 0, 0, 0, 0, 0, 0),
                    new GodotUnitStatusDto(22, (int)UnitTypeId.Villager, false, 0, 0, 0, 0, 0, 0, 0, 0),
                    new GodotUnitStatusDto(23, (int)UnitTypeId.Villager, false, 0, 0, 0, 0, 0, 0, 0, 0)
                },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 21, 22, 23 }, 0, 0, false);
            AssertEqual(true, text.Contains("3 Villager"), "hud should include homogeneous selected type summary");
        }

        private static void GodotHudTextIncludesSelectedTypeSummaryMixed()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(31, (int)UnitTypeId.Infantry, false, 0, 0, 0, 0, 0, 0, 0, 0),
                    new GodotUnitStatusDto(32, (int)UnitTypeId.Infantry, false, 0, 0, 0, 0, 0, 0, 0, 0),
                    new GodotUnitStatusDto(33, (int)UnitTypeId.Infantry, false, 0, 0, 0, 0, 0, 0, 0, 0),
                    new GodotUnitStatusDto(34, (int)UnitTypeId.Infantry, false, 0, 0, 0, 0, 0, 0, 0, 0),
                    new GodotUnitStatusDto(35, (int)UnitTypeId.Scout, false, 0, 0, 0, 0, 0, 0, 0, 0)
                },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 31, 32, 33, 34, 35 }, 0, 0, false);
            AssertEqual(true, text.Contains("4 Infantry, 1 Scout"), "hud should include mixed selected type summary");
        }

        private static void GodotHudTextIncludesSelectedControlGroupIndicator()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[] { new GodotUnitStatusDto(41, (int)UnitTypeId.Infantry, false, 0, 0, 0, 0, 0, 0, 0, 0) },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 41 }, 0, 0, false, selectedControlGroupIndex: 3);
            AssertEqual(true, text.Contains("CG:3"), "hud should include selected control group indicator when current selection matches a group");
        }

        private static void GodotHudTextOmitsSelectedControlGroupIndicatorWhenNoMatch()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[] { new GodotUnitStatusDto(42, (int)UnitTypeId.Infantry, false, 0, 0, 0, 0, 0, 0, 0, 0) },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 42 }, 0, 0, false);
            AssertEqual(false, text.Contains("CG:"), "hud should omit control group indicator when current selection does not match a group");
        }

        private static void GodotHudTextIncludesInputModeLabelWhenActive()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false, 0, null, "AttackMove");
            AssertEqual(true, text.Contains("Mode AttackMove"), "hud should expose active input mode for command clarity");
        }

        private static void GodotHudTextOmitsInputModeLabelWhenNormal()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false, 0, null, "");
            AssertEqual(false, text.Contains("Mode "), "hud should omit input mode label when no temporary command mode is active");
        }

        private static void GodotHudTextIncludesContextCommandHintWhenProvided()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 11 }, 0, 0, false, 0, null, "", "RMB Gather");
            AssertEqual(true, text.Contains("RMB Gather"), "hud should include contextual command hint when provided");
        }

        private static void GodotHudTextOmitsContextCommandHintWhenEmpty()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 11 }, 0, 0, false, 0, null, "", "");
            AssertEqual(false, text.Contains("RMB "), "hud should omit contextual command hint when not provided");
        }

        private static void GodotHudTextIncludesMultipleSelectedControlGroupIndicators()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[] { new GodotUnitStatusDto(43, (int)UnitTypeId.Infantry, false, 0, 0, 0, 0, 0, 0, 0, 0) },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 43 }, 0, 0, false, 0, new[] { 1, 3 }, "");
            AssertEqual(true, text.Contains("CG:1,3"), "hud should include all matching control groups when selection is shared");
        }

    }
}
