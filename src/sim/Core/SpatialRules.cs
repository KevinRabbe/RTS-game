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
            int tileX = GetTileX(position);
            int tileY = GetTileY(position);
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || building.BuildingTypeId != BuildingTypeId.Wall)
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

        public static bool IsTileBlockedByWall(GameState state, int tileX, int tileY)
        {
            state.SpatialTileIndex.EnsureWarm(state);
            return state.SpatialTileIndex.IsBlockedByWall(tileX, tileY);
        }

        public static bool IsTileInsideBuildingFootprint(Building building, int tileX, int tileY)
        {
            GetFootprintBounds(
                building.Position,
                GameData.GetBuildingFootprintWidthTiles(building.BuildingTypeId),
                GameData.GetBuildingFootprintHeightTiles(building.BuildingTypeId),
                out int minX,
                out int minY,
                out int maxX,
                out int maxY);
            return IsTileInsideFootprintBounds(tileX, tileY, minX, minY, maxX, maxY);
        }

        public static bool IsTileBlockedByBuildingFootprint(GameState state, int tileX, int tileY, int ignoredBuildingId = 0)
        {
            if (ignoredBuildingId <= 0)
            {
                state.SpatialTileIndex.EnsureWarm(state);
                return state.SpatialIndex.IsStaticBlocked(state, tileX, tileY, 0);
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
                return state.SpatialIndex.IsOccupied(state, tileX, tileY, 0);
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
                return state.SpatialIndex.IsReserved(state, tileX, tileY, 0);
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
            GetResourceFootprintDimensions(node, out int widthTiles, out int heightTiles);
            GetFootprintBounds(node.Position, widthTiles, heightTiles, out int minX, out int minY, out int maxX, out int maxY);
            return IsTileInsideFootprintBounds(tileX, tileY, minX, minY, maxX, maxY);
        }

        public static List<TileCoord> EnumerateResourceFootprintTiles(GameState state, ResourceNode node)
        {
            GetResourceFootprintDimensions(node, out int widthTiles, out int heightTiles);
            return EnumerateSimulationFootprintTiles(state, node.Position, widthTiles, heightTiles);
        }

        public static List<TileCoord> EnumerateBuildingFootprintTiles(GameState state, Building building)
        {
            return EnumerateSimulationFootprintTiles(
                state,
                building.Position,
                GameData.GetBuildingFootprintWidthTiles(building.BuildingTypeId),
                GameData.GetBuildingFootprintHeightTiles(building.BuildingTypeId));
        }

        public static List<TileCoord> EnumerateBuildingFootprintTiles(GameState state, BuildingTypeId buildingTypeId, FixedVector2 position)
        {
            return EnumerateSimulationFootprintTiles(
                state,
                position,
                GameData.GetBuildingFootprintWidthTiles(buildingTypeId),
                GameData.GetBuildingFootprintHeightTiles(buildingTypeId));
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

                if (!state.PathQueries.TryNextStep(state, unit.Id, unitTileX, unitTileY, tile.X, tile.Y, state.Tick, out _, out _))
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

            bool useReservationAgeTimeout = kind == InteractionReservationKind.ResourceNode
                || kind == InteractionReservationKind.Dropoff;
            if (!useReservationAgeTimeout)
            {
                int blockedTicks = unit.LastMovedTick < 0 ? int.MaxValue : state.Tick - unit.LastMovedTick;
                if (blockedTicks < GameData.NoProgressTimeoutTicks)
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

            int reservationAge = unit.LastReservationRetargetTick < 0
                ? int.MaxValue
                : state.Tick - unit.LastReservationRetargetTick;
            if (reservationAge < GameData.NoProgressTimeoutTicks)
            {
                return false;
            }

            if (reservationAge < GameData.ReservationRetargetCadenceTicks)
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
            var scopedCandidates = new List<TileCoord>(interactionTiles.Count);
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                TileCoord tile = interactionTiles[i];
                if (hasExcludedTile && tile.X == excludedTile.X && tile.Y == excludedTile.Y)
                {
                    continue;
                }
                scopedCandidates.Add(tile);
            }

            if (scopedCandidates.Count == 0)
            {
                selected = default;
                return false;
            }

            return state.TrafficReservations.TryReserve(state, unit, kind, targetId, scopedCandidates, state.Tick, out selected);
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
            if (blockedTicks >= GameData.NoProgressTimeoutTicks)
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
            bool useReservationAgeTimeout = kind == InteractionReservationKind.ResourceNode
                || kind == InteractionReservationKind.Dropoff;
            int timeoutTicks = useReservationAgeTimeout
                ? (unit.LastReservationRetargetTick < 0 ? int.MaxValue : state.Tick - unit.LastReservationRetargetTick)
                : blockedTicks;
            if (!alreadyAtSlot && timeoutTicks >= GameData.NoProgressTimeoutTicks)
            {
                if (useReservationAgeTimeout && timeoutTicks < GameData.ReservationRetargetCadenceTicks)
                {
                    return true;
                }

                if (!state.PathQueries.TryNextStep(
                    state,
                    unit.Id,
                    unitTileX,
                    unitTileY,
                    unit.ReservedInteractionTileX,
                    unit.ReservedInteractionTileY,
                    state.Tick,
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
                if (timeoutTicks >= GameData.NoProgressTimeoutTicks)
                {
                    return false;
                }
            }

            return true;
        }

        public static void ReserveInteractionSlot(GameState state, Unit unit, InteractionReservationKind kind, int targetId, TileCoord tile)
        {
            state.TrafficReservations.Reserve(state, unit, kind, targetId, tile);
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

                        if (!state.PathQueries.TryPathCost(state, unitTileX, unitTileY, x, y, state.Tick, out int pathCost))
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
            state.TrafficReservations.Release(state, unit, ReservationReleaseReason.None);
        }

        public static void ClearInteractionReservation(GameState state, Unit unit, ReservationReleaseReason reason)
        {
            state.TrafficReservations.Release(state, unit, reason);
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

        internal static bool IsInteractionSlotAvailableForUnit(
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

        private static List<TileCoord> EnumerateSimulationFootprintTiles(GameState state, FixedVector2 position, int widthTiles, int heightTiles)
        {
            GetFootprintBounds(position, widthTiles, heightTiles, out int minX, out int minY, out int maxX, out int maxY);
            var tiles = new List<TileCoord>();
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (IsTileInBounds(state, x, y))
                    {
                        tiles.Add(new TileCoord(x, y));
                    }
                }
            }

            SortTiles(tiles);
            return tiles;
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

        public static int CountNearbyTraffic(GameState state, Unit unit, int tileX, int tileY)
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

        private static void GetResourceFootprintDimensions(ResourceNode node, out int widthTiles, out int heightTiles)
        {
            GatherProfile profile = GameData.GetGatherProfile(node.GatherProfileId);
            if (profile.Id == GatherProfileId.None)
            {
                widthTiles = 1;
                heightTiles = 1;
                return;
            }

            widthTiles = profile.FootprintWidthTiles;
            heightTiles = profile.FootprintHeightTiles;
        }

        private static void GetFootprintBounds(
            FixedVector2 position,
            int widthTiles,
            int heightTiles,
            out int minX,
            out int minY,
            out int maxX,
            out int maxY)
        {
            int centerX = GetTileX(position);
            int centerY = GetTileY(position);
            int halfWidth = widthTiles / 2;
            int halfHeight = heightTiles / 2;

            minX = centerX - halfWidth;
            minY = centerY - halfHeight;
            maxX = minX + widthTiles - 1;
            maxY = minY + heightTiles - 1;
        }

        private static bool IsTileInsideFootprintBounds(int tileX, int tileY, int minX, int minY, int maxX, int maxY)
        {
            return tileX >= minX && tileX <= maxX && tileY >= minY && tileY <= maxY;
        }
    }
}

