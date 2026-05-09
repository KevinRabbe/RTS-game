using RtsGame.Sim.Core;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class NoOpCommand : ICommand
    {
        public CommandType Type
        {
            get { return CommandType.NoOp; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            return header.CommandType == Type && header.Tick == state.Tick && header.PlayerIndex >= 0 && header.PlayerIndex < rules.MaxPlayers;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
        }
    }
}
