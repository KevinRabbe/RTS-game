using System.Collections.Generic;
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
        public static int[] SelectUnitsInRectangle(GodotFrameDto frame, int localPlayerIndex, long leftRaw, long topRaw, long rightRaw, long bottomRaw)
        {
            long minX = leftRaw < rightRaw ? leftRaw : rightRaw;
            long maxX = leftRaw > rightRaw ? leftRaw : rightRaw;
            long minY = topRaw < bottomRaw ? topRaw : bottomRaw;
            long maxY = topRaw > bottomRaw ? topRaw : bottomRaw;

            var selected = new List<int>();
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.UnitSquare || primitive.OwnerPlayerIndex != localPlayerIndex)
                {
                    continue;
                }

                if (primitive.XRaw < minX || primitive.XRaw > maxX || primitive.YRaw < minY || primitive.YRaw > maxY)
                {
                    continue;
                }

                selected.Add(primitive.EntityId);
            }

            selected.Sort();
            return selected.ToArray();
        }

        public static GodotSelectionResult SelectAt(GodotFrameDto frame, int localPlayerIndex, long xRaw, long yRaw)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.UnitSquare || primitive.OwnerPlayerIndex != localPlayerIndex)
                {
                    continue;
                }

                if (GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, xRaw, yRaw))
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

                if (GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, xRaw, yRaw))
                {
                    return new GodotSelectionResult(GodotSelectionKind.Building, primitive.EntityId);
                }
            }

            return new GodotSelectionResult(GodotSelectionKind.None, 0);
        }
    }
}
