using System.Collections.Generic;
using RtsGame.Presentation.Visuals;

namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotControlGroupResolver
    {
        public static int[] FilterRecallableLocalUnitIds(GodotFrameDto? frame, int localPlayerIndex, IReadOnlyList<int> storedIds)
        {
            if (frame == null || storedIds.Count == 0)
            {
                return new int[0];
            }

            var statusById = new Dictionary<int, GodotUnitStatusDto>(frame.UnitStatuses.Length);
            for (int i = 0; i < frame.UnitStatuses.Length; i++)
            {
                statusById[frame.UnitStatuses[i].UnitId] = frame.UnitStatuses[i];
            }

            var localUnitPrimitiveIds = new HashSet<int>();
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if ((VisualPrimitiveKind)primitive.Kind == VisualPrimitiveKind.UnitSquare
                    && primitive.OwnerPlayerIndex == localPlayerIndex)
                {
                    localUnitPrimitiveIds.Add(primitive.EntityId);
                }
            }

            var filtered = new List<int>(storedIds.Count);
            for (int i = 0; i < storedIds.Count; i++)
            {
                int id = storedIds[i];
                if (id <= 0)
                {
                    continue;
                }

                if (!localUnitPrimitiveIds.Contains(id))
                {
                    continue;
                }

                if (!statusById.TryGetValue(id, out GodotUnitStatusDto status))
                {
                    continue;
                }

                if (status.CurrentHitPoints <= 0)
                {
                    continue;
                }

                filtered.Add(id);
            }

            return filtered.ToArray();
        }
    }
}
