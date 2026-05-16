using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class MoveUnitsCommand : ICommand
    {
        public IReadOnlyList<int> UnitIds { get; }
        public FixedVector2 Target { get; }

        public MoveUnitsCommand(IReadOnlyList<int> unitIds, FixedVector2 target)
        {
            UnitIds = unitIds;
            Target = target;
        }

        public CommandType Type
        {
            get { return CommandType.MoveUnits; }
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
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return false;
            }

            if (UnitIds.Count == 0)
            {
                return false;
            }

            int targetTileX = SpatialRules.GetTileX(Target);
            int targetTileY = SpatialRules.GetTileY(Target);
            if (SpatialRules.IsTileBlockedForUnitMovement(state, targetTileX, targetTileY))
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

                if (unit.OwnerPlayerIndex != header.PlayerIndex || unit.IsDead)
                {
                    return false;
                }

                int unitTileX = SpatialRules.GetTileX(unit.Position);
                int unitTileY = SpatialRules.GetTileY(unit.Position);
                if (!DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, targetTileX, targetTileY, out _, out _))
                {
                    return false;
                }
            }

            return true;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            List<int> sortedUnitIds = StableSort.Sorted(UnitIds, (left, right) => left.CompareTo(right));
            for (int i = 0; i < sortedUnitIds.Count; i++)
            {
                Unit unit = GetUnit(state, sortedUnitIds[i]);
                ClearBuildAssignment(state, unit);
                SpatialRules.ClearInteractionReservation(unit);
                unit.CurrentResourceNodeId = 0;
                unit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
                unit.HasMoveTarget = true;
                unit.MoveTarget = Target;
                unit.AttackTargetId = 0;
                unit.IsSiegeDeployed = false;
                unit.SiegeSetupTicksRemaining = 0;
                unit.SiegeReloadTicksRemaining = 0;
            }
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

        private static Unit GetUnit(GameState state, int unitId)
        {
            TryGetUnit(state, unitId, out Unit? unit);
            return unit!;
        }
    }
}
