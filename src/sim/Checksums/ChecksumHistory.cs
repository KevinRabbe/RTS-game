using System.Collections.Generic;

namespace RtsGame.Sim.Checksums
{
    public sealed class ChecksumHistory
    {
        private readonly List<ChecksumSample> _samples = new List<ChecksumSample>();

        public IReadOnlyList<ChecksumSample> Samples
        {
            get { return _samples; }
        }

        public void Add(int tick, ulong checksum)
        {
            _samples.Add(new ChecksumSample(tick, checksum));
        }
    }

    public readonly struct ChecksumSample
    {
        public int Tick { get; }
        public ulong Checksum { get; }

        public ChecksumSample(int tick, ulong checksum)
        {
            Tick = tick;
            Checksum = checksum;
        }
    }
}
