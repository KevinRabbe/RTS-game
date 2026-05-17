using System.Collections.Generic;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public sealed class DeterministicTrafficReservationService : ITrafficReservationService
    {
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
            for (int i = 0; i < candidates.Count; i++)
            {
                SpatialRules.TileCoord tile = candidates[i];
                if (!SpatialRules.IsInteractionSlotAvailableForUnit(state, unit, kind, targetId, tile.X, tile.Y))
                {
                    continue;
                }

                if (!state.PathQueries.TryPathCost(state, unitTileX, unitTileY, tile.X, tile.Y, contextVersion, out int cost))
                {
                    continue;
                }

                int distance = Abs(unitTileX - tile.X) + Abs(unitTileY - tile.Y);
                if (!found
                    || cost < bestPathCost
                    || (cost == bestPathCost && distance < bestDistance)
                    || (cost == bestPathCost && distance == bestDistance && Compare(tile, selected) < 0))
                {
                    selected = tile;
                    bestPathCost = cost;
                    bestDistance = distance;
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

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int Compare(SpatialRules.TileCoord left, SpatialRules.TileCoord right)
        {
            int yCompare = left.Y.CompareTo(right.Y);
            return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
        }
    }
}
