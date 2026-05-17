using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

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
            long oneTile = Fixed.FromInt(1).Raw;
            long halfTile = Fixed.FromRatio(1, 2).Raw;

            switch ((VisualPrimitiveKind)primitive.Kind)
            {
                case VisualPrimitiveKind.BuildingRectangle:
                case VisualPrimitiveKind.WallRectangle:
                    interactionHalfSize = halfSize + oneTile;
                    break;
                case VisualPrimitiveKind.UnitSquare:
                    interactionHalfSize = halfSize + halfTile;
                    break;
                case VisualPrimitiveKind.FoodResourceCircle:
                case VisualPrimitiveKind.WoodResourceCircle:
                case VisualPrimitiveKind.GoldResourceCircle:
                    interactionHalfSize = halfSize + halfTile;
                    break;
            }

            return xRaw >= primitive.XRaw - interactionHalfSize
                && xRaw <= primitive.XRaw + interactionHalfSize
                && yRaw >= primitive.YRaw - interactionHalfSize
                && yRaw <= primitive.YRaw + interactionHalfSize;
        }
    }
}
