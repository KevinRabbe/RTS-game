using System.Collections.Generic;

namespace RtsGame.Presentation.Visuals
{
    public sealed class VisualFrame
    {
        public int Tick { get; }
        public IReadOnlyList<VisualPrimitive> Primitives { get; }

        public VisualFrame(int tick, IReadOnlyList<VisualPrimitive> primitives)
        {
            Tick = tick;
            Primitives = primitives;
        }
    }
}
