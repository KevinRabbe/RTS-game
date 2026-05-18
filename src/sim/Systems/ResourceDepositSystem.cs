using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using System.Collections.Generic;

namespace RtsGame.Sim.Systems
{
    public sealed class ResourceDepositSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.CarriedAmount < GameData.VillagerCarryCapacity || unit.CarriedResourceType == ResourceType.None)
                {
                    continue;
                }

                Building? dropOff = FindNearestCompletedTownCenter(state, unit.OwnerPlayerIndex, unit.Position);
                if (dropOff == null)
                {
                    unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                    continue;
                }

                if (!SpatialRules.IsUnitInBuildingInteractionRange(unit, dropOff))
                {
                    if (IsNoProgressTimedOut(state, unit))
                    {
                        unit.HasMoveTarget = false;
                        SpatialRules.ClearInteractionReservation(state, unit);
                    }

                    if (ShouldKeepCurrentApproachTarget(state, unit, dropOff))
                    {
                        unit.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                        continue;
                    }

                    if (TryChooseDropOffApproachTile(state, unit, dropOff, out int approachX, out int approachY))
                    {
                        unit.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(approachX, approachY);
                        continue;
                    }

                    unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                    continue;
                }

                unit.TaskPhase = WorkerTaskPhase.Depositing;
                unit.HasMoveTarget = false;
                SpatialRules.ClearInteractionReservation(state, unit);
                state.PlayerStates.Players[unit.OwnerPlayerIndex].Resources.Add(unit.CarriedResourceType, unit.CarriedAmount);
                unit.CarriedAmount = 0;
                unit.CarriedResourceType = ResourceType.None;
                unit.TaskPhase = unit.CurrentResourceNodeId == 0 ? WorkerTaskPhase.Idle : WorkerTaskPhase.MovingToResourceSlot;
            }
        }

        private static Building? FindNearestCompletedTownCenter(GameState state, int ownerPlayerIndex, FixedVector2 unitPosition)
        {
            Building? best = null;
            long bestDistance = long.MaxValue;
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsDead
                    && !building.IsUnderConstruction
                    && building.OwnerPlayerIndex == ownerPlayerIndex
                    && building.BuildingTypeId == BuildingTypeId.TownCenter)
                {
                    long distance = (building.Position - unitPosition).LengthSquaredRaw();
                    if (best == null || distance < bestDistance || (distance == bestDistance && building.Id < best.Id))
                    {
                        best = building;
                        bestDistance = distance;
                    }
                }
            }

            return best;
        }

        private static bool TryChooseDropOffApproachTile(GameState state, Unit unit, Building dropOff, out int approachX, out int approachY)
        {
            approachX = 0;
            approachY = 0;
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateBuildingInteractionTiles(state, dropOff);
            bool hasExcludedTile = SpatialRules.IsInteractionReservationTimedOut(
                state,
                unit,
                InteractionReservationKind.Dropoff,
                dropOff.Id);
            SpatialRules.TileCoord excludedTile = hasExcludedTile
                ? new SpatialRules.TileCoord(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY)
                : default;
            bool allowExcludedFallback = !hasExcludedTile;
            if (!SpatialRules.TryReserveNearestReachableInteractionTile(
                state,
                unit,
                InteractionReservationKind.Dropoff,
                dropOff.Id,
                interactionTiles,
                hasExcludedTile,
                excludedTile,
                allowExcludedFallback,
                out SpatialRules.TileCoord selected))
            {
                if (hasExcludedTile)
                {
                    SpatialRules.ClearInteractionReservation(state, unit);
                }
                return false;
            }

            approachX = selected.X;
            approachY = selected.Y;
            return true;
        }

        private static bool ShouldKeepCurrentApproachTarget(GameState state, Unit unit, Building dropOff)
        {
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateBuildingInteractionTiles(state, dropOff);
            return SpatialRules.ShouldRetainInteractionReservation(
                state,
                unit,
                InteractionReservationKind.Dropoff,
                dropOff.Id,
                interactionTiles);
        }

        private static bool IsNoProgressTimedOut(GameState state, Unit unit)
        {
            if (!unit.HasMoveTarget)
            {
                return false;
            }

            int blockedTicks = unit.LastMovedTick < 0 ? int.MaxValue : state.Tick - unit.LastMovedTick;
            return blockedTicks >= GameData.NoProgressTimeoutTicks;
        }

    }
}

