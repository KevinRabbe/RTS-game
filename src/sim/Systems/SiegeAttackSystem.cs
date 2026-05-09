using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using System.Diagnostics.CodeAnalysis;

namespace RtsGame.Sim.Systems
{
    public sealed class SiegeAttackSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || !GameData.IsSiege(unit.UnitTypeId) || !unit.IsSiegeDeployed)
                {
                    continue;
                }

                if (unit.SiegeReloadTicksRemaining > 0)
                {
                    unit.SiegeReloadTicksRemaining--;
                    continue;
                }

                if (!TryGetTargetBuilding(state, unit.AttackTargetId, out Building? target))
                {
                    unit.AttackTargetId = 0;
                    unit.IsSiegeDeployed = false;
                    continue;
                }

                if (target.OwnerPlayerIndex == unit.OwnerPlayerIndex || !IsInRange(unit, target))
                {
                    continue;
                }

                target.HitPoints -= GameData.GetSiegeBuildingDamage(unit.UnitTypeId);
                unit.SiegeReloadTicksRemaining = GameData.GetSiegeReloadTicks(unit.UnitTypeId);
            }
        }

        private static bool IsInRange(Unit unit, Building target)
        {
            Fixed distance = FixedVector2.Distance(unit.Position, target.Position);
            return distance <= GameData.GetSiegeAttackRange(unit.UnitTypeId);
        }

        private static bool TryGetTargetBuilding(GameState state, int buildingId, [NotNullWhen(true)] out Building? building)
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
            return building.Id == buildingId && !building.IsDead;
        }
    }
}
