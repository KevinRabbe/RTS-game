using RtsGame.Presentation.Visuals;

namespace RtsGame.Presentation.GodotBridge
{
    public enum GodotSelectionKind
    {
        None = 0,
        Unit = 1,
        Building = 2
    }

    public readonly struct GodotSelectionResult
    {
        public GodotSelectionKind Kind { get; }
        public int EntityId { get; }

        public GodotSelectionResult(GodotSelectionKind kind, int entityId)
        {
            Kind = kind;
            EntityId = entityId;
        }
    }

    public static class GodotSelectionRouter
    {
        public static GodotSelectionResult SelectAt(GodotFrameDto frame, int localPlayerIndex, long xRaw, long yRaw)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.UnitSquare || primitive.OwnerPlayerIndex != localPlayerIndex)
                {
                    continue;
                }

                if (GodotPrimitiveHitTest.ContainsPoint(primitive, xRaw, yRaw))
                {
                    return new GodotSelectionResult(GodotSelectionKind.Unit, primitive.EntityId);
                }
            }

            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if ((primitive.Kind != (int)VisualPrimitiveKind.BuildingRectangle
                        && primitive.Kind != (int)VisualPrimitiveKind.WallRectangle)
                    || primitive.OwnerPlayerIndex != localPlayerIndex)
                {
                    continue;
                }

                if (GodotPrimitiveHitTest.ContainsPoint(primitive, xRaw, yRaw))
                {
                    return new GodotSelectionResult(GodotSelectionKind.Building, primitive.EntityId);
                }
            }

            return new GodotSelectionResult(GodotSelectionKind.None, 0);
        }
    }
}
