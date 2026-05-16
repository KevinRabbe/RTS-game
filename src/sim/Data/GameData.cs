namespace RtsGame.Sim.Data
{
    public static class GameData
    {
        public const int VillagerHitPoints = 25;
        public const int ScoutHitPoints = 45;
        public const int InfantryHitPoints = 60;
        public const int SiegeCannonHitPoints = 90;
        public const int TradeCartHitPoints = 50;
        public const int MangonelHitPoints = 75;
        public const int CavalryHitPoints = 85;
        public const int TownCenterHitPoints = 2400;
        public const int WallHitPoints = 300;
        public const int TradePostHitPoints = 900;
        public const int WallUnderConstructionHitPoints = 60;
        public const int TradePostUnderConstructionHitPoints = 180;
        public const int CapitalHitPointBonus = 400;
        public const int CapitalPopulationBonus = 10;
        public const int TownCenterBuildTicks = 5;
        public const int WallBuildTicks = 4;
        public const int TradePostBuildTicks = 6;
        public const int VillagerGatherPerTick = 5;
        public const int VillagerCarryCapacity = 10;
        public const int VillagerTrainTicks = 3;
        public const int VillagerFoodCost = 50;
        public const int InfantryTrainTicks = 4;
        public const int InfantryFoodCost = 60;
        public const int CavalryTrainTicks = 5;
        public const int CavalryFoodCost = 80;
        public const int CavalryGoldCost = 40;
        public const int SiegeCannonTrainTicks = 8;
        public const int SiegeCannonWoodCost = 120;
        public const int SiegeCannonGoldCost = 80;
        public const int MangonelTrainTicks = 7;
        public const int MangonelWoodCost = 110;
        public const int MangonelGoldCost = 60;
        public const int TradeCartTrainTicks = 4;
        public const int TradeCartWoodCost = 80;
        public const int TradeCartGoldCost = 20;
        public const int VillagerMoveSpeedTilesPerTickNumerator = 1;
        public const int VillagerMoveSpeedTilesPerTickDenominator = 2;
        public const int ScoutMoveSpeedTilesPerTickNumerator = 1;
        public const int ScoutMoveSpeedTilesPerTickDenominator = 1;
        public const int InfantryMoveSpeedTilesPerTickNumerator = 2;
        public const int InfantryMoveSpeedTilesPerTickDenominator = 5;
        public const int CavalryMoveSpeedTilesPerTickNumerator = 4;
        public const int CavalryMoveSpeedTilesPerTickDenominator = 5;
        public const int SiegeCannonMoveSpeedTilesPerTickNumerator = 1;
        public const int SiegeCannonMoveSpeedTilesPerTickDenominator = 5;
        public const int MangonelMoveSpeedTilesPerTickNumerator = 1;
        public const int MangonelMoveSpeedTilesPerTickDenominator = 5;
        public const int TradeCartMoveSpeedTilesPerTickNumerator = 3;
        public const int TradeCartMoveSpeedTilesPerTickDenominator = 5;
        public const int MapWidthTiles = 128;
        public const int MapHeightTiles = 96;
        public const int VillagerSightRadiusTiles = 4;
        public const int ScoutSightRadiusTiles = 8;
        public const int InfantrySightRadiusTiles = 5;
        public const int CavalrySightRadiusTiles = 5;
        public const int SiegeCannonSightRadiusTiles = 6;
        public const int TradeCartSightRadiusTiles = 5;
        public const int MangonelSightRadiusTiles = 6;
        public const int TownCenterSightRadiusTiles = 6;
        public const int TradePostSightRadiusTiles = 6;
        public const int NeutralOwnerPlayerIndex = -1;
        public const int ResignedAssetDespawnTicks = 1200;
        public const int VillagerAttackDamage = 2;
        public const int ScoutAttackDamage = 3;
        public const int InfantryAttackDamage = 12;
        public const int CavalryAttackDamage = 16;
        public const int VillagerAttackCooldownTicks = 8;
        public const int ScoutAttackCooldownTicks = 6;
        public const int InfantryAttackCooldownTicks = 5;
        public const int CavalryAttackCooldownTicks = 5;
        public const int SiegeCannonSetupTicks = 3;
        public const int SiegeCannonReloadTicks = 6;
        public const int SiegeCannonBuildingDamage = 220;
        public const int SiegeCannonRangeTiles = 8;
        public const int MangonelAreaDamage = 30;
        public const int MangonelAreaRadiusTiles = 2;
        public const int MangonelAreaCooldownTicks = 8;
        public const int MangonelAreaRangeTiles = 6;
        public const int TradeIncomePerTile = 2;
        public const int StartingFoodAmount = 500;
        public const int StartingWoodAmount = 700;
        public const int StartingGoldAmount = 400;
        public const int CenterFoodAmount = 900;
        public const int CenterWoodAmount = 1000;
        public const int CenterGoldAmount = 1200;
        public const int TownCenterPlacementRadiusTiles = 2;
        public const int WallPlacementRadiusTiles = 1;
        public const int TradePostPlacementRadiusTiles = 2;
        public const int ResourcePlacementRadiusTiles = 1;
        public const int InteractionTargetRetargetBlockedTicks = 8;
        public const int TownCenterWoodCost = 275;
        public const int WallWoodCost = 5;
        public const int TradePostWoodCost = 150;
        public const int TradePostGoldCost = 50;
        public const int InfantryAttack1ResearchTicks = 6;
        public const int InfantryAttack1FoodCost = 100;
        public const int InfantryAttack1GoldCost = 50;
        public const int InfantryAttack1DamageBonus = 2;

        public static GatherProfile GetGatherProfile(GatherProfileId profileId)
        {
            switch (profileId)
            {
                case GatherProfileId.Tree:
                    return new GatherProfile(
                        GatherProfileId.Tree,
                        ResourceType.Wood,
                        ResourceNodeType.Tree,
                        DropOffCategory.Wood,
                        VillagerCarryCapacity,
                        VillagerGatherPerTick,
                        ResourcePlacementRadiusTiles,
                        ResourcePlacementRadiusTiles + 2,
                        true,
                        true,
                        ResourceDepletedBehavior.RemoveNodeBlocker,
                        ResourceAutoContinuationMode.SameArea);
                case GatherProfileId.BerryBush:
                    return new GatherProfile(
                        GatherProfileId.BerryBush,
                        ResourceType.Food,
                        ResourceNodeType.BerryBush,
                        DropOffCategory.Food,
                        VillagerCarryCapacity,
                        VillagerGatherPerTick,
                        ResourcePlacementRadiusTiles,
                        ResourcePlacementRadiusTiles + 1,
                        true,
                        true,
                        ResourceDepletedBehavior.RemoveNodeBlocker,
                        ResourceAutoContinuationMode.SameArea);
                case GatherProfileId.GoldVeinSmall:
                    return new GatherProfile(
                        GatherProfileId.GoldVeinSmall,
                        ResourceType.Gold,
                        ResourceNodeType.GoldVeinSmall,
                        DropOffCategory.Gold,
                        VillagerCarryCapacity,
                        VillagerGatherPerTick,
                        ResourcePlacementRadiusTiles,
                        ResourcePlacementRadiusTiles + 1,
                        true,
                        true,
                        ResourceDepletedBehavior.RemoveNodeBlocker,
                        ResourceAutoContinuationMode.SameArea);
                case GatherProfileId.GoldVeinLarge:
                    return new GatherProfile(
                        GatherProfileId.GoldVeinLarge,
                        ResourceType.Gold,
                        ResourceNodeType.GoldVeinLarge,
                        DropOffCategory.Gold,
                        VillagerCarryCapacity,
                        VillagerGatherPerTick,
                        ResourcePlacementRadiusTiles + 1,
                        ResourcePlacementRadiusTiles + 2,
                        true,
                        true,
                        ResourceDepletedBehavior.RemoveNodeBlocker,
                        ResourceAutoContinuationMode.SameArea);
                default:
                    return new GatherProfile(
                        GatherProfileId.None,
                        ResourceType.None,
                        ResourceNodeType.None,
                        DropOffCategory.None,
                        VillagerCarryCapacity,
                        VillagerGatherPerTick,
                        ResourcePlacementRadiusTiles,
                        ResourcePlacementRadiusTiles,
                        true,
                        true,
                        ResourceDepletedBehavior.RemoveNodeBlocker,
                        ResourceAutoContinuationMode.None);
            }
        }

        public static int GetBuildingPlacementRadiusTiles(BuildingTypeId buildingTypeId)
        {
            switch (buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    return TownCenterPlacementRadiusTiles;
                case BuildingTypeId.Wall:
                    return WallPlacementRadiusTiles;
                case BuildingTypeId.TradePost:
                    return TradePostPlacementRadiusTiles;
                default:
                    return 1;
            }
        }

        public static ResourceStockpile GetBuildingCost(BuildingTypeId buildingTypeId, bool isFirstTownCenter)
        {
            var cost = new ResourceStockpile();
            switch (buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    if (!isFirstTownCenter)
                    {
                        cost.Wood = TownCenterWoodCost;
                    }

                    break;
                case BuildingTypeId.Wall:
                    cost.Wood = WallWoodCost;
                    break;
                case BuildingTypeId.TradePost:
                    cost.Wood = TradePostWoodCost;
                    cost.Gold = TradePostGoldCost;
                    break;
            }

            return cost;
        }

        public static int GetUnitPopulation(UnitTypeId unitTypeId)
        {
            switch (unitTypeId)
            {
                case UnitTypeId.Villager:
                    return 1;
                case UnitTypeId.Scout:
                    return 1;
                case UnitTypeId.Infantry:
                    return 1;
                case UnitTypeId.Cavalry:
                    return 2;
                case UnitTypeId.SiegeCannon:
                    return 3;
                case UnitTypeId.TradeCart:
                    return 1;
                case UnitTypeId.Mangonel:
                    return 3;
                default:
                    return 0;
            }
        }

        public static int GetUnitHitPoints(UnitTypeId unitTypeId)
        {
            switch (unitTypeId)
            {
                case UnitTypeId.Villager:
                    return VillagerHitPoints;
                case UnitTypeId.Scout:
                    return ScoutHitPoints;
                case UnitTypeId.Infantry:
                    return InfantryHitPoints;
                case UnitTypeId.Cavalry:
                    return CavalryHitPoints;
                case UnitTypeId.SiegeCannon:
                    return SiegeCannonHitPoints;
                case UnitTypeId.TradeCart:
                    return TradeCartHitPoints;
                case UnitTypeId.Mangonel:
                    return MangonelHitPoints;
                default:
                    return 1;
            }
        }

        public static int GetUnitTrainTicks(UnitTypeId unitTypeId)
        {
            switch (unitTypeId)
            {
                case UnitTypeId.Villager:
                    return VillagerTrainTicks;
                case UnitTypeId.Infantry:
                    return InfantryTrainTicks;
                case UnitTypeId.Cavalry:
                    return CavalryTrainTicks;
                case UnitTypeId.SiegeCannon:
                    return SiegeCannonTrainTicks;
                case UnitTypeId.TradeCart:
                    return TradeCartTrainTicks;
                case UnitTypeId.Mangonel:
                    return MangonelTrainTicks;
                default:
                    return 1;
            }
        }

        public static ResourceStockpile GetUnitCost(UnitTypeId unitTypeId)
        {
            var cost = new ResourceStockpile();
            switch (unitTypeId)
            {
                case UnitTypeId.Villager:
                    cost.Food = VillagerFoodCost;
                    break;
                case UnitTypeId.Infantry:
                    cost.Food = InfantryFoodCost;
                    break;
                case UnitTypeId.Cavalry:
                    cost.Food = CavalryFoodCost;
                    cost.Gold = CavalryGoldCost;
                    break;
                case UnitTypeId.SiegeCannon:
                    cost.Wood = SiegeCannonWoodCost;
                    cost.Gold = SiegeCannonGoldCost;
                    break;
                case UnitTypeId.Mangonel:
                    cost.Wood = MangonelWoodCost;
                    cost.Gold = MangonelGoldCost;
                    break;
                case UnitTypeId.TradeCart:
                    cost.Wood = TradeCartWoodCost;
                    cost.Gold = TradeCartGoldCost;
                    break;
            }

            return cost;
        }

        public static int GetResearchTicks(TechId techId)
        {
            switch (techId)
            {
                case TechId.InfantryAttack1:
                    return InfantryAttack1ResearchTicks;
                default:
                    return 0;
            }
        }

        public static ResourceStockpile GetResearchCost(TechId techId)
        {
            var cost = new ResourceStockpile();
            switch (techId)
            {
                case TechId.InfantryAttack1:
                    cost.Food = InfantryAttack1FoodCost;
                    cost.Gold = InfantryAttack1GoldCost;
                    break;
            }

            return cost;
        }

        public static bool CanResearch(BuildingTypeId buildingTypeId, TechId techId)
        {
            return buildingTypeId == BuildingTypeId.TownCenter
                && techId == TechId.InfantryAttack1;
        }

        public static bool CanTrain(BuildingTypeId buildingTypeId, UnitTypeId unitTypeId)
        {
            return (buildingTypeId == BuildingTypeId.TownCenter
                    && (unitTypeId == UnitTypeId.Villager || unitTypeId == UnitTypeId.Infantry || unitTypeId == UnitTypeId.Cavalry || unitTypeId == UnitTypeId.SiegeCannon || unitTypeId == UnitTypeId.Mangonel))
                || (buildingTypeId == BuildingTypeId.TradePost && unitTypeId == UnitTypeId.TradeCart);
        }

        public static Determinism.Fixed GetUnitMoveSpeed(UnitTypeId unitTypeId)
        {
            switch (unitTypeId)
            {
                case UnitTypeId.Scout:
                    return Determinism.Fixed.FromRatio(ScoutMoveSpeedTilesPerTickNumerator, ScoutMoveSpeedTilesPerTickDenominator);
                case UnitTypeId.Infantry:
                    return Determinism.Fixed.FromRatio(InfantryMoveSpeedTilesPerTickNumerator, InfantryMoveSpeedTilesPerTickDenominator);
                case UnitTypeId.Cavalry:
                    return Determinism.Fixed.FromRatio(CavalryMoveSpeedTilesPerTickNumerator, CavalryMoveSpeedTilesPerTickDenominator);
                case UnitTypeId.SiegeCannon:
                    return Determinism.Fixed.FromRatio(SiegeCannonMoveSpeedTilesPerTickNumerator, SiegeCannonMoveSpeedTilesPerTickDenominator);
                case UnitTypeId.Mangonel:
                    return Determinism.Fixed.FromRatio(MangonelMoveSpeedTilesPerTickNumerator, MangonelMoveSpeedTilesPerTickDenominator);
                case UnitTypeId.TradeCart:
                    return Determinism.Fixed.FromRatio(TradeCartMoveSpeedTilesPerTickNumerator, TradeCartMoveSpeedTilesPerTickDenominator);
                case UnitTypeId.Villager:
                    return Determinism.Fixed.FromRatio(VillagerMoveSpeedTilesPerTickNumerator, VillagerMoveSpeedTilesPerTickDenominator);
                default:
                    return Determinism.Fixed.FromInt(0);
            }
        }

        public static int GetUnitSightRadius(UnitTypeId unitTypeId)
        {
            switch (unitTypeId)
            {
                case UnitTypeId.Scout:
                    return ScoutSightRadiusTiles;
                case UnitTypeId.Infantry:
                    return InfantrySightRadiusTiles;
                case UnitTypeId.Cavalry:
                    return CavalrySightRadiusTiles;
                case UnitTypeId.SiegeCannon:
                    return SiegeCannonSightRadiusTiles;
                case UnitTypeId.Mangonel:
                    return MangonelSightRadiusTiles;
                case UnitTypeId.TradeCart:
                    return TradeCartSightRadiusTiles;
                case UnitTypeId.Villager:
                    return VillagerSightRadiusTiles;
                default:
                    return 0;
            }
        }

        public static int GetBuildingSightRadius(BuildingTypeId buildingTypeId)
        {
            switch (buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    return TownCenterSightRadiusTiles;
                case BuildingTypeId.TradePost:
                    return TradePostSightRadiusTiles;
                default:
                    return 0;
            }
        }

        public static int GetBuildingCompletedHitPoints(BuildingTypeId buildingTypeId, bool isCapital)
        {
            switch (buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    return TownCenterHitPoints + (isCapital ? CapitalHitPointBonus : 0);
                case BuildingTypeId.Wall:
                    return WallHitPoints;
                case BuildingTypeId.TradePost:
                    return TradePostHitPoints;
                default:
                    return 1;
            }
        }

        public static int GetUnitAttackDamage(UnitTypeId unitTypeId)
        {
            switch (unitTypeId)
            {
                case UnitTypeId.Infantry:
                    return InfantryAttackDamage;
                case UnitTypeId.Cavalry:
                    return CavalryAttackDamage;
                case UnitTypeId.Scout:
                    return ScoutAttackDamage;
                case UnitTypeId.Villager:
                    return VillagerAttackDamage;
                case UnitTypeId.SiegeCannon:
                case UnitTypeId.Mangonel:
                    return 0;
                default:
                    return 0;
            }
        }

        public static int GetUnitAttackCooldownTicks(UnitTypeId unitTypeId)
        {
            switch (unitTypeId)
            {
                case UnitTypeId.Infantry:
                    return InfantryAttackCooldownTicks;
                case UnitTypeId.Cavalry:
                    return CavalryAttackCooldownTicks;
                case UnitTypeId.Scout:
                    return ScoutAttackCooldownTicks;
                case UnitTypeId.Villager:
                    return VillagerAttackCooldownTicks;
                default:
                    return 1;
            }
        }

        public static Determinism.Fixed GetUnitAttackRange(UnitTypeId unitTypeId)
        {
            return Determinism.Fixed.FromInt(1);
        }

        public static bool IsSiege(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.SiegeCannon;
        }

        public static bool IsAreaDamage(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.Mangonel;
        }

        public static int GetAreaDamage(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.Mangonel ? MangonelAreaDamage : 0;
        }

        public static Determinism.Fixed GetAreaDamageRadius(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.Mangonel ? Determinism.Fixed.FromInt(MangonelAreaRadiusTiles) : Determinism.Fixed.FromInt(0);
        }

        public static int GetAreaDamageCooldownTicks(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.Mangonel ? MangonelAreaCooldownTicks : 0;
        }

        public static Determinism.Fixed GetAreaDamageAttackRange(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.Mangonel ? Determinism.Fixed.FromInt(MangonelAreaRangeTiles) : Determinism.Fixed.FromInt(0);
        }

        public static bool CanAreaDamageHitUnits(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.Mangonel;
        }

        public static bool CanAreaDamageHitBuildings(UnitTypeId unitTypeId)
        {
            return false;
        }

        public static int GetSiegeSetupTicks(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.SiegeCannon ? SiegeCannonSetupTicks : 0;
        }

        public static int GetSiegeReloadTicks(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.SiegeCannon ? SiegeCannonReloadTicks : 0;
        }

        public static int GetSiegeBuildingDamage(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.SiegeCannon ? SiegeCannonBuildingDamage : 0;
        }

        public static Determinism.Fixed GetSiegeAttackRange(UnitTypeId unitTypeId)
        {
            return unitTypeId == UnitTypeId.SiegeCannon ? Determinism.Fixed.FromInt(SiegeCannonRangeTiles) : Determinism.Fixed.FromInt(0);
        }
    }
}
