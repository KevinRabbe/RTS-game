using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using System.Collections.Generic;

namespace RtsGame.Sim.Systems
{
    public sealed class ResourceGatherSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.UnitTypeId != UnitTypeId.Villager || unit.CurrentResourceNodeId == 0)
                {
                    continue;
                }

                ResourceNode? node = ResolveCurrentNode(state, unit);
                if (node == null)
                {
                    ClearExhaustedGatherIntent(unit);
                    continue;
                }

                if (unit.CarriedAmount >= GameData.VillagerCarryCapacity)
                {
                    if (unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode)
                    {
                        SpatialRules.ClearInteractionReservation(unit);
                    }

                    unit.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
                    continue;
                }

                int carryRoom = GameData.VillagerCarryCapacity - unit.CarriedAmount;
                if (!SpatialRules.IsUnitInResourceInteractionRange(unit, node))
                {
                    if (ShouldKeepCurrentApproachTarget(state, unit, node))
                    {
                        unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                        continue;
                    }

                    if (TryChooseResourceApproachTile(state, unit, node, out int approachX, out int approachY))
                    {
                        unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(approachX, approachY);
                        continue;
                    }

                    unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                    continue;
                }

                unit.TaskPhase = WorkerTaskPhase.Gathering;
                unit.HasMoveTarget = false;
                GatherProfile profile = GameData.GetGatherProfile(node.GatherProfileId);
                int gathered = Min(profile.GatherAmountPerTick, carryRoom, node.RemainingAmount);
                if (gathered <= 0)
                {
                    continue;
                }

                if (unit.CarriedResourceType == ResourceType.None)
                {
                    unit.CarriedResourceType = node.ResourceType;
                }

                if (unit.CarriedResourceType != node.ResourceType)
                {
                    continue;
                }

                unit.CarriedAmount += gathered;
                node.RemainingAmount -= gathered;
                if (node.IsDepleted)
                {
                    SpatialRules.ClearInteractionReservation(unit);
                    if (unit.CarriedAmount >= GameData.VillagerCarryCapacity)
                    {
                        ResolveCurrentNode(state, unit);
                        unit.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
                    }
                    else if (TryChooseContinuationNode(state, unit, node.ResourceAreaId, out ResourceNode? nextNode))
                    {
                        unit.CurrentResourceNodeId = nextNode!.Id;
                        unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                    }
                    else
                    {
                        ClearExhaustedGatherIntent(unit);
                    }
                }
            }
        }

        private static ResourceNode? ResolveCurrentNode(GameState state, Unit unit)
        {
            ResourceNode? node = FindNode(state, unit.CurrentResourceNodeId);
            if (node != null && !node.IsDepleted)
            {
                return node;
            }

            if (node != null)
            {
                SpatialRules.ClearInteractionReservation(unit);
            }

            if (unit.CurrentResourceAreaId != 0
                && TryChooseContinuationNode(state, unit, unit.CurrentResourceAreaId, out ResourceNode? nextNode))
            {
                unit.CurrentResourceNodeId = nextNode!.Id;
                return nextNode;
            }

            return null;
        }

        private static bool TryChooseContinuationNode(GameState state, Unit unit, int resourceAreaId, out ResourceNode? selectedNode)
        {
            selectedNode = null;
            ResourceNode? best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode candidate = state.EconomyState.ResourceNodes[i];
                if (candidate.ResourceAreaId != resourceAreaId || candidate.IsDepleted)
                {
                    continue;
                }

                GatherProfile profile = GameData.GetGatherProfile(candidate.GatherProfileId);
                if (profile.AutoContinuationMode != ResourceAutoContinuationMode.SameArea)
                {
                    continue;
                }

                int unitTileX = SpatialRules.GetTileX(unit.Position);
                int unitTileY = SpatialRules.GetTileY(unit.Position);
                int candidateTileX = SpatialRules.GetTileX(candidate.Position);
                int candidateTileY = SpatialRules.GetTileY(candidate.Position);
                int distance = Abs(unitTileX - candidateTileX) + Abs(unitTileY - candidateTileY);
                if (best == null
                    || distance < bestDistance
                    || (distance == bestDistance && candidate.Id < best.Id))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            if (best == null)
            {
                return false;
            }

            if (!TryChooseResourceApproachTile(state, unit, best, out _, out _))
            {
                return false;
            }

            selectedNode = best;
            return true;
        }

        private static void ClearExhaustedGatherIntent(Unit unit)
        {
            unit.CurrentResourceAreaId = 0;
            unit.CurrentResourceNodeId = 0;
            unit.HasMoveTarget = false;
            SpatialRules.ClearInteractionReservation(unit);
            unit.TaskPhase = WorkerTaskPhase.Idle;
        }

        private static ResourceNode? FindNode(GameState state, int nodeId)
        {
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                if (state.EconomyState.ResourceNodes[i].Id == nodeId)
                {
                    return state.EconomyState.ResourceNodes[i];
                }
            }

            return null;
        }

        private static bool TryChooseResourceApproachTile(GameState state, Unit unit, ResourceNode node, out int approachX, out int approachY)
        {
            approachX = 0;
            approachY = 0;
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, node);
            bool hasExcludedTile = SpatialRules.IsInteractionReservationTimedOut(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                node.Id);
            SpatialRules.TileCoord excludedTile = hasExcludedTile
                ? new SpatialRules.TileCoord(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY)
                : default;
            if (!SpatialRules.TryReserveNearestReachableInteractionTile(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                node.Id,
                interactionTiles,
                hasExcludedTile,
                excludedTile,
                out SpatialRules.TileCoord selected))
            {
                return false;
            }

            approachX = selected.X;
            approachY = selected.Y;
            return true;
        }

        private static bool ShouldKeepCurrentApproachTarget(GameState state, Unit unit, ResourceNode node)
        {
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, node);
            return SpatialRules.ShouldRetainInteractionReservation(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                node.Id,
                interactionTiles);
        }

        private static int Min(int a, int b, int c)
        {
            int result = a < b ? a : b;
            return result < c ? result : c;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

    }
}
