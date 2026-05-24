using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using System.Collections.Generic;

namespace RtsGame.Sim.Systems
{
    public sealed class ResourceGatherSystem : ISimSystem
    {
        private readonly GatherEngineV2 gatherEngineV2 = new GatherEngineV2();

        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            if (rules.EnableGatherEngineV2)
            {
                gatherEngineV2.Run(state, rules, commandContext, () => RunLegacyPipeline(state, rules));
                return;
            }

            RunLegacyPipeline(state, rules);
        }

        private void RunLegacyPipeline(GameState state, GameRules rules)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.UnitTypeId != UnitTypeId.Villager || unit.CurrentResourceNodeId == 0)
                {
                    continue;
                }

                ResourceNode? node = ResolveCurrentNode(state, unit);
                // Contract: AssignedResourceNodeId is the player's sticky intent.
                // CurrentResourceNodeId may rotate for deterministic continuation.
                if (rules.EnableGatherEngineV2
                    && unit.AssignedResourceNodeId != 0
                    && (node == null || node.Id != unit.AssignedResourceNodeId))
                {
                    ResourceNode? assignedNode = FindNode(state, unit.AssignedResourceNodeId);
                    if (assignedNode != null && !assignedNode.IsDepleted)
                    {
                        unit.CurrentResourceAreaId = assignedNode.ResourceAreaId;
                        unit.CurrentResourceNodeId = assignedNode.Id;
                        node = assignedNode;
                    }
                }
                if (node == null)
                {
                    unit.LastGatherFallbackReason = GatherFallbackReason.NodeInvalid;
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
                    bool noProgressTimedOut = state.MovementProgressPolicy.IsNoProgressTimedOut(state, unit);
                    bool hardTimedOut = blockedTicks >= GameData.ReservationHardTimeoutTicks;
                    bool hasActiveResourceApproach = unit.HasMoveTarget
                        || (unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode
                            && unit.ReservedInteractionTargetId == node.Id);
                    if (hardTimedOut && hasActiveResourceApproach)
                    {
                        SpatialRules.ClearInteractionReservation(state, unit, ReservationReleaseReason.Timeout);
                        unit.LastReservationFailureTick = state.Tick;
                        unit.LastReservationFailureReason = ReservationAttemptFailureReason.NoReachablePath;
                        unit.LastGatherFallbackReason = GatherFallbackReason.Unreachable;
                        unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                        unit.HasMoveTarget = false;
                        continue;
                    }

                    if (ShouldDelayGatherReselect(state, rules, unit))
                    {
                        unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                        unit.HasMoveTarget = false;
                        unit.LastGatherFallbackReason = GatherFallbackReason.StaleTimeout;
                        continue;
                    }

                    if (!noProgressTimedOut && ShouldKeepCurrentApproachTarget(state, unit, node))
                    {
                        ActivateResourceApproach(state, unit);
                        continue;
                    }

                    bool allowAreaFallback = ShouldAllowAreaFallback(state, unit, node);
                    blockedTicks = unit.LastMovedTick < 0 ? 0 : state.Tick - unit.LastMovedTick;
                    hardTimedOut = blockedTicks >= GameData.ReservationHardTimeoutTicks;
                    if (allowAreaFallback
                        && unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode
                        && unit.ReservedInteractionTargetId == node.Id)
                    {
                        unit.LastGatherFallbackReason = GatherFallbackReason.StaleTimeout;
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
                        ActivateResourceApproach(state, unit);
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
                    unit.LastGatherFallbackReason = GatherFallbackReason.NodeDepleted;
                    SpatialRules.ClearInteractionReservation(state, unit);
                    if (unit.CarriedAmount >= GameData.VillagerCarryCapacity)
                    {
                        ResolveCurrentNode(state, unit);
                        unit.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
                    }
                    else if (TryChooseContinuationNode(state, unit, node.ResourceAreaId, out ResourceNode? nextNode))
                    {
                        unit.CurrentResourceNodeId = nextNode!.Id;
                        ActivateResourceApproach(state, unit);
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
                ActivateResourceApproach(state, unit);
                return nextNode;
            }

            return null;
        }

        private static bool TryChooseContinuationNode(GameState state, Unit unit, int resourceAreaId, out ResourceNode? selectedNode)
        {
            // Continuation selection is area-scoped and reservation-aware so
            // multi-worker pressure distributes instead of hot-looping one node.
            return ResourceGatherTargeting.TryChooseResourceNodeAndReserveSlot(
                state,
                unit,
                resourceAreaId,
                unit.CurrentResourceNodeId,
                false,
                true,
                out selectedNode);
        }

        private static void ActivateResourceApproach(GameState state, Unit unit)
        {
            bool isFreshApproach = !unit.HasMoveTarget
                || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting
                || unit.MoveTarget.X.Raw != Fixed.FromInt(unit.ReservedInteractionTileX).Raw
                || unit.MoveTarget.Y.Raw != Fixed.FromInt(unit.ReservedInteractionTileY).Raw;
            unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
            unit.HasMoveTarget = true;
            unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
            // Only reset progress window on a fresh/changed approach. Reapplying the
            // same target every tick must not mask no-progress stall detection.
            if (isFreshApproach)
            {
                unit.LastMovedTick = state.Tick;
            }
        }

        private static void ClearExhaustedGatherIntent(GameState state, Unit unit)
        {
            unit.CurrentResourceAreaId = 0;
            unit.CurrentResourceNodeId = 0;
            unit.AssignedResourceNodeId = 0;
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
            // If a worker is waiting without an active reservation, allow area-level redistribution
            // so saturated single-node contention does not starve multi-worker gather loops.
            if (unit.TaskPhase == WorkerTaskPhase.BlockedWaiting
                && unit.ReservedInteractionKind != InteractionReservationKind.ResourceNode)
            {
                return true;
            }

            return SpatialRules.IsInteractionReservationTimedOut(
                state,
                unit,
                InteractionReservationKind.ResourceNode,
                node.Id);
        }

        private static bool ShouldDelayGatherReselect(GameState state, GameRules rules, Unit unit)
        {
            if (rules.EnableGatherEngineV2)
            {
                int churnWindowAge = unit.ReservationChurnWindowStartTick < 0 ? int.MaxValue : state.Tick - unit.ReservationChurnWindowStartTick;
                if (churnWindowAge > GameData.GatherReservationChurnWindowTicks)
                {
                    unit.ReservationChurnWindowStartTick = -1;
                    unit.ReservationChurnCountWindow = 0;
                }
                else if (unit.ReservationChurnCountWindow >= GameData.GatherReservationChurnMaxPerWindow)
                {
                    return true;
                }
            }

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

    }
}

