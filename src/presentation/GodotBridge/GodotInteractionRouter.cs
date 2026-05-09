using RtsGame.Presentation.Visuals;

namespace RtsGame.Presentation.GodotBridge
{
    public enum GodotInteractionIntentKind
    {
        None = 0,
        Move = 1,
        GatherResource = 2,
        Attack = 3
    }

    public readonly struct GodotInteractionIntent
    {
        public GodotInteractionIntentKind Kind { get; }
        public int TargetEntityId { get; }
        public int ResourceNodeId { get; }

        public GodotInteractionIntent(GodotInteractionIntentKind kind, int targetEntityId, int resourceNodeId)
        {
            Kind = kind;
            TargetEntityId = targetEntityId;
            ResourceNodeId = resourceNodeId;
        }
    }

    public static class GodotInteractionRouter
    {
        public static GodotInteractionIntent RouteRightClick(GodotFrameDto frame, int localPlayerIndex, bool hasSelectedUnits, long xRaw, long yRaw)
        {
            if (!hasSelectedUnits)
            {
                return new GodotInteractionIntent(GodotInteractionIntentKind.None, 0, 0);
            }

            int targetId = FindEnemyTargetAt(frame, localPlayerIndex, xRaw, yRaw);
            if (targetId != 0)
            {
                return new GodotInteractionIntent(GodotInteractionIntentKind.Attack, targetId, 0);
            }

            int resourceId = FindResourceAt(frame, xRaw, yRaw);
            if (resourceId != 0)
            {
                return new GodotInteractionIntent(GodotInteractionIntentKind.GatherResource, 0, resourceId);
            }

            return new GodotInteractionIntent(GodotInteractionIntentKind.Move, 0, 0);
        }

        public static int FindResourceAt(GodotFrameDto frame, long xRaw, long yRaw)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.FoodResourceCircle
                    && primitive.Kind != (int)VisualPrimitiveKind.WoodResourceCircle
                    && primitive.Kind != (int)VisualPrimitiveKind.GoldResourceCircle)
                {
                    continue;
                }

                if (ContainsPoint(primitive, xRaw, yRaw))
                {
                    return primitive.EntityId;
                }
            }

            return 0;
        }

        public static int FindEnemyTargetAt(GodotFrameDto frame, int localPlayerIndex, long xRaw, long yRaw)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if ((primitive.Kind != (int)VisualPrimitiveKind.UnitSquare
                        && primitive.Kind != (int)VisualPrimitiveKind.BuildingRectangle
                        && primitive.Kind != (int)VisualPrimitiveKind.WallRectangle)
                    || primitive.OwnerPlayerIndex == localPlayerIndex
                    || primitive.OwnerPlayerIndex < 0)
                {
                    continue;
                }

                if (ContainsPoint(primitive, xRaw, yRaw))
                {
                    return primitive.EntityId;
                }
            }

            return 0;
        }

        private static bool ContainsPoint(GodotPrimitiveDto primitive, long xRaw, long yRaw)
        {
            long halfSize = primitive.SizeRaw / 2;
            return xRaw >= primitive.XRaw - halfSize
                && xRaw <= primitive.XRaw + halfSize
                && yRaw >= primitive.YRaw - halfSize
                && yRaw <= primitive.YRaw + halfSize;
        }
    }
}
