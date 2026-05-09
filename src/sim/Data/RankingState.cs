using System.Collections.Generic;

namespace RtsGame.Sim.Data
{
    public sealed class RankingState
    {
        public int NextPlacement { get; set; }
        public List<int> PlacementOrder { get; } = new List<int>();

        public RankingState(int playerCount)
        {
            NextPlacement = playerCount;
        }
    }
}
