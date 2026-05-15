using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Systems
{
    public sealed class ConstructionSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsUnderConstruction || building.IsDead)
                {
                    continue;
                }

                int builderCount = CountValidBuildersInRange(state, building);
                if (builderCount <= 0)
                {
                    continue;
                }

                building.BuildProgressTicks += builderCount;
                int requiredTicks = GetRequiredBuildTicks(building.BuildingTypeId);
                if (building.BuildProgressTicks >= requiredTicks)
                {
                    building.BuildProgressTicks = requiredTicks;
                    building.IsUnderConstruction = false;
                    building.HitPoints = GameData.GetBuildingCompletedHitPoints(building.BuildingTypeId, building.IsCapital);
                    ClearBuilderOrders(state, building);
                    building.AssignedBuilderIds.Clear();
                }
            }
        }

        private static int CountValidBuildersInRange(GameState state, Building building)
        {
            int[] builderIndexes = CollectSortedBuilderIndexes(state, building);
            AssignBuilderApproachTargets(state, building, builderIndexes);

            int count = 0;
            for (int i = 0; i < builderIndexes.Length; i++)
            {
                Unit unit = state.EntityState.Units[builderIndexes[i]];
                if (IsInBuildInteractionRange(unit, building))
                {
                    count++;
                }
            }

            return count;
        }

        private static int[] CollectSortedBuilderIndexes(GameState state, Building building)
        {
            var indexes = new System.Collections.Generic.List<int>();
            for (int i = 0; i < building.AssignedBuilderIds.Count; i++)
            {
                int builderId = building.AssignedBuilderIds[i];
                if (!state.EntityState.EntityLookup.TryGetValue(builderId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
                {
                    continue;
                }

                if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Units.Count)
                {
                    continue;
                }

                Unit unit = state.EntityState.Units[entityRef.Index];
                if (!unit.IsDead
                    && unit.OwnerPlayerIndex == building.OwnerPlayerIndex
                    && unit.UnitTypeId == UnitTypeId.Villager
                    && unit.CurrentBuildTargetId == building.Id)
                {
                    indexes.Add(entityRef.Index);
                }
            }

            indexes.Sort((left, right) => state.EntityState.Units[left].Id.CompareTo(state.EntityState.Units[right].Id));
            return indexes.ToArray();
        }

        private static void AssignBuilderApproachTargets(GameState state, Building building, int[] builderIndexes)
        {
            var interactionTiles = EnumerateBuildInteractionTiles(state, building);
            var reserved = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < builderIndexes.Length; i++)
            {
                Unit unit = state.EntityState.Units[builderIndexes[i]];
                if (IsInBuildInteractionRange(unit, building))
                {
                    unit.HasMoveTarget = false;
                    continue;
                }

                if (TryChooseApproachTile(state, unit, interactionTiles, reserved, out int tileX, out int tileY))
                {
                    unit.HasMoveTarget = true;
                    unit.MoveTarget = FixedVector2.FromInts(tileX, tileY);
                    reserved.Add(tileX + "," + tileY);
                }
            }
        }

        private static bool TryChooseApproachTile(
            GameState state,
            Unit unit,
            System.Collections.Generic.List<(int x, int y)> interactionTiles,
            System.Collections.Generic.HashSet<string> reserved,
            out int selectedX,
            out int selectedY)
        {
            selectedX = 0;
            selectedY = 0;
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            bool found = false;
            int bestScore = int.MaxValue;

            for (int i = 0; i < interactionTiles.Count; i++)
            {
                (int x, int y) tile = interactionTiles[i];
                string key = tile.x + "," + tile.y;
                if (reserved.Contains(key) || SpatialRules.IsTileOccupiedByLiveUnit(state, tile.x, tile.y, unit.Id))
                {
                    continue;
                }

                if (!DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, tile.x, tile.y, out _, out _))
                {
                    continue;
                }

                int score = Abs(unitTileX - tile.x) + Abs(unitTileY - tile.y);
                if (!found || score < bestScore || (score == bestScore && (tile.y < selectedY || (tile.y == selectedY && tile.x < selectedX))))
                {
                    selectedX = tile.x;
                    selectedY = tile.y;
                    bestScore = score;
                    found = true;
                }
            }

            return found;
        }

        private static bool IsInBuildInteractionRange(Unit unit, Building building)
        {
            int tileX = SpatialRules.GetTileX(unit.Position);
            int tileY = SpatialRules.GetTileY(unit.Position);
            if (SpatialRules.IsTileInsideBuildingFootprint(building, tileX, tileY))
            {
                return false;
            }

            return SpatialRules.IsTileInsideBuildingFootprint(building, tileX + 1, tileY)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX - 1, tileY)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX, tileY + 1)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX, tileY - 1);
        }

        private static System.Collections.Generic.List<(int x, int y)> EnumerateBuildInteractionTiles(GameState state, Building building)
        {
            int centerX = SpatialRules.GetTileX(building.Position);
            int centerY = SpatialRules.GetTileY(building.Position);
            int radius = GameData.GetBuildingPlacementRadiusTiles(building.BuildingTypeId);
            var tiles = new System.Collections.Generic.List<(int x, int y)>();
            for (int y = centerY - radius - 1; y <= centerY + radius + 1; y++)
            {
                for (int x = centerX - radius - 1; x <= centerX + radius + 1; x++)
                {
                    if (!SpatialRules.IsTileInBounds(state, x, y)
                        || SpatialRules.IsTileInsideBuildingFootprint(building, x, y)
                        || !IsAdjacentToBuildingFootprint(building, x, y)
                        || SpatialRules.IsTileBlockedByBuildingFootprint(state, x, y, building.Id)
                        || SpatialRules.IsTileBlockedByWall(state, x, y)
                        || SpatialRules.IsTileBlockedByResource(state, x, y))
                    {
                        continue;
                    }

                    tiles.Add((x, y));
                }
            }

            tiles.Sort((left, right) => left.y != right.y ? left.y.CompareTo(right.y) : left.x.CompareTo(right.x));
            return tiles;
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

        private static void ClearBuilderOrders(GameState state, Building building)
        {
            for (int i = 0; i < building.AssignedBuilderIds.Count; i++)
            {
                int builderId = building.AssignedBuilderIds[i];
                if (!state.EntityState.EntityLookup.TryGetValue(builderId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
                {
                    continue;
                }

                if (entityRef.Index >= 0 && entityRef.Index < state.EntityState.Units.Count)
                {
                    state.EntityState.Units[entityRef.Index].CurrentBuildTargetId = 0;
                }
            }
        }

        private static int GetRequiredBuildTicks(BuildingTypeId buildingTypeId)
        {
            switch (buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    return GameData.TownCenterBuildTicks;
                case BuildingTypeId.Wall:
                    return GameData.WallBuildTicks;
                case BuildingTypeId.TradePost:
                    return GameData.TradePostBuildTicks;
                default:
                    return 1;
            }
        }
    }
}
