using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Data;

namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotPrimitiveHitTest
    {
        public static bool ContainsPoint(GodotPrimitiveDto primitive, long xRaw, long yRaw)
        {
            long halfSize = primitive.SizeRaw / 2;
            return xRaw >= primitive.XRaw - halfSize
                && xRaw <= primitive.XRaw + halfSize
                && yRaw >= primitive.YRaw - halfSize
                && yRaw <= primitive.YRaw + halfSize;
        }

        public static bool ContainsPointForInteraction(GodotPrimitiveDto primitive, long xRaw, long yRaw)
        {
            long halfSize = primitive.SizeRaw / 2;
            long interactionHalfSize = halfSize;

            switch ((VisualPrimitiveKind)primitive.Kind)
            {
                case VisualPrimitiveKind.BuildingRectangle:
                case VisualPrimitiveKind.WallRectangle:
                    interactionHalfSize = halfSize * 2;
                    if (primitive.TypeId == (int)BuildingTypeId.TownCenter)
                    {
                        interactionHalfSize = halfSize * 3;
                    }
                    if (primitive.IsCapital)
                    {
                        interactionHalfSize = interactionHalfSize * 11 / 10;
                    }
                    break;
                case VisualPrimitiveKind.UnitSquare:
                    interactionHalfSize = halfSize * 3 / 2;
                    break;
                case VisualPrimitiveKind.FoodResourceCircle:
                case VisualPrimitiveKind.WoodResourceCircle:
                case VisualPrimitiveKind.GoldResourceCircle:
                    interactionHalfSize = halfSize * 2;
                    break;
            }

            return xRaw >= primitive.XRaw - interactionHalfSize
                && xRaw <= primitive.XRaw + interactionHalfSize
                && yRaw >= primitive.YRaw - interactionHalfSize
                && yRaw <= primitive.YRaw + interactionHalfSize;
        }
    }
}
