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
                if (SpatialRules.IsUnitInBuildInteractionRange(unit, building))
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
            var interactionTiles = SpatialRules.EnumerateBuildInteractionTiles(state, building);
            var reserved = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < builderIndexes.Length; i++)
            {
                Unit unit = state.EntityState.Units[builderIndexes[i]];
                if (SpatialRules.IsUnitInBuildInteractionRange(unit, building))
                {
                    unit.HasMoveTarget = false;
                    continue;
                }

                if (TryChooseApproachTile(state, unit, interactionTiles, reserved, out int tileX, out int tileY))
                {
                    unit.HasMoveTarget = true;
                    unit.MoveTarget = FixedVector2.FromInts(tileX, tileY);
                    reserved.Add(EncodeTile(tileX, tileY));
                }
            }
        }

        private static bool TryChooseApproachTile(
            GameState state,
            Unit unit,
            System.Collections.Generic.List<SpatialRules.TileCoord> interactionTiles,
            System.Collections.Generic.HashSet<int> reserved,
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
                SpatialRules.TileCoord tile = interactionTiles[i];
                int key = EncodeTile(tile.X, tile.Y);
                if (reserved.Contains(key) || SpatialRules.IsTileOccupiedByLiveUnit(state, tile.X, tile.Y, unit.Id))
                {
                    continue;
                }

                if (!DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, tile.X, tile.Y, out _, out _))
                {
                    continue;
                }

                int score = Abs(unitTileX - tile.X) + Abs(unitTileY - tile.Y);
                if (!found || score < bestScore || (score == bestScore && (tile.Y < selectedY || (tile.Y == selectedY && tile.X < selectedX))))
                {
                    selectedX = tile.X;
                    selectedY = tile.Y;
                    bestScore = score;
                    found = true;
                }
            }

            return found;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int EncodeTile(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
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
