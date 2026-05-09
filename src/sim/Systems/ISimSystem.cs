using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Sim.Systems
{
    public interface ISimSystem
    {
        void Run(GameState state, GameRules rules, TickCommandContext commandContext);
    }
}
