using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Net.Lockstep
{
    public sealed class LockstepSession
    {
        private readonly List<LockstepPeer> _peers = new List<LockstepPeer>();

        public GameRules Rules { get; }
        public IReadOnlyList<LockstepPeer> Peers
        {
            get { return _peers; }
        }

        public List<DesyncReport> DesyncReports { get; } = new List<DesyncReport>();
        public int CurrentTick { get; private set; }
        public InputDelay InputDelay { get; }

        public LockstepSession(GameRules rules, ulong matchSeed)
            : this(rules, matchSeed, false)
        {
        }

        public LockstepSession(GameRules rules, ulong matchSeed, bool useNomadStart)
        {
            Rules = rules;
            InputDelay = new InputDelay(rules.InputDelayTicks);
            CurrentTick = 0;

            for (int i = 0; i < rules.MaxPlayers; i++)
            {
                GameState initialState = useNomadStart
                    ? GameInitializer.CreateNomadStart(matchSeed, rules.MaxPlayers)
                    : new GameState(matchSeed, rules.MaxPlayers);
                _peers.Add(new LockstepPeer(i, initialState));
            }
        }

        public CommandEnvelope CreateScheduledCommand(int playerIndex, uint sequence, ICommand payload)
        {
            int scheduledTick = CurrentTick + InputDelay.Ticks;
            var header = new CommandHeader(scheduledTick, playerIndex, sequence, payload.Type);
            return new CommandEnvelope(header, payload);
        }

        public void Broadcast(CommandEnvelope command)
        {
            for (int i = 0; i < _peers.Count; i++)
            {
                _peers[i].Receive(command);
            }
        }

        public bool TryAdvanceOneTick()
        {
            for (int i = 0; i < _peers.Count; i++)
            {
                if (!_peers[i].CanAdvance(Rules))
                {
                    return false;
                }
            }

            for (int i = 0; i < _peers.Count; i++)
            {
                _peers[i].Advance(Rules);
            }

            CompareChecksums();
            CurrentTick++;
            return true;
        }

        private void CompareChecksums()
        {
            if (_peers.Count < 2 || Rules.ChecksumIntervalTicks <= 0)
            {
                return;
            }

            int completedTick = CurrentTick;
            if (completedTick % Rules.ChecksumIntervalTicks != 0)
            {
                return;
            }

            ulong checksum = _peers[0].LocalState.LastChecksum;
            for (int i = 1; i < _peers.Count; i++)
            {
                ulong other = _peers[i].LocalState.LastChecksum;
                if (checksum != other)
                {
                    DesyncReports.Add(new DesyncReport(completedTick, _peers[0].PlayerIndex, checksum, _peers[i].PlayerIndex, other));
                }
            }
        }
    }
}
