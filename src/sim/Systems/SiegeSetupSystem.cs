using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class SiegeSetupSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || !GameData.IsSiege(unit.UnitTypeId))
                {
                    continue;
                }

                if (unit.AttackTargetId == 0 || unit.HasMoveTarget || !HasValidBuildingTarget(state, unit))
                {
                    unit.IsSiegeDeployed = false;
                    unit.SiegeSetupTicksRemaining = 0;
                    continue;
                }

                if (unit.IsSiegeDeployed)
                {
                    continue;
                }

                if (unit.SiegeSetupTicksRemaining <= 0)
                {
                    unit.SiegeSetupTicksRemaining = GameData.GetSiegeSetupTicks(unit.UnitTypeId);
                }

                unit.SiegeSetupTicksRemaining--;
                if (unit.SiegeSetupTicksRemaining == 0)
                {
                    unit.IsSiegeDeployed = true;
                }
            }
        }

        private static bool HasValidBuildingTarget(GameState state, Unit unit)
        {
            if (!state.EntityState.EntityLookup.TryGetValue(unit.AttackTargetId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Building)
            {
                return false;
            }

            if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Buildings.Count)
            {
                return false;
            }

            Building building = state.EntityState.Buildings[entityRef.Index];
            return building.Id == unit.AttackTargetId && !building.IsDead && building.OwnerPlayerIndex != unit.OwnerPlayerIndex;
        }
    }
}
