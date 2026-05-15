using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class GatherResourceCommand : ICommand
    {
        public int ResourceNodeId { get; }
        public IReadOnlyList<int> UnitIds { get; }

        public GatherResourceCommand(int resourceNodeId, IReadOnlyList<int> unitIds)
        {
            ResourceNodeId = resourceNodeId;
            UnitIds = unitIds;
        }

        public CommandType Type
        {
            get { return CommandType.GatherResource; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteInt32(ResourceNodeId);
            writer.WriteListCount(UnitIds.Count);
            for (int i = 0; i < UnitIds.Count; i++)
            {
                writer.WriteInt32(UnitIds[i]);
            }
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return false;
            }

            if (UnitIds.Count == 0 || !TryGetResourceNode(state, ResourceNodeId, out ResourceNode? node) || node.IsDepleted)
            {
                return false;
            }

            var seen = new HashSet<int>();
            for (int i = 0; i < UnitIds.Count; i++)
            {
                int unitId = UnitIds[i];
                if (!seen.Add(unitId) || !TryGetUnit(state, unitId, out Unit? unit))
                {
                    return false;
                }

                if (unit.OwnerPlayerIndex != header.PlayerIndex || unit.IsDead || unit.UnitTypeId != UnitTypeId.Villager)
                {
                    return false;
                }

                if (unit.CarriedResourceType != ResourceType.None && unit.CarriedResourceType != node.ResourceType)
                {
                    return false;
                }
            }

            return true;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            ResourceNode node = GetResourceNode(state, ResourceNodeId);
            List<int> sortedUnitIds = StableSort.Sorted(UnitIds, (left, right) => left.CompareTo(right));
            var reservedApproachTiles = new HashSet<int>();
            for (int i = 0; i < sortedUnitIds.Count; i++)
            {
                Unit unit = GetUnit(state, sortedUnitIds[i]);
                ClearBuildAssignment(state, unit);
                unit.CurrentResourceNodeId = ResourceNodeId;
                unit.AttackTargetId = 0;
                unit.IsSiegeDeployed = false;
                unit.SiegeSetupTicksRemaining = 0;
                unit.SiegeReloadTicksRemaining = 0;

                if (TryChooseResourceApproachTile(state, unit, node, reservedApproachTiles, out int approachX, out int approachY))
                {
                    unit.HasMoveTarget = true;
                    unit.MoveTarget = FixedVector2.FromInts(approachX, approachY);
                    reservedApproachTiles.Add(EncodeTile(approachX, approachY));
                }
            }
        }

        private static bool TryChooseResourceApproachTile(GameState state, Unit unit, ResourceNode node, HashSet<int> reservedApproachTiles, out int approachX, out int approachY)
        {
            approachX = 0;
            approachY = 0;
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            int nodeTileX = SpatialRules.GetTileX(node.Position);
            int nodeTileY = SpatialRules.GetTileY(node.Position);
            int bestScore = int.MaxValue;
            bool found = false;

            for (int y = nodeTileY - 2; y <= nodeTileY + 2; y++)
            {
                for (int x = nodeTileX - 2; x <= nodeTileX + 2; x++)
                {
                    if (!SpatialRules.IsTileInBounds(state, x, y)
                        || !IsAdjacentToResource(node, x, y)
                        || IsInsideResource(node, x, y)
                        || reservedApproachTiles.Contains(EncodeTile(x, y))
                        || SpatialRules.IsTileBlockedByWall(state, x, y)
                        || SpatialRules.IsTileBlockedByBuildingFootprint(state, x, y)
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

        private static bool IsAdjacentToResource(ResourceNode node, int tileX, int tileY)
        {
            return IsInsideResource(node, tileX + 1, tileY)
                || IsInsideResource(node, tileX - 1, tileY)
                || IsInsideResource(node, tileX, tileY + 1)
                || IsInsideResource(node, tileX, tileY - 1);
        }

        private static bool IsInsideResource(ResourceNode node, int tileX, int tileY)
        {
            long radiusRaw = Fixed.FromInt(GameData.ResourcePlacementRadiusTiles).Raw;
            long radiusSquaredRaw = checked(radiusRaw * radiusRaw);
            FixedVector2 tile = FixedVector2.FromInts(tileX, tileY);
            return (tile - node.Position).LengthSquaredRaw() < radiusSquaredRaw;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int EncodeTile(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
        }

        private static void ClearBuildAssignment(GameState state, Unit unit)
        {
            int previousTargetId = unit.CurrentBuildTargetId;
            unit.CurrentBuildTargetId = 0;
            if (previousTargetId == 0 || !TryGetBuilding(state, previousTargetId, out Building? building))
            {
                return;
            }

            building.AssignedBuilderIds.Remove(unit.Id);
        }

        private static bool TryGetResourceNode(GameState state, int resourceNodeId, [NotNullWhen(true)] out ResourceNode? node)
        {
            node = null;
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                if (state.EconomyState.ResourceNodes[i].Id == resourceNodeId)
                {
                    node = state.EconomyState.ResourceNodes[i];
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetBuilding(GameState state, int buildingId, [NotNullWhen(true)] out Building? building)
        {
            building = null;
            if (!state.EntityState.EntityLookup.TryGetValue(buildingId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Building)
            {
                return false;
            }

            if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Buildings.Count)
            {
                return false;
            }

            building = state.EntityState.Buildings[entityRef.Index];
            return building.Id == buildingId;
        }

        private static bool TryGetUnit(GameState state, int unitId, [NotNullWhen(true)] out Unit? unit)
        {
            unit = null;
            if (!state.EntityState.EntityLookup.TryGetValue(unitId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
            {
                return false;
            }

            if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Units.Count)
            {
                return false;
            }

            unit = state.EntityState.Units[entityRef.Index];
            return unit.Id == unitId;
        }

        private static Unit GetUnit(GameState state, int unitId)
        {
            TryGetUnit(state, unitId, out Unit? unit);
            return unit!;
        }

        private static ResourceNode GetResourceNode(GameState state, int nodeId)
        {
            TryGetResourceNode(state, nodeId, out ResourceNode? node);
            return node!;
        }
    }
}
