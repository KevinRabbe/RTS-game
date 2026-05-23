using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class AttackMoveCommand : ICommand
    {
        public IReadOnlyList<int> UnitIds { get; }
        public FixedVector2 Target { get; }

        public AttackMoveCommand(IReadOnlyList<int> unitIds, FixedVector2 target)
        {
            UnitIds = unitIds;
            Target = target;
        }

        public CommandType Type
        {
            get { return CommandType.AttackMove; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteListCount(UnitIds.Count);
            for (int i = 0; i < UnitIds.Count; i++)
            {
                writer.WriteInt32(UnitIds[i]);
            }

            writer.WriteFixed(Target.X);
            writer.WriteFixed(Target.Y);
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            return CommandValidationInspector.IsAccepted(GetValidationReason(state, rules, header));
        }

        public CommandValidationReason GetValidationReason(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return CommandValidationReason.InvalidHeader;
            }

            if (UnitIds.Count == 0)
            {
                return CommandValidationReason.UnitCannotPerformAction;
            }

            int targetTileX = SpatialRules.GetTileX(Target);
            int targetTileY = SpatialRules.GetTileY(Target);
            if (SpatialRules.IsTileBlockedForUnitMovement(state, targetTileX, targetTileY))
            {
                return CommandValidationReason.TargetBlockedByStaticGeometry;
            }

            var seen = new HashSet<int>();
            bool anyEligibleUnit = false;
            bool anyUnitReachable = false;
            for (int i = 0; i < UnitIds.Count; i++)
            {
                int unitId = UnitIds[i];
                if (!seen.Add(unitId) || !TryGetUnit(state, unitId, out Unit? unit))
                {
                    return CommandValidationReason.DuplicateUnitSelection;
                }

                if (unit.OwnerPlayerIndex != header.PlayerIndex || unit.IsDead || !CanAttack(unit.UnitTypeId))
                {
                    continue;
                }

                anyEligibleUnit = true;
                int unitTileX = SpatialRules.GetTileX(unit.Position);
                int unitTileY = SpatialRules.GetTileY(unit.Position);
                if (state.PathQueries.TryNextStep(state, unit.Id, unitTileX, unitTileY, targetTileX, targetTileY, state.Tick, out _, out _))
                {
                    anyUnitReachable = true;
                }
            }

            if (!anyEligibleUnit)
            {
                return CommandValidationReason.UnitCannotPerformAction;
            }

            return anyUnitReachable
                ? CommandValidationReason.Accepted
                : CommandValidationReason.NoStaticPath;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            List<int> sortedUnitIds = StableSort.Sorted(UnitIds, (left, right) => left.CompareTo(right));
            var units = new List<Unit>();
            for (int i = 0; i < sortedUnitIds.Count; i++)
            {
                if (!TryGetUnit(state, sortedUnitIds[i], out Unit? unit))
                {
                    continue;
                }

                if (unit.OwnerPlayerIndex != header.PlayerIndex || unit.IsDead || !CanAttack(unit.UnitTypeId))
                {
                    continue;
                }

                ClearBuildAssignment(state, unit);
                SpatialRules.ClearInteractionReservation(state, unit);
                unit.CurrentResourceAreaId = 0;
                unit.CurrentResourceNodeId = 0;
                unit.AssignedResourceNodeId = 0;
                unit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
                unit.HasMoveTarget = false;
                unit.AttackTargetId = 0;
                unit.HasAttackMoveTarget = true;
                unit.AttackMoveTarget = Target;
                unit.IsSiegeDeployed = false;
                unit.SiegeSetupTicksRemaining = 0;
                unit.SiegeReloadTicksRemaining = 0;
                units.Add(unit);
            }

            int targetTileX = SpatialRules.GetTileX(Target);
            int targetTileY = SpatialRules.GetTileY(Target);
            int searchRadius = GetDestinationSearchRadius(units.Count);
            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                if (SpatialRules.TryReserveNearestReachableMoveDestinationTile(
                    state,
                    unit,
                    targetTileX,
                    targetTileY,
                    searchRadius,
                    out SpatialRules.TileCoord destination))
                {
                    unit.HasMoveTarget = true;
                    unit.MoveTarget = FixedVector2.FromInts(destination.X, destination.Y);
                }
            }
        }

        private static int GetDestinationSearchRadius(int unitCount)
        {
            int radius = unitCount / 2 + 2;
            return radius < 3 ? 3 : radius;
        }

        private static bool CanAttack(UnitTypeId unitTypeId)
        {
            return GameData.GetUnitAttackDamage(unitTypeId) > 0
                || GameData.GetAreaDamage(unitTypeId) > 0
                || GameData.GetSiegeBuildingDamage(unitTypeId) > 0;
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
    }
}
