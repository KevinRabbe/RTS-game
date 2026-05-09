using RtsGame.Presentation.Visuals;

namespace RtsGame.Presentation.GodotBridge
{
    public enum GodotVisualStyle
    {
        LocalUnitDefault = 1,
        LocalVillager = 2,
        LocalScout = 3,
        LocalInfantry = 4,
        LocalCavalry = 5,
        EnemyUnit = 6,
        NormalBuilding = 7,
        CapitalBuilding = 8,
        Wall = 9,
        FoodResource = 10,
        WoodResource = 11,
        GoldResource = 12
    }

    public static class GodotVisualStyleResolver
    {
        private const int VillagerUnitTypeId = 1;
        private const int ScoutUnitTypeId = 2;
        private const int InfantryUnitTypeId = 3;
        private const int CavalryUnitTypeId = 7;

        public static GodotVisualStyle ResolveUnit(GodotPrimitiveDto primitive, int localPlayerIndex)
        {
            if (primitive.OwnerPlayerIndex != localPlayerIndex)
            {
                return GodotVisualStyle.EnemyUnit;
            }

            if (primitive.TypeId == VillagerUnitTypeId)
            {
                return GodotVisualStyle.LocalVillager;
            }

            if (primitive.TypeId == ScoutUnitTypeId)
            {
                return GodotVisualStyle.LocalScout;
            }

            if (primitive.TypeId == InfantryUnitTypeId)
            {
                return GodotVisualStyle.LocalInfantry;
            }

            if (primitive.TypeId == CavalryUnitTypeId)
            {
                return GodotVisualStyle.LocalCavalry;
            }

            return GodotVisualStyle.LocalUnitDefault;
        }

        public static GodotVisualStyle ResolveBuilding(GodotPrimitiveDto primitive)
        {
            if (primitive.Kind == (int)VisualPrimitiveKind.WallRectangle)
            {
                return GodotVisualStyle.Wall;
            }

            return primitive.IsCapital ? GodotVisualStyle.CapitalBuilding : GodotVisualStyle.NormalBuilding;
        }

        public static GodotVisualStyle ResolveResource(GodotPrimitiveDto primitive)
        {
            if (primitive.Kind == (int)VisualPrimitiveKind.WoodResourceCircle)
            {
                return GodotVisualStyle.WoodResource;
            }

            if (primitive.Kind == (int)VisualPrimitiveKind.GoldResourceCircle)
            {
                return GodotVisualStyle.GoldResource;
            }

            return GodotVisualStyle.FoodResource;
        }
    }
}
