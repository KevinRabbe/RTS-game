using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Sim.Replay
{
    public sealed class ReplayFile
    {
        public uint FormatVersion { get; }
        public uint RulesVersion { get; }
        public ulong MatchSeed { get; }
        public int PlayerCount { get; }
        public ReplayInitialState InitialState { get; }
        public GameRules InitialRules { get; }
        public List<CommandEnvelope> Commands { get; } = new List<CommandEnvelope>();
        public List<ReplayChecksum> OptionalChecksums { get; } = new List<ReplayChecksum>();

        public ReplayFile(uint formatVersion, GameRules initialRules, ulong matchSeed, int playerCount, ReplayInitialState initialState)
        {
            FormatVersion = formatVersion;
            RulesVersion = initialRules.RulesVersion;
            InitialRules = initialRules;
            MatchSeed = matchSeed;
            PlayerCount = playerCount;
            InitialState = initialState;
        }
    }

    public enum ReplayInitialState
    {
        Empty = 0,
        Nomad = 1
    }

    public readonly struct ReplayChecksum
    {
        public int Tick { get; }
        public ulong Checksum { get; }

        public ReplayChecksum(int tick, ulong checksum)
        {
            Tick = tick;
            Checksum = checksum;
        }
    }
}
