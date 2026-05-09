using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class RankingSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int playerIndex = 0; playerIndex < state.PlayerStates.Players.Count; playerIndex++)
            {
                PlayerState player = state.PlayerStates.Players[playerIndex];
                if (!player.IsDefeated || player.Placement != 0)
                {
                    continue;
                }

                player.Placement = state.RankingState.NextPlacement;
                state.RankingState.NextPlacement--;
                state.RankingState.PlacementOrder.Add(playerIndex);
            }
        }
    }
}
