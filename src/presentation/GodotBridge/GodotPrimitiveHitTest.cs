using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotPrimitiveHitTest
    {
        public static bool ContainsPoint(GodotPrimitiveDto primitive, long xRaw, long yRaw)
        {
            long halfWidth = primitive.WidthRaw / 2;
            long halfHeight = primitive.HeightRaw / 2;
            return xRaw >= primitive.XRaw - halfWidth
                && xRaw <= primitive.XRaw + halfWidth
                && yRaw >= primitive.YRaw - halfHeight
                && yRaw <= primitive.YRaw + halfHeight;
        }

        public static bool ContainsPointForInteraction(GodotPrimitiveDto primitive, long xRaw, long yRaw)
        {
            long halfWidth = primitive.WidthRaw / 2;
            long halfHeight = primitive.HeightRaw / 2;
            long interactionHalfWidth = halfWidth;
            long interactionHalfHeight = halfHeight;
            long oneTile = Fixed.FromInt(1).Raw;
            long halfTile = Fixed.FromRatio(1, 2).Raw;

            switch ((VisualPrimitiveKind)primitive.Kind)
            {
                case VisualPrimitiveKind.BuildingRectangle:
                case VisualPrimitiveKind.WallRectangle:
                    interactionHalfWidth = halfWidth + oneTile;
                    interactionHalfHeight = halfHeight + oneTile;
                    break;
                case VisualPrimitiveKind.UnitSquare:
                    interactionHalfWidth = halfWidth + halfTile;
                    interactionHalfHeight = halfHeight + halfTile;
                    break;
                case VisualPrimitiveKind.FoodResourceCircle:
                case VisualPrimitiveKind.WoodResourceCircle:
                case VisualPrimitiveKind.GoldResourceCircle:
                    interactionHalfWidth = halfWidth + halfTile;
                    interactionHalfHeight = halfHeight + halfTile;
                    break;
            }

            return xRaw >= primitive.XRaw - interactionHalfWidth
                && xRaw <= primitive.XRaw + interactionHalfWidth
                && yRaw >= primitive.YRaw - interactionHalfHeight
                && yRaw <= primitive.YRaw + interactionHalfHeight;
        }
    }
}
