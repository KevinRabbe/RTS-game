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

    public readonly struct GodotSelectionEditResult
    {
        public GodotSelectionEditResult(int[] selectedUnitIds, int selectedBuildingId, bool changed)
        {
            SelectedUnitIds = selectedUnitIds;
            SelectedBuildingId = selectedBuildingId;
            Changed = changed;
        }

        public int[] SelectedUnitIds { get; }
        public int SelectedBuildingId { get; }
        public bool Changed { get; }
    }

    public static class GodotSelectionRouter
    {
        public static int[] SelectOwnedUnitsOfSameTypeAt(GodotFrameDto frame, int localPlayerIndex, long xRaw, long yRaw)
        {
            GodotSelectionResult selection = SelectAt(frame, localPlayerIndex, xRaw, yRaw);
            if (selection.Kind != GodotSelectionKind.Unit)
            {
                return new int[0];
            }

            int selectedUnitTypeId = 0;
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind == (int)VisualPrimitiveKind.UnitSquare
                    && primitive.OwnerPlayerIndex == localPlayerIndex
                    && primitive.EntityId == selection.EntityId)
                {
                    selectedUnitTypeId = primitive.TypeId;
                    break;
                }
            }

            if (selectedUnitTypeId == 0)
            {
                return new[] { selection.EntityId };
            }

            var selected = new List<int>();
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.UnitSquare
                    || primitive.OwnerPlayerIndex != localPlayerIndex
                    || primitive.TypeId != selectedUnitTypeId)
                {
                    continue;
                }

                selected.Add(primitive.EntityId);
            }

            selected.Sort();
            return selected.ToArray();
        }

        public static GodotSelectionEditResult ResolveClickSelection(
            IReadOnlyList<int> currentSelectedUnitIds,
            int currentSelectedBuildingId,
            GodotSelectionResult clickSelection,
            bool additive)
        {
            if (!additive)
            {
                if (clickSelection.Kind == GodotSelectionKind.Unit)
                {
                    return new GodotSelectionEditResult(new[] { clickSelection.EntityId }, 0, true);
                }

                if (clickSelection.Kind == GodotSelectionKind.Building)
                {
                    return new GodotSelectionEditResult(new int[0], clickSelection.EntityId, true);
                }

                bool hadSelection = currentSelectedUnitIds.Count > 0 || currentSelectedBuildingId != 0;
                return new GodotSelectionEditResult(new int[0], 0, hadSelection);
            }

            if (clickSelection.Kind == GodotSelectionKind.Unit)
            {
                var next = new List<int>(currentSelectedUnitIds.Count + 1);
                for (int i = 0; i < currentSelectedUnitIds.Count; i++)
                {
                    next.Add(currentSelectedUnitIds[i]);
                }

                bool removed = next.Remove(clickSelection.EntityId);
                if (!removed)
                {
                    next.Add(clickSelection.EntityId);
                }

                next.Sort();
                return new GodotSelectionEditResult(next.ToArray(), 0, true);
            }

            if (clickSelection.Kind == GodotSelectionKind.Building)
            {
                bool changed = currentSelectedBuildingId != clickSelection.EntityId || currentSelectedUnitIds.Count > 0;
                return new GodotSelectionEditResult(new int[0], clickSelection.EntityId, changed);
            }

            return new GodotSelectionEditResult(ToArray(currentSelectedUnitIds), currentSelectedBuildingId, false);
        }

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

        public static GodotSelectionEditResult ResolveRectangleSelection(
            IReadOnlyList<int> currentSelectedUnitIds,
            int currentSelectedBuildingId,
            IReadOnlyList<int> rectangleSelectedUnitIds,
            bool additive)
        {
            if (!additive)
            {
                int[] next = ToArray(rectangleSelectedUnitIds);
                return new GodotSelectionEditResult(next, 0, true);
            }

            if (rectangleSelectedUnitIds.Count == 0)
            {
                return new GodotSelectionEditResult(ToArray(currentSelectedUnitIds), currentSelectedBuildingId, false);
            }

            var nextSelection = new List<int>(currentSelectedUnitIds.Count + rectangleSelectedUnitIds.Count);
            for (int i = 0; i < currentSelectedUnitIds.Count; i++)
            {
                nextSelection.Add(currentSelectedUnitIds[i]);
            }

            bool changed = false;
            for (int i = 0; i < rectangleSelectedUnitIds.Count; i++)
            {
                int unitId = rectangleSelectedUnitIds[i];
                bool removed = nextSelection.Remove(unitId);
                if (removed)
                {
                    changed = true;
                    continue;
                }

                nextSelection.Add(unitId);
                changed = true;
            }

            nextSelection.Sort();
            return new GodotSelectionEditResult(nextSelection.ToArray(), 0, changed || currentSelectedBuildingId != 0);
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

        private static int[] ToArray(IReadOnlyList<int> values)
        {
            var result = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return result;
        }
    }
}
