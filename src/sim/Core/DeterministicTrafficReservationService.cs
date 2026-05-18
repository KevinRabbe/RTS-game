using System.Collections.Generic;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public sealed class DeterministicTrafficReservationService : ITrafficReservationService
    {
        private readonly Dictionary<long, int> _evictedUntilTickBySlotKey = new Dictionary<long, int>();

        public ITrafficLanePreferenceScorer? LanePreferenceScorer { get; set; }

        public void Reserve(GameState state, Unit unit, InteractionReservationKind kind, int targetId, SpatialRules.TileCoord tile)
        {
            bool hadReservation = unit.ReservedInteractionKind != InteractionReservationKind.None;
            int previousTileKey = SpatialRules.EncodeTileKey(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
            unit.ReservedInteractionKind = kind;
            unit.ReservedInteractionTargetId = targetId;
            unit.ReservedInteractionTileX = tile.X;
            unit.ReservedInteractionTileY = tile.Y;
            int nextTileKey = SpatialRules.EncodeTileKey(tile.X, tile.Y);
            state.SpatialTileIndex.ApplyReservationChange(previousTileKey, hadReservation, nextTileKey, true);
        }

        public void Release(GameState state, Unit unit, ReservationReleaseReason reason)
        {
            bool hadReservation = unit.ReservedInteractionKind != InteractionReservationKind.None;
            int previousTileKey = SpatialRules.EncodeTileKey(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
            if (hadReservation && reason == ReservationReleaseReason.Timeout)
            {
                long evictionKey = ComposeEvictionKey(unit.Id, unit.ReservedInteractionKind, unit.ReservedInteractionTargetId, unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                _evictedUntilTickBySlotKey[evictionKey] = state.Tick + GameData.ReservationStaleEvictionWindowTicks;
            }

            unit.ReservedInteractionKind = InteractionReservationKind.None;
            unit.ReservedInteractionTargetId = 0;
            unit.ReservedInteractionTileX = 0;
            unit.ReservedInteractionTileY = 0;
            state.SpatialTileIndex.ApplyReservationChange(previousTileKey, hadReservation, 0, false);
            if (hadReservation)
            {
                state.DebugCounters.ReservationReleaseCount++;
            }
        }

        public bool Revalidate(GameState state, Unit unit, InteractionReservationKind kind, int targetId, int tileX, int tileY)
        {
            if (unit.ReservedInteractionKind != kind || unit.ReservedInteractionTargetId != targetId)
            {
                return false;
            }

            return unit.ReservedInteractionTileX == tileX && unit.ReservedInteractionTileY == tileY;
        }

        public bool TryReserve(GameState state, Unit unit, InteractionReservationKind kind, int targetId, IReadOnlyList<SpatialRules.TileCoord> candidates, int contextVersion, out SpatialRules.TileCoord selected)
        {
            selected = default;
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            bool found = false;
            int bestPathCost = int.MaxValue;
            int bestDistance = int.MaxValue;
            int bestLaneScore = int.MaxValue;
            IReadOnlyList<SpatialRules.TileCoord> scopedCandidates = BuildScopedCandidates(state, unit, kind, targetId, candidates, unitTileX, unitTileY);
            for (int i = 0; i < scopedCandidates.Count; i++)
            {
                SpatialRules.TileCoord tile = scopedCandidates[i];
                if (!SpatialRules.IsInteractionSlotAvailableForUnit(state, unit, kind, targetId, tile.X, tile.Y))
                {
                    continue;
                }

                if (IsEvictedForUnit(state, unit, kind, targetId, tile.X, tile.Y))
                {
                    continue;
                }

                if (!state.PathQueries.TryPathCost(state, unitTileX, unitTileY, tile.X, tile.Y, contextVersion, out int cost))
                {
                    continue;
                }

                int distance = Abs(unitTileX - tile.X) + Abs(unitTileY - tile.Y);
                int laneScore = LanePreferenceScorer?.Score(state, unit, kind, targetId, tile, contextVersion) ?? 0;
                if (!found
                    || cost < bestPathCost
                    || (cost == bestPathCost && distance < bestDistance)
                    || (cost == bestPathCost && distance == bestDistance && laneScore < bestLaneScore)
                    || (cost == bestPathCost && distance == bestDistance && laneScore == bestLaneScore && Compare(tile, selected) < 0))
                {
                    selected = tile;
                    bestPathCost = cost;
                    bestDistance = distance;
                    bestLaneScore = laneScore;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            Reserve(state, unit, kind, targetId, selected);
            state.DebugCounters.ReservationRetargetCount++;
            return true;
        }

        private IReadOnlyList<SpatialRules.TileCoord> BuildScopedCandidates(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            IReadOnlyList<SpatialRules.TileCoord> candidates,
            int unitTileX,
            int unitTileY)
        {
            if (candidates.Count <= GameData.PathCostShortlistMaxCandidates)
            {
                return candidates;
            }

            var scored = new List<CandidateScore>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                SpatialRules.TileCoord tile = candidates[i];
                int distance = Abs(unitTileX - tile.X) + Abs(unitTileY - tile.Y);
                int congestion = SpatialRules.CountNearbyTraffic(state, unit, tile.X, tile.Y);
                int evicted = IsEvictedForUnit(state, unit, kind, targetId, tile.X, tile.Y) ? 1 : 0;
                scored.Add(new CandidateScore(tile, distance, congestion, evicted));
            }

            scored.Sort((left, right) =>
            {
                int evictedCompare = left.Evicted.CompareTo(right.Evicted);
                if (evictedCompare != 0)
                {
                    return evictedCompare;
                }

                int distanceCompare = left.Distance.CompareTo(right.Distance);
                if (distanceCompare != 0)
                {
                    return distanceCompare;
                }

                int congestionCompare = left.Congestion.CompareTo(right.Congestion);
                if (congestionCompare != 0)
                {
                    return congestionCompare;
                }

                return Compare(left.Tile, right.Tile);
            });

            int take = Min(scored.Count, GameData.PathCostShortlistMaxCandidates);
            var shortlist = new List<SpatialRules.TileCoord>(take);
            for (int i = 0; i < take; i++)
            {
                shortlist.Add(scored[i].Tile);
            }

            return shortlist;
        }

        private bool IsEvictedForUnit(GameState state, Unit unit, InteractionReservationKind kind, int targetId, int tileX, int tileY)
        {
            long key = ComposeEvictionKey(unit.Id, kind, targetId, tileX, tileY);
            if (!_evictedUntilTickBySlotKey.TryGetValue(key, out int untilTick))
            {
                return false;
            }

            if (state.Tick > untilTick)
            {
                _evictedUntilTickBySlotKey.Remove(key);
                return false;
            }

            return true;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int Min(int left, int right)
        {
            return left < right ? left : right;
        }

        private static int Compare(SpatialRules.TileCoord left, SpatialRules.TileCoord right)
        {
            int yCompare = left.Y.CompareTo(right.Y);
            return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
        }

        private static long ComposeEvictionKey(int unitId, InteractionReservationKind kind, int targetId, int tileX, int tileY)
        {
            long key = unitId;
            key = (key * 73856093L) ^ (long)kind;
            key = (key * 19349663L) ^ targetId;
            key = (key * 83492791L) ^ tileX;
            key = (key * 2654435761L) ^ tileY;
            return key;
        }

        private readonly struct CandidateScore
        {
            public SpatialRules.TileCoord Tile { get; }
            public int Distance { get; }
            public int Congestion { get; }
            public int Evicted { get; }

            public CandidateScore(SpatialRules.TileCoord tile, int distance, int congestion, int evicted)
            {
                Tile = tile;
                Distance = distance;
                Congestion = congestion;
                Evicted = evicted;
            }
        }
    }
}
