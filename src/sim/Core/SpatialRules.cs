using System.Collections.Generic;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Core
{
    public static class SpatialRules
    {
        public readonly struct TileCoord
        {
            public int X { get; }
            public int Y { get; }

            public TileCoord(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public static int GetTileX(FixedVector2 position)
        {
            return position.X.FloorToInt();
        }

        public static int GetTileY(FixedVector2 position)
        {
            return position.Y.FloorToInt();
        }

        public static bool IsBlockedByWall(GameState state, FixedVector2 position)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || building.BuildingTypeId != BuildingTypeId.Wall)
                {
                    continue;
                }

                if (IsInsideRadius(position, building.Position, GameData.WallPlacementRadiusTiles))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileBlockedByWall(GameState state, int tileX, int tileY)
        {
            return IsBlockedByWall(state, FixedVector2.FromInts(tileX, tileY));
        }

        public static bool IsTileInsideBuildingFootprint(Building building, int tileX, int tileY)
        {
            int radiusTiles = GameData.GetBuildingPlacementRadiusTiles(building.BuildingTypeId);
            return IsInsideRadius(FixedVector2.FromInts(tileX, tileY), building.Position, radiusTiles);
        }

        public static bool IsTileBlockedByBuildingFootprint(GameState state, int tileX, int tileY, int ignoredBuildingId = 0)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || building.Id == ignoredBuildingId)
                {
                    continue;
                }

                if (IsTileInsideBuildingFootprint(building, tileX, tileY))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileBlockedByResource(GameState state, int tileX, int tileY)
        {
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted)
                {
                    continue;
                }

                if (IsInsideRadius(FixedVector2.FromInts(tileX, tileY), node.Position, GameData.ResourcePlacementRadiusTiles))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileOccupiedByLiveUnit(GameState state, int tileX, int tileY, int ignoredUnitId = 0)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.Id == ignoredUnitId)
                {
                    continue;
                }

                if (GetTileX(unit.Position) == tileX && GetTileY(unit.Position) == tileY)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileInBounds(GameState state, int tileX, int tileY)
        {
            return tileX >= 0 && tileY >= 0 && tileX < state.MapState.WidthTiles && tileY < state.MapState.HeightTiles;
        }

        public static bool IsTileBlockedForUnitMovement(GameState state, int tileX, int tileY, int ignoredBuildingId = 0)
        {
            return !IsTileInBounds(state, tileX, tileY)
                || IsTileBlockedByWall(state, tileX, tileY)
                || IsTileBlockedByBuildingFootprint(state, tileX, tileY, ignoredBuildingId)
                || IsTileBlockedByResource(state, tileX, tileY);
        }

        public static bool IsTileInsideResourceFootprint(ResourceNode node, int tileX, int tileY)
        {
            return IsInsideRadius(FixedVector2.FromInts(tileX, tileY), node.Position, GameData.ResourcePlacementRadiusTiles);
        }

        public static bool IsTileAdjacentToResourceFootprint(ResourceNode node, int tileX, int tileY)
        {
            return IsTileInsideResourceFootprint(node, tileX + 1, tileY)
                || IsTileInsideResourceFootprint(node, tileX - 1, tileY)
                || IsTileInsideResourceFootprint(node, tileX, tileY + 1)
                || IsTileInsideResourceFootprint(node, tileX, tileY - 1);
        }

        public static List<TileCoord> EnumerateResourceInteractionTiles(GameState state, ResourceNode node)
        {
            int centerX = GetTileX(node.Position);
            int centerY = GetTileY(node.Position);
            int radius = GameData.ResourcePlacementRadiusTiles;
            var tiles = new List<TileCoord>();
            for (int y = centerY - radius - 1; y <= centerY + radius + 1; y++)
            {
                for (int x = centerX - radius - 1; x <= centerX + radius + 1; x++)
                {
                    if (!IsTileInBounds(state, x, y)
                        || IsTileInsideResourceFootprint(node, x, y)
                        || !IsTileAdjacentToResourceFootprint(node, x, y)
                        || IsTileBlockedForUnitMovement(state, x, y))
                    {
                        continue;
                    }

                    tiles.Add(new TileCoord(x, y));
                }
            }

            tiles.Sort((left, right) =>
            {
                int yCompare = left.Y.CompareTo(right.Y);
                return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
            });
            return tiles;
        }

        public static bool IsUnitInResourceInteractionRange(Unit unit, ResourceNode node)
        {
            int tileX = GetTileX(unit.Position);
            int tileY = GetTileY(unit.Position);
            if (IsTileInsideResourceFootprint(node, tileX, tileY))
            {
                return false;
            }

            return IsTileAdjacentToResourceFootprint(node, tileX, tileY);
        }

        public static List<TileCoord> EnumerateBuildingInteractionTiles(GameState state, Building building)
        {
            int centerX = GetTileX(building.Position);
            int centerY = GetTileY(building.Position);
            int radius = GameData.GetBuildingPlacementRadiusTiles(building.BuildingTypeId);
            var tiles = new List<TileCoord>();
            for (int y = centerY - radius - 1; y <= centerY + radius + 1; y++)
            {
                for (int x = centerX - radius - 1; x <= centerX + radius + 1; x++)
                {
                    if (!IsTileInBounds(state, x, y)
                        || IsTileInsideBuildingFootprint(building, x, y)
                        || !IsAdjacentToBuildingFootprint(building, x, y)
                        || IsTileBlockedForUnitMovement(state, x, y, building.Id))
                    {
                        continue;
                    }

                    tiles.Add(new TileCoord(x, y));
                }
            }

            tiles.Sort((left, right) =>
            {
                int yCompare = left.Y.CompareTo(right.Y);
                return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
            });
            return tiles;
        }

        public static bool IsUnitInBuildingInteractionRange(Unit unit, Building building)
        {
            int tileX = GetTileX(unit.Position);
            int tileY = GetTileY(unit.Position);
            if (IsTileInsideBuildingFootprint(building, tileX, tileY))
            {
                return false;
            }

            return IsAdjacentToBuildingFootprint(building, tileX, tileY);
        }

        public static bool TryChooseNearestReachableInteractionTile(
            GameState state,
            Unit unit,
            List<TileCoord> interactionTiles,
            HashSet<int>? reservedTiles,
            out TileCoord selected)
        {
            selected = default;
            int unitTileX = GetTileX(unit.Position);
            int unitTileY = GetTileY(unit.Position);
            int bestScore = int.MaxValue;
            bool found = false;
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                TileCoord tile = interactionTiles[i];
                int tileKey = EncodeTile(tile.X, tile.Y);
                if ((reservedTiles != null && reservedTiles.Contains(tileKey))
                    || IsTileOccupiedByLiveUnit(state, tile.X, tile.Y, unit.Id))
                {
                    continue;
                }

                if (!DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, tile.X, tile.Y, out _, out _))
                {
                    continue;
                }

                int score = Abs(unitTileX - tile.X) + Abs(unitTileY - tile.Y);
                if (!found || score < bestScore || (score == bestScore && CompareTiles(tile, selected) < 0))
                {
                    selected = tile;
                    bestScore = score;
                    found = true;
                }
            }

            return found;
        }

        public static List<TileCoord> EnumerateBuildInteractionTiles(GameState state, Building building)
        {
            return EnumerateBuildingInteractionTiles(state, building);
        }

        public static bool IsUnitInBuildInteractionRange(Unit unit, Building building)
        {
            return IsUnitInBuildingInteractionRange(unit, building);
        }

        private static bool IsAdjacentToBuildingFootprint(Building building, int tileX, int tileY)
        {
            return IsTileInsideBuildingFootprint(building, tileX + 1, tileY)
                || IsTileInsideBuildingFootprint(building, tileX - 1, tileY)
                || IsTileInsideBuildingFootprint(building, tileX, tileY + 1)
                || IsTileInsideBuildingFootprint(building, tileX, tileY - 1);
        }

        private static bool IsInsideRadius(FixedVector2 position, FixedVector2 center, int radiusTiles)
        {
            long radiusRaw = Fixed.FromInt(radiusTiles).Raw;
            long radiusSquaredRaw = checked(radiusRaw * radiusRaw);
            return (position - center).LengthSquaredRaw() < radiusSquaredRaw;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int CompareTiles(TileCoord left, TileCoord right)
        {
            int yCompare = left.Y.CompareTo(right.Y);
            return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
        }

        private static int EncodeTile(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
        }
    }
}
