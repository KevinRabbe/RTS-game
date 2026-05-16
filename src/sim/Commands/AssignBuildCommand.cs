using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class AssignBuildCommand : ICommand
    {
        public int TargetBuildingId { get; }
        public IReadOnlyList<int> UnitIds { get; }

        public AssignBuildCommand(int targetBuildingId, IReadOnlyList<int> unitIds)
        {
            TargetBuildingId = targetBuildingId;
            UnitIds = unitIds;
        }

        public CommandType Type
        {
            get { return CommandType.AssignBuild; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteInt32(TargetBuildingId);
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

            if (UnitIds.Count == 0 || !TryGetBuilding(state, TargetBuildingId, out Building? building))
            {
                return false;
            }

            if (building.OwnerPlayerIndex != header.PlayerIndex || building.IsDead || !building.IsUnderConstruction)
            {
                return false;
            }

            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateBuildInteractionTiles(state, building);
            if (interactionTiles.Count == 0)
            {
                return false;
            }

            var seen = new HashSet<int>();
            bool anyUnitReachable = false;
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

                if (CanUnitReachAnyInteractionTile(state, unit, interactionTiles))
                {
                    anyUnitReachable = true;
                }
            }

            return anyUnitReachable;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            Building building = GetBuilding(state, TargetBuildingId);
            List<int> sortedUnitIds = StableSort.Sorted(UnitIds, (left, right) => left.CompareTo(right));
            List<SpatialRules.TileCoord> availableTiles = SpatialRules.EnumerateBuildInteractionTiles(state, building);

            for (int i = 0; i < sortedUnitIds.Count; i++)
            {
                int unitId = sortedUnitIds[i];
                Unit unit = GetUnit(state, unitId);
                ClearPreviousBuildAssignment(state, unit);
                SpatialRules.ClearInteractionReservation(unit);
                unit.CurrentBuildTargetId = TargetBuildingId;

                if (!building.AssignedBuilderIds.Contains(unitId))
                {
                    building.AssignedBuilderIds.Add(unitId);
                }

                unit.CurrentResourceNodeId = 0;
                unit.AttackTargetId = 0;
                unit.IsSiegeDeployed = false;
                unit.SiegeSetupTicksRemaining = 0;
                unit.SiegeReloadTicksRemaining = 0;

                if (TryChooseBuildApproachTile(state, unit, availableTiles, out SpatialRules.TileCoord approachTile))
                {
                    unit.HasMoveTarget = true;
                    unit.MoveTarget = FixedVector2.FromInts(approachTile.X, approachTile.Y);
                }
            }

            building.AssignedBuilderIds.Sort();
        }

        private static bool TryChooseBuildApproachTile(
            GameState state,
            Unit unit,
            List<SpatialRules.TileCoord> availableTiles,
            out SpatialRules.TileCoord selected)
        {
            return SpatialRules.TryReserveNearestReachableInteractionTile(
                state,
                unit,
                InteractionReservationKind.BuildSite,
                unit.CurrentBuildTargetId,
                availableTiles,
                out selected);
        }

        private static bool CanUnitReachAnyInteractionTile(GameState state, Unit unit, List<SpatialRules.TileCoord> interactionTiles)
        {
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                SpatialRules.TileCoord tile = interactionTiles[i];
                if (SpatialRules.IsTileOccupiedByLiveUnit(state, tile.X, tile.Y, unit.Id))
                {
                    continue;
                }

                if (DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, tile.X, tile.Y, out _, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ClearPreviousBuildAssignment(GameState state, Unit unit)
        {
            int previousTargetId = unit.CurrentBuildTargetId;
            if (previousTargetId == 0 || !TryGetBuilding(state, previousTargetId, out Building? previous))
            {
                return;
            }

            previous.AssignedBuilderIds.Remove(unit.Id);
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

        private static Building GetBuilding(GameState state, int buildingId)
        {
            TryGetBuilding(state, buildingId, out Building? building);
            return building!;
        }

        private static Unit GetUnit(GameState state, int unitId)
        {
            TryGetUnit(state, unitId, out Unit? unit);
            return unit!;
        }

    }
}
