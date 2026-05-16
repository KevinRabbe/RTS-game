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
            var reservedApproachTiles = new HashSet<int>();
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
                    continue;
                }

                if (!SpatialRules.IsUnitInBuildingInteractionRange(unit, dropOff))
                {
                    if (ShouldKeepCurrentApproachTarget(state, unit, dropOff))
                    {
                        reservedApproachTiles.Add(EncodeTile(SpatialRules.GetTileX(unit.MoveTarget), SpatialRules.GetTileY(unit.MoveTarget)));
                        continue;
                    }

                    if (TryChooseDropOffApproachTile(state, unit, dropOff, reservedApproachTiles, out int approachX, out int approachY))
                    {
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(approachX, approachY);
                        reservedApproachTiles.Add(EncodeTile(approachX, approachY));
                    }

                    continue;
                }

                unit.HasMoveTarget = false;
                state.PlayerStates.Players[unit.OwnerPlayerIndex].Resources.Add(unit.CarriedResourceType, unit.CarriedAmount);
                unit.CarriedAmount = 0;
                unit.CarriedResourceType = ResourceType.None;
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

        private static bool TryChooseDropOffApproachTile(GameState state, Unit unit, Building dropOff, HashSet<int> reservedApproachTiles, out int approachX, out int approachY)
        {
            approachX = 0;
            approachY = 0;
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateBuildingInteractionTiles(state, dropOff);
            if (!SpatialRules.TryChooseNearestReachableInteractionTile(state, unit, interactionTiles, reservedApproachTiles, out SpatialRules.TileCoord selected))
            {
                return false;
            }

            approachX = selected.X;
            approachY = selected.Y;
            return true;
        }

        private static bool ShouldKeepCurrentApproachTarget(GameState state, Unit unit, Building dropOff)
        {
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateBuildingInteractionTiles(state, dropOff);
            return SpatialRules.ShouldRetainInteractionMoveTarget(state, unit, interactionTiles);
        }

        private static int EncodeTile(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
        }
    }
}
