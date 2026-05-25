using System;

namespace RtsGame.Presentation.GodotBridge
{
    public sealed class GodotControlGroupRecallTracker
    {
        private readonly int[] _lastRecallTickByGroup = new int[10];
        private readonly int _doubleTapWindowTicks;

        public GodotControlGroupRecallTracker(int doubleTapWindowTicks = 24)
        {
            _doubleTapWindowTicks = Math.Max(1, doubleTapWindowTicks);
            Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < _lastRecallTickByGroup.Length; i++)
            {
                _lastRecallTickByGroup[i] = int.MinValue;
            }
        }

        public bool ResolveDoubleTap(int groupIndex, int tick)
        {
            if (groupIndex < 1 || groupIndex > 9)
            {
                return false;
            }

            int lastTick = _lastRecallTickByGroup[groupIndex];
            bool isDoubleTap = lastTick != int.MinValue && tick >= lastTick && tick - lastTick <= _doubleTapWindowTicks;
            _lastRecallTickByGroup[groupIndex] = isDoubleTap ? int.MinValue : tick;
            return isDoubleTap;
        }
    }
}
