using RtsGame.Sim.Checksums;
using RtsGame.Sim.Core;

namespace RtsGame.Sim.Systems
{
    public sealed class ChecksumSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            state.LastChecksum = StateChecksum.Compute(state, rules);
        }
    }
}
