using RtsGame.Sim.Core;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class DebugIncrementCounterCommand : ICommand
    {
        public int Amount { get; }

        public DebugIncrementCounterCommand(int amount)
        {
            Amount = amount;
        }

        public CommandType Type
        {
            get { return CommandType.DebugIncrementCounter; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteInt32(Amount);
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            return header.CommandType == Type
                && header.Tick == state.Tick
                && header.PlayerIndex >= 0
                && header.PlayerIndex < rules.MaxPlayers
                && Amount >= 0
                && Amount <= 1000;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            checked
            {
                state.DebugCounters.DebugCounter += Amount;
            }
        }
    }
}
