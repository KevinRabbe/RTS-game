namespace RtsGame.Net.Lockstep
{
    public readonly struct InputDelay
    {
        public int Ticks { get; }

        public InputDelay(int ticks)
        {
            Ticks = ticks < 0 ? 0 : ticks;
        }
    }
}
