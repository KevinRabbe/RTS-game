using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public sealed class DeterministicTrafficLanePreferenceScorerV2 : ITrafficLanePreferenceScorer
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
            int congestionPenalty = congestion * (GameData.SlotScoringCongestionWeight + 2);
            int chokePenalty = CountBlockedNeighbors(state, candidate.X, candidate.Y) * 2;

            if (kind != InteractionReservationKind.MoveDestination)
            {
                return congestionPenalty + chokePenalty;
            }

            int targetTileX = DecodeTileX(targetId);
            int targetTileY = DecodeTileY(targetId);
            int distancePenalty = Abs(candidate.X - targetTileX) + Abs(candidate.Y - targetTileY);
            return congestionPenalty + chokePenalty + distancePenalty;
        }

        private static int CountBlockedNeighbors(GameState state, int tileX, int tileY)
        {
            int blocked = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    if (SpatialRules.IsTileBlockedForUnitMovement(state, tileX + dx, tileY + dy))
                    {
                        blocked++;
                    }
                }
            }

            return blocked;
        }

        private static int DecodeTileX(int key)
        {
            return (short)(key & 0xFFFF);
        }

        private static int DecodeTileY(int key)
        {
            return (short)((key >> 16) & 0xFFFF);
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }
    }
}
