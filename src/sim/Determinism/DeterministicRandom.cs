namespace RtsGame.Sim.Determinism
{
    public struct DeterministicRandomState
    {
        public ulong Value;

        public DeterministicRandomState(ulong seed)
        {
            Value = seed == 0UL ? 0x9E3779B97F4A7C15UL : seed;
        }
    }

    public static class DeterministicRandom
    {
        public static uint NextUInt32(ref DeterministicRandomState state)
        {
            ulong x = state.Value;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            state.Value = x;
            return (uint)((x * 0x2545F4914F6CDD1DUL) >> 32);
        }

        public static int Range(ref DeterministicRandomState state, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt32(ref state) % span);
        }
    }
}
