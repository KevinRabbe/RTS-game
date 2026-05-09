using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Sim.Replay
{
    public sealed class ReplayRecorder
    {
        private readonly ReplayFile _replay;

        public ReplayRecorder(GameRules rules, ulong matchSeed, int playerCount)
            : this(rules, matchSeed, playerCount, ReplayInitialState.Empty)
        {
        }

        public ReplayRecorder(GameRules rules, ulong matchSeed, int playerCount, ReplayInitialState initialState)
        {
            _replay = new ReplayFile(1, rules, matchSeed, playerCount, initialState);
        }

        public ReplayFile Replay
        {
            get { return _replay; }
        }

        public void RecordCommand(CommandEnvelope command)
        {
            _replay.Commands.Add(command);
        }

        public void RecordChecksum(int tick, ulong checksum)
        {
            _replay.OptionalChecksums.Add(new ReplayChecksum(tick, checksum));
        }
    }
}
