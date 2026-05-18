using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public sealed class DeterministicTrafficLanePreferenceScorer : ITrafficLanePreferenceScorer
    {
        public int Score(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            SpatialRules.TileCoord candidate,
            int contextVersion)
        {
            int congestion = SpatialRules.CountNearbyTraffic(state, unit, candidate.X, candidate.Y);
            return congestion * GameData.SlotScoringCongestionWeight;
        }
    }
}
