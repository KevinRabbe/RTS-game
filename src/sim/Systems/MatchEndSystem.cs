using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class MatchEndSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            if (state.MatchResultState.IsFinished || state.PlayerStates.Players.Count <= 1)
            {
                return;
            }

            int aliveCount = 0;
            int winnerIndex = -1;
            for (int i = 0; i < state.PlayerStates.Players.Count; i++)
            {
                PlayerState player = state.PlayerStates.Players[i];
                if (!player.IsDefeated)
                {
                    aliveCount++;
                    winnerIndex = player.PlayerIndex;
                }
            }

            if (aliveCount != 1 || winnerIndex < 0)
            {
                return;
            }

            PlayerState winner = state.PlayerStates.Players[winnerIndex];
            if (winner.Placement == 0)
            {
                winner.Placement = 1;
                state.RankingState.PlacementOrder.Add(winnerIndex);
            }

            state.RankingState.NextPlacement = 0;
            state.MatchResultState.IsFinished = true;
            state.MatchResultState.WinnerPlayerIndex = winnerIndex;
            state.MatchResultState.FinishedTick = state.Tick;
        }
    }
}
