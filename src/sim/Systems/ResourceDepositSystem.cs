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

                if (!IsInDropOffInteractionRange(unit, dropOff))
                {
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

        private static bool IsInDropOffInteractionRange(Unit unit, Building dropOff)
        {
            int tileX = SpatialRules.GetTileX(unit.Position);
            int tileY = SpatialRules.GetTileY(unit.Position);
            if (SpatialRules.IsTileInsideBuildingFootprint(dropOff, tileX, tileY))
            {
                return false;
            }

            return SpatialRules.IsTileInsideBuildingFootprint(dropOff, tileX + 1, tileY)
                || SpatialRules.IsTileInsideBuildingFootprint(dropOff, tileX - 1, tileY)
                || SpatialRules.IsTileInsideBuildingFootprint(dropOff, tileX, tileY + 1)
                || SpatialRules.IsTileInsideBuildingFootprint(dropOff, tileX, tileY - 1);
        }

        private static bool TryChooseDropOffApproachTile(GameState state, Unit unit, Building dropOff, HashSet<int> reservedApproachTiles, out int approachX, out int approachY)
        {
            approachX = 0;
            approachY = 0;
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            int centerX = SpatialRules.GetTileX(dropOff.Position);
            int centerY = SpatialRules.GetTileY(dropOff.Position);
            int radius = GameData.GetBuildingPlacementRadiusTiles(dropOff.BuildingTypeId);
            bool found = false;
            int bestScore = int.MaxValue;

            for (int y = centerY - radius - 1; y <= centerY + radius + 1; y++)
            {
                for (int x = centerX - radius - 1; x <= centerX + radius + 1; x++)
                {
                    if (!SpatialRules.IsTileInBounds(state, x, y)
                        || SpatialRules.IsTileInsideBuildingFootprint(dropOff, x, y)
                        || !IsAdjacentToBuildingFootprint(dropOff, x, y)
                        || reservedApproachTiles.Contains(EncodeTile(x, y))
                        || SpatialRules.IsTileBlockedByWall(state, x, y)
                        || SpatialRules.IsTileBlockedByBuildingFootprint(state, x, y, dropOff.Id)
                        || SpatialRules.IsTileBlockedByResource(state, x, y)
                        || SpatialRules.IsTileOccupiedByLiveUnit(state, x, y, unit.Id))
                    {
                        continue;
                    }

                    if (!DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, x, y, out _, out _))
                    {
                        continue;
                    }

                    int score = Abs(unitTileX - x) + Abs(unitTileY - y);
                    if (!found || score < bestScore || (score == bestScore && (y < approachY || (y == approachY && x < approachX))))
                    {
                        approachX = x;
                        approachY = y;
                        bestScore = score;
                        found = true;
                    }
                }
            }

            return found;
        }

        private static bool IsAdjacentToBuildingFootprint(Building building, int tileX, int tileY)
        {
            return SpatialRules.IsTileInsideBuildingFootprint(building, tileX + 1, tileY)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX - 1, tileY)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX, tileY + 1)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX, tileY - 1);
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int EncodeTile(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
        }
    }
}
