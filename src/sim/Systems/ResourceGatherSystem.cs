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

                ResourceNode? node = FindNode(state, unit.CurrentResourceNodeId);
                if (node == null || node.IsDepleted)
                {
                    unit.CurrentResourceNodeId = 0;
                    SpatialRules.ClearInteractionReservation(unit);
                    unit.TaskPhase = WorkerTaskPhase.Idle;
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
                int gathered = Min(GameData.VillagerGatherPerTick, carryRoom, node.RemainingAmount);
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
                    unit.CurrentResourceNodeId = 0;
                    SpatialRules.ClearInteractionReservation(unit);
                    unit.TaskPhase = WorkerTaskPhase.Idle;
                }
            }
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
            if (!SpatialRules.TryReserveNearestReachableInteractionTile(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                node.Id,
                interactionTiles,
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

    }
}
