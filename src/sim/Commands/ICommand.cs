using RtsGame.Sim.Core;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public interface ICommand
    {
        CommandType Type { get; }
        void WritePayload(CanonicalWriter writer);
        bool IsValid(GameState state, GameRules rules, CommandHeader header);
        void Execute(GameState state, GameRules rules, CommandHeader header);
    }
}
