using RtsGame.Presentation.Visuals;

namespace RtsGame.Presentation.GodotBridge
{
    public enum GodotPrimitiveDrawKind
    {
        None = 0,
        Unit = 1,
        Building = 2,
        TradeRoute = 3,
        HealthBar = 4,
        FogOverlay = 5,
        Resource = 6
    }

    public static class GodotPrimitiveDrawKindResolver
    {
        public static GodotPrimitiveDrawKind Resolve(GodotPrimitiveDto primitive)
        {
            switch ((VisualPrimitiveKind)primitive.Kind)
            {
                case VisualPrimitiveKind.UnitSquare:
                    return GodotPrimitiveDrawKind.Unit;
                case VisualPrimitiveKind.BuildingRectangle:
                case VisualPrimitiveKind.WallRectangle:
                    return GodotPrimitiveDrawKind.Building;
                case VisualPrimitiveKind.TradeRouteLine:
                    return GodotPrimitiveDrawKind.TradeRoute;
                case VisualPrimitiveKind.HealthBar:
                    return GodotPrimitiveDrawKind.HealthBar;
                case VisualPrimitiveKind.FogOverlay:
                    return GodotPrimitiveDrawKind.FogOverlay;
                case VisualPrimitiveKind.FoodResourceCircle:
                case VisualPrimitiveKind.WoodResourceCircle:
                case VisualPrimitiveKind.GoldResourceCircle:
                    return GodotPrimitiveDrawKind.Resource;
                default:
                    return GodotPrimitiveDrawKind.None;
            }
        }
    }
}
