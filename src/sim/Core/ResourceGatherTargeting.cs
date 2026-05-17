using System.Collections.Generic;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public static class ResourceGatherTargeting
    {
        public static bool TryChooseResourceNodeAndReserveSlot(
            GameState state,
            Unit unit,
            int resourceAreaId,
            int preferredNodeId,
            out ResourceNode? selectedNode)
        {
            selectedNode = null;
            List<ResourceNode> candidates = EnumerateCandidateNodes(state, resourceAreaId);
            if (candidates.Count == 0)
            {
                return false;
            }

            candidates.Sort((left, right) =>
            {
                int leftReserved = CountReservedWorkersOnNode(state, left.Id);
                int rightReserved = CountReservedWorkersOnNode(state, right.Id);
                int reservedCompare = leftReserved.CompareTo(rightReserved);
                if (reservedCompare != 0)
                {
                    return reservedCompare;
                }

                int unitTileX = SpatialRules.GetTileX(unit.Position);
                int unitTileY = SpatialRules.GetTileY(unit.Position);
                int leftDistance = Abs(unitTileX - SpatialRules.GetTileX(left.Position)) + Abs(unitTileY - SpatialRules.GetTileY(left.Position));
                int rightDistance = Abs(unitTileX - SpatialRules.GetTileX(right.Position)) + Abs(unitTileY - SpatialRules.GetTileY(right.Position));
                int distanceCompare = leftDistance.CompareTo(rightDistance);
                if (distanceCompare != 0)
                {
                    return distanceCompare;
                }

                bool leftPreferred = left.Id == preferredNodeId;
                bool rightPreferred = right.Id == preferredNodeId;
                if (leftPreferred != rightPreferred)
                {
                    return leftPreferred ? -1 : 1;
                }

                return left.Id.CompareTo(right.Id);
            });

            for (int i = 0; i < candidates.Count; i++)
            {
                ResourceNode candidate = candidates[i];
                if (TryReserveApproachTile(state, unit, candidate))
                {
                    selectedNode = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryReserveApproachTile(GameState state, Unit unit, ResourceNode node)
        {
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, node);
            bool hasExcludedTile = SpatialRules.IsInteractionReservationTimedOut(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                node.Id);
            SpatialRules.TileCoord excludedTile = hasExcludedTile
                ? new SpatialRules.TileCoord(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY)
                : default;

            // Prefer alternate tiles first after timeout, but allow deterministic
            // fallback to the excluded tile if no alternative exists.
            bool allowExcludedFallback = true;
            return SpatialRules.TryReserveNearestReachableInteractionTile(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                node.Id,
                interactionTiles,
                hasExcludedTile,
                excludedTile,
                allowExcludedFallback,
                out _);
        }

        private static List<ResourceNode> EnumerateCandidateNodes(GameState state, int resourceAreaId)
        {
            var candidates = new List<ResourceNode>();
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.ResourceAreaId != resourceAreaId || node.IsDepleted)
                {
                    continue;
                }

                GatherProfile profile = GameData.GetGatherProfile(node.GatherProfileId);
                if (profile.AutoContinuationMode != ResourceAutoContinuationMode.SameArea)
                {
                    continue;
                }

                candidates.Add(node);
            }

            return candidates;
        }

        private static int CountReservedWorkersOnNode(GameState state, int resourceNodeId)
        {
            int count = 0;
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.ReservedInteractionKind != InteractionReservationKind.ResourceNode)
                {
                    continue;
                }

                if (unit.ReservedInteractionTargetId == resourceNodeId)
                {
                    count++;
                }
            }

            return count;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }
    }
}
