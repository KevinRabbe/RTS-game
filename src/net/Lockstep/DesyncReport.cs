namespace RtsGame.Net.Lockstep
{
    public sealed class DesyncReport
    {
        public int Tick { get; }
        public int FirstPlayerIndex { get; }
        public ulong FirstChecksum { get; }
        public int SecondPlayerIndex { get; }
        public ulong SecondChecksum { get; }

        public DesyncReport(int tick, int firstPlayerIndex, ulong firstChecksum, int secondPlayerIndex, ulong secondChecksum)
        {
            Tick = tick;
            FirstPlayerIndex = firstPlayerIndex;
            FirstChecksum = firstChecksum;
            SecondPlayerIndex = secondPlayerIndex;
            SecondChecksum = secondChecksum;
        }
    }
}
