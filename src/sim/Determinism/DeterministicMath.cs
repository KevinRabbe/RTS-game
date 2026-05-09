namespace RtsGame.Sim.Determinism
{
    public static class DeterministicMath
    {
        public static long SqrtRaw(long value)
        {
            if (value <= 0)
            {
                return 0;
            }

            ulong n = (ulong)value;
            ulong result = 0;
            ulong bit = 1UL << 62;

            while (bit > n)
            {
                bit >>= 2;
            }

            while (bit != 0)
            {
                if (n >= result + bit)
                {
                    n -= result + bit;
                    result = (result >> 1) + bit;
                }
                else
                {
                    result >>= 1;
                }

                bit >>= 2;
            }

            return (long)result;
        }
    }
}
