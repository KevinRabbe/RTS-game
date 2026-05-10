using RtsGame.Presentation.Visuals;

namespace RtsGame.Presentation.GodotBridge
{
    public enum GodotInteractionTargetKind
    {
        None = 0,
        Unit = 1,
        Building = 2,
        Resource = 3
    }

    public readonly struct GodotInteractionProbeResult
    {
        public GodotInteractionProbeResult(GodotInteractionTargetKind targetKind, int targetEntityId)
        {
            TargetKind = targetKind;
            TargetEntityId = targetEntityId;
        }

        public GodotInteractionTargetKind TargetKind { get; }
        public int TargetEntityId { get; }
    }

    public static class GodotInteractionProbe
    {
        public static GodotInteractionProbeResult Probe(GodotFrameDto frame, int localPlayerIndex, long xRaw, long yRaw)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.UnitSquare)
                {
                    continue;
                }

                if (GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, xRaw, yRaw))
                {
                    return new GodotInteractionProbeResult(GodotInteractionTargetKind.Unit, primitive.EntityId);
                }
            }

            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.BuildingRectangle
                    && primitive.Kind != (int)VisualPrimitiveKind.WallRectangle)
                {
                    continue;
                }

                if (GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, xRaw, yRaw))
                {
                    return new GodotInteractionProbeResult(GodotInteractionTargetKind.Building, primitive.EntityId);
                }
            }

            int resourceId = GodotInteractionRouter.FindResourceAt(frame, xRaw, yRaw);
            if (resourceId != 0)
            {
                return new GodotInteractionProbeResult(GodotInteractionTargetKind.Resource, resourceId);
            }

            return new GodotInteractionProbeResult(GodotInteractionTargetKind.None, 0);
        }
    }
}
