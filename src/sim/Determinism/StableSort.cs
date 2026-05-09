using System;
using System.Collections.Generic;

namespace RtsGame.Sim.Determinism
{
    public static class StableSort
    {
        public static List<T> Sorted<T>(IEnumerable<T> values, Comparison<T> comparison)
        {
            var indexed = new List<IndexedValue<T>>();
            int index = 0;
            foreach (T value in values)
            {
                indexed.Add(new IndexedValue<T>(value, index));
                index++;
            }

            indexed.Sort((left, right) =>
            {
                int result = comparison(left.Value, right.Value);
                return result != 0 ? result : left.Index.CompareTo(right.Index);
            });

            var sorted = new List<T>(indexed.Count);
            foreach (IndexedValue<T> item in indexed)
            {
                sorted.Add(item.Value);
            }

            return sorted;
        }

        private readonly struct IndexedValue<T>
        {
            public readonly T Value;
            public readonly int Index;

            public IndexedValue(T value, int index)
            {
                Value = value;
                Index = index;
            }
        }
    }
}
