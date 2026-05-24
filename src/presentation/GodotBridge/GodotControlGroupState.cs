using System;
using System.Collections.Generic;

namespace RtsGame.Presentation.GodotBridge
{
    public sealed class GodotControlGroupState
    {
        private readonly int[][] _groups = new int[10][];

        public void Reset()
        {
            for (int i = 0; i < _groups.Length; i++)
            {
                _groups[i] = Array.Empty<int>();
            }
        }

        public void Assign(int groupIndex, IReadOnlyList<int> unitIds)
        {
            if (groupIndex < 1 || groupIndex > 9)
            {
                return;
            }

            if (unitIds.Count == 0)
            {
                _groups[groupIndex] = Array.Empty<int>();
                return;
            }

            var deduped = new List<int>(unitIds.Count);
            int last = int.MinValue;
            for (int i = 0; i < unitIds.Count; i++)
            {
                int id = unitIds[i];
                if (id <= 0 || id == last)
                {
                    continue;
                }

                deduped.Add(id);
                last = id;
            }

            _groups[groupIndex] = deduped.Count == 0 ? Array.Empty<int>() : deduped.ToArray();
        }

        public int[] Recall(int groupIndex)
        {
            if (groupIndex < 1 || groupIndex > 9)
            {
                return Array.Empty<int>();
            }

            int[] group = _groups[groupIndex];
            if (group == null || group.Length == 0)
            {
                return Array.Empty<int>();
            }

            int[] copy = new int[group.Length];
            Array.Copy(group, copy, group.Length);
            return copy;
        }
    }
}

