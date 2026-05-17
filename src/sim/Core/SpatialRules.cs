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
            state.SpatialTileIndex.EnsureWarm(state);
            return state.SpatialTileIndex.IsBlockedByWall(tileX, tileY);
        }

        public static bool IsTileInsideBuildingFootprint(Building building, int tileX, int tileY)
        {
            return IsTileInsideSimulationFootprint(tileX, tileY, building.Position, GameData.GetBuildingPlacementRadiusTiles(building.BuildingTypeId));
        }

        public static bool IsTileBlockedByBuildingFootprint(GameState state, int tileX, int tileY, int ignoredBuildingId = 0)
        {
            if (ignoredBuildingId <= 0)
            {
                state.SpatialTileIndex.EnsureWarm(state);
                return state.SpatialTileIndex.IsBlockedByBuilding(tileX, tileY, 0);
            }

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
            state.SpatialTileIndex.EnsureWarm(state);
            return state.SpatialTileIndex.IsBlockedByResource(tileX, tileY);
        }

        public static bool IsTileOccupiedByLiveUnit(GameState state, int tileX, int tileY, int ignoredUnitId = 0)
        {
            if (ignoredUnitId <= 0)
            {
                state.SpatialTileIndex.EnsureWarm(state);
                return state.SpatialTileIndex.IsOccupiedByLiveUnit(tileX, tileY, 0);
            }

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

        public static bool IsTileAvailableForUnitSpawn(GameState state, int tileX, int tileY)
        {
            return !IsTileBlockedForUnitMovement(state, tileX, tileY)
                && !IsTileOccupiedByLiveUnit(state, tileX, tileY)
                && !IsTileReservedByLiveUnit(state, tileX, tileY);
        }

        public static bool IsTileReservedByLiveUnit(GameState state, int tileX, int tileY, int ignoredUnitId = 0)
        {
            if (ignoredUnitId <= 0)
            {
                state.SpatialTileIndex.EnsureWarm(state);
                return state.SpatialTileIndex.IsReservedByLiveUnit(tileX, tileY, 0);
            }

            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead
                    || unit.Id == ignoredUnitId
                    || unit.ReservedInteractionKind == InteractionReservationKind.None)
                {
                    continue;
                }

                if (unit.ReservedInteractionTileX == tileX && unit.ReservedInteractionTileY == tileY)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileInsideResourceFootprint(ResourceNode node, int tileX, int tileY)
        {
            return IsTileInsideSimulationFootprint(tileX, tileY, node.Position, GetResourceFootprintRadiusTiles(node));
        }

        public static List<TileCoord> EnumerateResourceFootprintTiles(GameState state, ResourceNode node)
        {
            return EnumerateSimulationFootprintTiles(state, node.Position, GetResourceFootprintRadiusTiles(node));
        }

        public static List<TileCoord> EnumerateBuildingFootprintTiles(GameState state, Building building)
        {
            return EnumerateSimulationFootprintTiles(state, building.Position, GameData.GetBuildingPlacementRadiusTiles(building.BuildingTypeId));
        }

        public static bool IsTileAdjacentToResourceFootprint(ResourceNode node, int tileX, int tileY)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if ((offsetX != 0 || offsetY != 0)
                        && IsTileInsideResourceFootprint(node, tileX + offsetX, tileY + offsetY))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static List<TileCoord> EnumerateResourceInteractionTiles(GameState state, ResourceNode node)
        {
            return EnumerateInteractionTilesForFootprint(state, EnumerateResourceFootprintTiles(state, node), 0);
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
            return EnumerateInteractionTilesForFootprint(state, EnumerateBuildingFootprintTiles(state, building), building.Id);
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

        public static bool TryReserveNearestReachableInteractionTile(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            List<TileCoord> interactionTiles,
            out TileCoord selected)
        {
            return TryReserveNearestReachableInteractionTile(
                state,
                unit,
                kind,
                targetId,
                interactionTiles,
                false,
                default,
                true,
                out selected);
        }

        public static bool TryReserveNearestReachableInteractionTile(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            List<TileCoord> interactionTiles,
            bool hasExcludedTile,
            TileCoord excludedTile,
            out TileCoord selected)
        {
            return TryReserveNearestReachableInteractionTile(
                state,
                unit,
                kind,
                targetId,
                interactionTiles,
                hasExcludedTile,
                excludedTile,
                true,
                out selected);
        }

        public static bool TryReserveNearestReachableInteractionTile(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            List<TileCoord> interactionTiles,
            bool hasExcludedTile,
            TileCoord excludedTile,
            bool allowExcludedFallback,
            out TileCoord selected)
        {
            if (TryReserveNearestReachableInteractionTileCore(
                state,
                unit,
                kind,
                targetId,
                interactionTiles,
                hasExcludedTile,
                excludedTile,
                out selected))
            {
                return true;
            }

            if (!hasExcludedTile || !allowExcludedFallback)
            {
                return false;
            }

            return TryReserveNearestReachableInteractionTileCore(
                state,
                unit,
                kind,
                targetId,
                interactionTiles,
                false,
                excludedTile,
                out selected);
        }

        public static bool IsInteractionReservationTimedOut(GameState state, Unit unit, InteractionReservationKind kind, int targetId)
        {
            if (unit.ReservedInteractionKind != kind || unit.ReservedInteractionTargetId != targetId)
            {
                return false;
            }

            int blockedTicks = unit.LastMovedTick < 0 ? int.MaxValue : state.Tick - unit.LastMovedTick;
            if (blockedTicks < GameData.InteractionTargetRetargetBlockedTicks)
            {
                return false;
            }

            if (!unit.HasMoveTarget)
            {
                return true;
            }

            return GetTileX(unit.MoveTarget) == unit.ReservedInteractionTileX
                && GetTileY(unit.MoveTarget) == unit.ReservedInteractionTileY;
        }

        private static bool TryReserveNearestReachableInteractionTileCore(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            List<TileCoord> interactionTiles,
            bool hasExcludedTile,
            TileCoord excludedTile,
            out TileCoord selected)
        {
            selected = default;
            int unitTileX = GetTileX(unit.Position);
            int unitTileY = GetTileY(unit.Position);
            int bestPathCost = int.MaxValue;
            int bestDistance = int.MaxValue;
            int bestCongestion = int.MaxValue;
            bool found = false;
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                TileCoord tile = interactionTiles[i];
                if (hasExcludedTile && tile.X == excludedTile.X && tile.Y == excludedTile.Y)
                {
                    continue;
                }

                if (!IsInteractionSlotAvailableForUnit(state, unit, kind, targetId, tile.X, tile.Y))
                {
                    continue;
                }

                if (!DeterministicPathfinder.TryFindPathCost(state, unitTileX, unitTileY, tile.X, tile.Y, out int pathCost))
                {
                    continue;
                }

                int distance = Abs(unitTileX - tile.X) + Abs(unitTileY - tile.Y);
                int congestion = CountNearbyTraffic(state, unit, tile.X, tile.Y);
                if (!found
                    || pathCost < bestPathCost
                    || (pathCost == bestPathCost && distance < bestDistance)
                    || (pathCost == bestPathCost && distance == bestDistance && congestion < bestCongestion)
                    || (pathCost == bestPathCost && distance == bestDistance && congestion == bestCongestion && CompareTiles(tile, selected) < 0)
                    || (pathCost == bestPathCost && distance == bestDistance && congestion == bestCongestion && CompareTiles(tile, selected) == 0 && targetId < unit.ReservedInteractionTargetId))
                {
                    selected = tile;
                    bestPathCost = pathCost;
                    bestDistance = distance;
                    bestCongestion = congestion;
                    found = true;
                }
            }

            if (found)
            {
                ReserveInteractionSlot(state, unit, kind, targetId, selected);
            }

            return found;
        }

        public static bool ContainsInteractionTile(List<TileCoord> interactionTiles, int tileX, int tileY)
        {
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                if (interactionTiles[i].X == tileX && interactionTiles[i].Y == tileY)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ShouldRetainInteractionMoveTarget(GameState state, Unit unit, List<TileCoord> interactionTiles)
        {
            if (!unit.HasMoveTarget)
            {
                return false;
            }

            int targetX = GetTileX(unit.MoveTarget);
            int targetY = GetTileY(unit.MoveTarget);
            if (!ContainsInteractionTile(interactionTiles, targetX, targetY))
            {
                return false;
            }

            if (IsTileOccupiedByLiveUnit(state, targetX, targetY, unit.Id))
            {
                return false;
            }

            int blockedTicks = unit.LastMovedTick < 0 ? int.MaxValue : state.Tick - unit.LastMovedTick;
            if (blockedTicks >= GameData.InteractionTargetRetargetBlockedTicks)
            {
                return false;
            }

            return true;
        }

        public static bool ShouldRetainInteractionReservation(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            List<TileCoord> interactionTiles)
        {
            if (unit.ReservedInteractionKind != kind
                || unit.ReservedInteractionTargetId != targetId
                || !ContainsInteractionTile(interactionTiles, unit.ReservedInteractionTileX, unit.ReservedInteractionTileY))
            {
                return false;
            }

            if (!IsInteractionSlotAvailableForUnit(
                state,
                unit,
                kind,
                targetId,
                unit.ReservedInteractionTileX,
                unit.ReservedInteractionTileY))
            {
                return false;
            }

            int unitTileX = GetTileX(unit.Position);
            int unitTileY = GetTileY(unit.Position);
            bool alreadyAtSlot = unitTileX == unit.ReservedInteractionTileX && unitTileY == unit.ReservedInteractionTileY;
            int blockedTicks = unit.LastMovedTick < 0 ? int.MaxValue : state.Tick - unit.LastMovedTick;
            if (!alreadyAtSlot && blockedTicks >= GameData.InteractionTargetRetargetBlockedTicks)
            {
                if (!DeterministicPathfinder.TryFindNextTile(
                    state,
                    unitTileX,
                    unitTileY,
                    unit.ReservedInteractionTileX,
                    unit.ReservedInteractionTileY,
                    out _,
                    out _))
                {
                    return false;
                }
            }

            if (unit.HasMoveTarget
                && GetTileX(unit.MoveTarget) == unit.ReservedInteractionTileX
                && GetTileY(unit.MoveTarget) == unit.ReservedInteractionTileY)
            {
                if (blockedTicks >= GameData.InteractionTargetRetargetBlockedTicks)
                {
                    return false;
                }
            }

            return true;
        }

        public static void ReserveInteractionSlot(GameState state, Unit unit, InteractionReservationKind kind, int targetId, TileCoord tile)
        {
            bool hadReservation = unit.ReservedInteractionKind != InteractionReservationKind.None;
            int previousTileKey = EncodeTileKey(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
            unit.ReservedInteractionKind = kind;
            unit.ReservedInteractionTargetId = targetId;
            unit.ReservedInteractionTileX = tile.X;
            unit.ReservedInteractionTileY = tile.Y;
            int nextTileKey = EncodeTileKey(tile.X, tile.Y);
            state.SpatialTileIndex.ApplyReservationChange(previousTileKey, hadReservation, nextTileKey, true);
        }

        public static bool TryReserveNearestReachableMoveDestinationTile(
            GameState state,
            Unit unit,
            int targetTileX,
            int targetTileY,
            int searchRadius,
            out TileCoord selected)
        {
            selected = default;
            int unitTileX = GetTileX(unit.Position);
            int unitTileY = GetTileY(unit.Position);
            int bestPathCost = int.MaxValue;
            int bestTargetDistance = int.MaxValue;
            int bestUnitDistance = int.MaxValue;
            bool found = false;

            for (int radius = 0; radius <= searchRadius; radius++)
            {
                for (int y = targetTileY - radius; y <= targetTileY + radius; y++)
                {
                    for (int x = targetTileX - radius; x <= targetTileX + radius; x++)
                    {
                        if (Max(Abs(x - targetTileX), Abs(y - targetTileY)) != radius)
                        {
                            continue;
                        }

                        if ((x != targetTileX || y != targetTileY) && x == unitTileX && y == unitTileY)
                        {
                            continue;
                        }

                        if (!IsMoveDestinationSlotAvailableForUnit(state, unit, x, y))
                        {
                            continue;
                        }

                        if (!DeterministicPathfinder.TryFindPathCost(state, unitTileX, unitTileY, x, y, out int pathCost))
                        {
                            continue;
                        }

                        int targetDistance = Abs(x - targetTileX) + Abs(y - targetTileY);
                        int unitDistance = Abs(x - unitTileX) + Abs(y - unitTileY);
                        var candidate = new TileCoord(x, y);
                        if (!found
                            || targetDistance < bestTargetDistance
                            || (targetDistance == bestTargetDistance && pathCost < bestPathCost)
                            || (targetDistance == bestTargetDistance && pathCost == bestPathCost && unitDistance < bestUnitDistance)
                            || (targetDistance == bestTargetDistance && pathCost == bestPathCost && unitDistance == bestUnitDistance && CompareTiles(candidate, selected) < 0))
                        {
                            selected = candidate;
                            bestPathCost = pathCost;
                            bestTargetDistance = targetDistance;
                            bestUnitDistance = unitDistance;
                            found = true;
                        }
                    }
                }
            }

            if (found)
            {
                ReserveInteractionSlot(state, unit, InteractionReservationKind.MoveDestination, EncodeTileKey(targetTileX, targetTileY), selected);
            }

            return found;
        }

        public static void ClearInteractionReservation(GameState state, Unit unit)
        {
            bool hadReservation = unit.ReservedInteractionKind != InteractionReservationKind.None;
            int previousTileKey = EncodeTileKey(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
            unit.ReservedInteractionKind = InteractionReservationKind.None;
            unit.ReservedInteractionTargetId = 0;
            unit.ReservedInteractionTileX = 0;
            unit.ReservedInteractionTileY = 0;
            state.SpatialTileIndex.ApplyReservationChange(previousTileKey, hadReservation, 0, false);
        }

        public static bool HasReservedInteractionSlot(Unit unit, InteractionReservationKind kind, int targetId)
        {
            return unit.ReservedInteractionKind == kind && unit.ReservedInteractionTargetId == targetId;
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
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if ((offsetX != 0 || offsetY != 0)
                        && IsTileInsideBuildingFootprint(building, tileX + offsetX, tileY + offsetY))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsInteractionSlotAvailableForUnit(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            int tileX,
            int tileY)
        {
            if (IsTileBlockedForUnitMovement(state, tileX, tileY)
                || IsTileOccupiedByLiveUnit(state, tileX, tileY, unit.Id))
            {
                return false;
            }

            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit other = state.EntityState.Units[i];
                if (other.IsDead
                    || other.Id == unit.Id
                    || other.ReservedInteractionKind == InteractionReservationKind.None)
                {
                    continue;
                }

                if (other.ReservedInteractionTileX == tileX && other.ReservedInteractionTileY == tileY)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsMoveDestinationSlotAvailableForUnit(GameState state, Unit unit, int tileX, int tileY)
        {
            return !IsTileBlockedForUnitMovement(state, tileX, tileY)
                && !IsTileOccupiedByLiveUnit(state, tileX, tileY, unit.Id)
                && !IsTileReservedByLiveUnit(state, tileX, tileY, unit.Id);
        }

        private static bool IsInsideRadius(FixedVector2 position, FixedVector2 center, int radiusTiles)
        {
            long radiusRaw = Fixed.FromInt(radiusTiles).Raw;
            long radiusSquaredRaw = checked(radiusRaw * radiusRaw);
            return (position - center).LengthSquaredRaw() < radiusSquaredRaw;
        }

        private static List<TileCoord> EnumerateSimulationFootprintTiles(GameState state, FixedVector2 position, int radiusTiles)
        {
            int centerX = GetTileX(position);
            int centerY = GetTileY(position);
            var tiles = new List<TileCoord>();
            for (int y = centerY - radiusTiles; y <= centerY + radiusTiles; y++)
            {
                for (int x = centerX - radiusTiles; x <= centerX + radiusTiles; x++)
                {
                    if (IsTileInBounds(state, x, y) && IsTileInsideSimulationFootprint(x, y, position, radiusTiles))
                    {
                        tiles.Add(new TileCoord(x, y));
                    }
                }
            }

            SortTiles(tiles);
            return tiles;
        }

        private static bool IsTileInsideSimulationFootprint(int tileX, int tileY, FixedVector2 position, int radiusTiles)
        {
            return IsInsideRadius(FixedVector2.FromInts(tileX, tileY), position, radiusTiles);
        }

        private static List<TileCoord> EnumerateInteractionTilesForFootprint(GameState state, List<TileCoord> footprintTiles, int ignoredBuildingId)
        {
            var tiles = new List<TileCoord>();
            for (int i = 0; i < footprintTiles.Count; i++)
            {
                TileCoord footprint = footprintTiles[i];
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        int x = footprint.X + offsetX;
                        int y = footprint.Y + offsetY;
                        if (!IsTileInBounds(state, x, y)
                            || ContainsTile(footprintTiles, x, y)
                            || ContainsTile(tiles, x, y)
                            || IsTileBlockedForUnitMovement(state, x, y, ignoredBuildingId))
                        {
                            continue;
                        }

                        tiles.Add(new TileCoord(x, y));
                    }
                }
            }

            SortTiles(tiles);
            return tiles;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int Max(int left, int right)
        {
            return left > right ? left : right;
        }

        private static int CompareTiles(TileCoord left, TileCoord right)
        {
            int yCompare = left.Y.CompareTo(right.Y);
            return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
        }

        private static void SortTiles(List<TileCoord> tiles)
        {
            tiles.Sort((left, right) => CompareTiles(left, right));
        }

        private static bool ContainsTile(List<TileCoord> tiles, int tileX, int tileY)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].X == tileX && tiles[i].Y == tileY)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountNearbyTraffic(GameState state, Unit unit, int tileX, int tileY)
        {
            int count = 0;
            for (int y = tileY - 1; y <= tileY + 1; y++)
            {
                for (int x = tileX - 1; x <= tileX + 1; x++)
                {
                    if (x == tileX && y == tileY)
                    {
                        continue;
                    }

                    if (IsTileOccupiedByLiveUnit(state, x, y, unit.Id)
                        || IsTileReservedByLiveUnit(state, x, y, unit.Id))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static int EncodeTileKey(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
        }

        private static int EncodeTile(int tileX, int tileY)
        {
            return EncodeTileKey(tileX, tileY);
        }

        private static int GetResourceFootprintRadiusTiles(ResourceNode node)
        {
            GatherProfile profile = GameData.GetGatherProfile(node.GatherProfileId);
            return profile.Id == GatherProfileId.None ? GameData.ResourcePlacementRadiusTiles : profile.FootprintRadiusTiles;
        }
    }
}

