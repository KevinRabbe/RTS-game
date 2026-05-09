using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class ResignCommand : ICommand
    {
        public CommandType Type
        {
            get { return CommandType.Resign; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return false;
            }

            PlayerState player = state.PlayerStates.Players[header.PlayerIndex];
            return !player.IsResigned && !player.IsDefeated;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            state.PlayerStates.Players[header.PlayerIndex].IsResigned = true;
        }
    }
}
