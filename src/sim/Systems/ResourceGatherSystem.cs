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
                    ClearExhaustedGatherIntent(state, unit);
                    continue;
                }

                if (unit.CarriedAmount >= GameData.VillagerCarryCapacity)
                {
                    if (unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode)
                    {
                        SpatialRules.ClearInteractionReservation(state, unit);
                    }

                    unit.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
                    continue;
                }

                int carryRoom = GameData.VillagerCarryCapacity - unit.CarriedAmount;
                if (!SpatialRules.IsUnitInResourceInteractionRange(unit, node))
                {
                    int blockedTicks = unit.LastMovedTick < 0 ? 0 : state.Tick - unit.LastMovedTick;
                    bool hardTimedOut = blockedTicks >= GameData.ReservationHardTimeoutTicks;
                    bool hasActiveResourceApproach = unit.HasMoveTarget
                        || (unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode
                            && unit.ReservedInteractionTargetId == node.Id);
                    if (hardTimedOut && hasActiveResourceApproach)
                    {
                        SpatialRules.ClearInteractionReservation(state, unit, ReservationReleaseReason.Timeout);
                        unit.LastReservationFailureTick = state.Tick;
                        unit.LastReservationFailureReason = ReservationAttemptFailureReason.NoReachablePath;
                        unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                        unit.HasMoveTarget = false;
                        continue;
                    }

                    bool hasApproachStateForYield = unit.HasMoveTarget
                        || (unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode
                            && unit.ReservedInteractionTargetId == node.Id);
                    if (hasApproachStateForYield
                        && blockedTicks >= GameData.NoProgressTimeoutTicks
                        && ShouldYieldToContestedPeer(state, unit, node.Id))
                    {
                        unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                        unit.HasMoveTarget = false;
                        continue;
                    }

                    if (ShouldDelayGatherReselect(state, unit))
                    {
                        unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                        unit.HasMoveTarget = false;
                        continue;
                    }

                    if (ShouldKeepCurrentApproachTarget(state, unit, node))
                    {
                        unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                        continue;
                    }

                    bool allowAreaFallback = ShouldAllowAreaFallback(state, unit, node);
                    blockedTicks = unit.LastMovedTick < 0 ? 0 : state.Tick - unit.LastMovedTick;
                    hardTimedOut = blockedTicks >= GameData.ReservationHardTimeoutTicks;
                    if (allowAreaFallback
                        && unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode
                        && unit.ReservedInteractionTargetId == node.Id)
                    {
                        bool hadRecentReservationFailure = unit.LastReservationFailureTick >= 0
                            && state.Tick - unit.LastReservationFailureTick <= GameData.ReservationRetargetCadenceTicks
                            && (unit.LastReservationFailureReason == ReservationAttemptFailureReason.SlotUnavailable
                                || unit.LastReservationFailureReason == ReservationAttemptFailureReason.NoReachablePath);
                        if (!hadRecentReservationFailure)
                        {
                            // Preserve single-slot fallback behavior when a slot merely timed out.
                            allowAreaFallback = true;
                        }

                        List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, node);
                        bool hasAlternativeAvailable = false;
                        for (int tileIndex = 0; tileIndex < interactionTiles.Count; tileIndex++)
                        {
                            SpatialRules.TileCoord tile = interactionTiles[tileIndex];
                            bool isCurrentReservedTile = tile.X == unit.ReservedInteractionTileX
                                && tile.Y == unit.ReservedInteractionTileY;
                            if (isCurrentReservedTile)
                            {
                                continue;
                            }

                            if (SpatialRules.IsInteractionSlotAvailableForUnit(
                                state,
                                unit,
                                InteractionReservationKind.ResourceNode,
                                node.Id,
                                tile.X,
                                tile.Y))
                            {
                                hasAlternativeAvailable = true;
                                break;
                            }
                        }

                        if (hadRecentReservationFailure && hasAlternativeAvailable)
                        {
                            SpatialRules.ClearInteractionReservation(state, unit, ReservationReleaseReason.Timeout);
                        }
                    }

                    bool preferCurrentNodeFirst = !hardTimedOut;
                    if (ResourceGatherTargeting.TryChooseResourceNodeAndReserveSlot(
                        state,
                        unit,
                        node.ResourceAreaId,
                        node.Id,
                        preferCurrentNodeFirst,
                        allowAreaFallback,
                        out ResourceNode? selectedNode))
                    {
                        unit.CurrentResourceNodeId = selectedNode!.Id;
                        unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                        continue;
                    }

                    unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                    unit.HasMoveTarget = false;
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
                    SpatialRules.ClearInteractionReservation(state, unit);
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
                        ClearExhaustedGatherIntent(state, unit);
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
                SpatialRules.ClearInteractionReservation(state, unit);
            }

            if (unit.CurrentResourceAreaId != 0
                && TryChooseContinuationNode(state, unit, unit.CurrentResourceAreaId, out ResourceNode? nextNode))
            {
                unit.CurrentResourceNodeId = nextNode!.Id;
                unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
                unit.HasMoveTarget = true;
                unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                return nextNode;
            }

            return null;
        }

        private static bool TryChooseContinuationNode(GameState state, Unit unit, int resourceAreaId, out ResourceNode? selectedNode)
        {
            return ResourceGatherTargeting.TryChooseResourceNodeAndReserveSlot(
                state,
                unit,
                resourceAreaId,
                unit.CurrentResourceNodeId,
                false,
                true,
                out selectedNode);
        }

        private static void ClearExhaustedGatherIntent(GameState state, Unit unit)
        {
            unit.CurrentResourceAreaId = 0;
            unit.CurrentResourceNodeId = 0;
            unit.HasMoveTarget = false;
            SpatialRules.ClearInteractionReservation(state, unit);
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

        private static bool ShouldAllowAreaFallback(GameState state, Unit unit, ResourceNode node)
        {
            return SpatialRules.IsInteractionReservationTimedOut(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                node.Id);
        }

        private static bool ShouldDelayGatherReselect(GameState state, Unit unit)
        {
            if (unit.TaskPhase != WorkerTaskPhase.BlockedWaiting)
            {
                return false;
            }

            if (unit.LastReservationFailureTick < 0)
            {
                return false;
            }

            if (unit.LastReservationFailureReason != ReservationAttemptFailureReason.SlotUnavailable
                && unit.LastReservationFailureReason != ReservationAttemptFailureReason.NoReachablePath)
            {
                return false;
            }

            return state.Tick - unit.LastReservationFailureTick < GameData.ReservationRetargetCadenceTicks;
        }

        private static int Min(int a, int b, int c)
        {
            int result = a < b ? a : b;
            return result < c ? result : c;
        }

        private static bool ShouldYieldToContestedPeer(GameState state, Unit unit, int resourceNodeId)
        {
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit other = state.EntityState.Units[i];
                if (other.IsDead
                    || other.Id == unit.Id
                    || other.CurrentResourceNodeId != resourceNodeId
                    || other.TaskPhase != WorkerTaskPhase.MovingToResourceSlot
                    || other.Id > unit.Id)
                {
                    continue;
                }

                int otherTileX = SpatialRules.GetTileX(other.Position);
                int otherTileY = SpatialRules.GetTileY(other.Position);
                int distance = Abs(unitTileX - otherTileX) + Abs(unitTileY - otherTileY);
                if (distance <= 2)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

    }
}

