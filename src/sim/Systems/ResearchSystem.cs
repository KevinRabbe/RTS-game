using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class ResearchSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int playerIndex = 0; playerIndex < state.PlayerStates.Players.Count; playerIndex++)
            {
                PlayerState player = state.PlayerStates.Players[playerIndex];
                if (player.IsDefeated || player.IsResigned || player.TechState.ResearchQueue.Count == 0)
                {
                    continue;
                }

                ResearchQueueItem item = player.TechState.ResearchQueue[0];
                item.ProgressTicks++;
                if (item.ProgressTicks < item.RequiredTicks)
                {
                    continue;
                }

                player.TechState.ResearchQueue.RemoveAt(0);
                if (!TechRules.IsCompleted(player, item.TechId))
                {
                    TechRules.ApplyCompletedTech(player, item.TechId);
                }
            }
        }
    }
}
