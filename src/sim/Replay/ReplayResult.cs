using System.Collections.Generic;

namespace RtsGame.Sim.Replay
{
    public sealed class ReplayResult
    {
        public int FinalTick { get; }
        public ulong FinalChecksum { get; }
        public List<ChecksumMismatch> ChecksumMismatches { get; } = new List<ChecksumMismatch>();

        public ReplayResult(int finalTick, ulong finalChecksum, List<ChecksumMismatch> mismatches)
        {
            FinalTick = finalTick;
            FinalChecksum = finalChecksum;
            ChecksumMismatches = mismatches;
        }
    }

    public readonly struct ChecksumMismatch
    {
        public int Tick { get; }
        public ulong Expected { get; }
        public ulong Actual { get; }

        public ChecksumMismatch(int tick, ulong expected, ulong actual)
        {
            Tick = tick;
            Expected = expected;
            Actual = actual;
        }
    }
}
