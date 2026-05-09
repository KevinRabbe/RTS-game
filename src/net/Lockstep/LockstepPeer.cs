using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Net.Lockstep
{
    public sealed class LockstepPeer
    {
        private readonly TickRunner _runner = new TickRunner();

        public int PlayerIndex { get; }
        public GameState LocalState { get; }
        public PeerCommandInbox Inbox { get; } = new PeerCommandInbox();
        public ChecksumHistory ChecksumHistory { get; } = new ChecksumHistory();

        public LockstepPeer(int playerIndex, ulong matchSeed, int playerCount)
            : this(playerIndex, new GameState(matchSeed, playerCount))
        {
        }

        public LockstepPeer(int playerIndex, GameState initialState)
        {
            PlayerIndex = playerIndex;
            LocalState = initialState;
        }

        public bool CanAdvance(GameRules rules)
        {
            return Inbox.Commands.HasAllRequiredPlayerInputs(LocalState.Tick, rules.MaxPlayers);
        }

        public void Advance(GameRules rules)
        {
            _runner.AdvanceOneTick(LocalState, rules, Inbox.Commands);
            ChecksumHistory.Add(LocalState.Tick - 1, LocalState.LastChecksum);
        }

        public void Receive(CommandEnvelope command)
        {
            Inbox.Receive(command);
        }
    }
}
