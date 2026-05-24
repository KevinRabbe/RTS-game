using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class AttackMoveAcquisitionSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            int remainingBudget = GameData.AttackMoveAcquireBudgetPerTick;
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                if (remainingBudget <= 0)
                {
                    break;
                }

                Unit unit = state.EntityState.Units[i];
                if (!ShouldAcquireForUnit(state, unit))
                {
                    continue;
                }

                unit.NextAttackMoveAcquireTick = state.Tick + GameData.AttackMoveAcquireCadenceTicks;
                if (TryAcquireNearestEnemyUnit(state, unit, GameData.AttackMoveAcquireRadiusTiles, out int targetUnitId))
                {
                    unit.AttackTargetId = targetUnitId;
                    unit.HasMoveTarget = false;
                    remainingBudget--;
                }
            }
        }

        private static bool ShouldAcquireForUnit(GameState state, Unit unit)
        {
            return !unit.IsDead
                && unit.HasAttackMoveTarget
                && unit.AttackTargetId == 0
                && unit.OwnerPlayerIndex >= 0
                && unit.OwnerPlayerIndex < state.PlayerStates.Players.Count
                && GameData.GetUnitAttackDamage(unit.UnitTypeId) > 0
                && !GameData.IsSiege(unit.UnitTypeId)
                && !GameData.IsAreaDamage(unit.UnitTypeId)
                && state.Tick >= unit.NextAttackMoveAcquireTick;
        }

        private static bool TryAcquireNearestEnemyUnit(GameState state, Unit attacker, int searchRadiusTiles, out int targetUnitId)
        {
            targetUnitId = 0;
            int attackerTileX = SpatialRules.GetTileX(attacker.Position);
            int attackerTileY = SpatialRules.GetTileY(attacker.Position);
            long bestDistanceSquared = long.MaxValue;

            for (int offsetY = -searchRadiusTiles; offsetY <= searchRadiusTiles; offsetY++)
            {
                int tileY = attackerTileY + offsetY;
                for (int offsetX = -searchRadiusTiles; offsetX <= searchRadiusTiles; offsetX++)
                {
                    int tileX = attackerTileX + offsetX;
                    if (!SpatialRules.IsTileInBounds(state, tileX, tileY)
                        || !state.SpatialIndex.TryGetOccupiedUnitId(state, tileX, tileY, attacker.Id, out int candidateUnitId))
                    {
                        continue;
                    }

                    if (!TryGetAliveEnemyUnit(state, candidateUnitId, attacker.OwnerPlayerIndex, out Unit candidate))
                    {
                        continue;
                    }

                    long distanceSquared = (candidate.Position - attacker.Position).LengthSquaredRaw();
                    if (distanceSquared < bestDistanceSquared
                        || (distanceSquared == bestDistanceSquared && candidate.Id < targetUnitId))
                    {
                        bestDistanceSquared = distanceSquared;
                        targetUnitId = candidate.Id;
                    }
                }
            }

            return targetUnitId != 0;
        }

        private static bool TryGetAliveEnemyUnit(GameState state, int unitId, int ownerPlayerIndex, out Unit unit)
        {
            unit = null!;
            if (!state.EntityState.EntityLookup.TryGetValue(unitId, out EntityRef entityRef)
                || entityRef.Kind != EntityKind.Unit
                || entityRef.Index < 0
                || entityRef.Index >= state.EntityState.Units.Count)
            {
                return false;
            }

            Unit candidate = state.EntityState.Units[entityRef.Index];
            if (candidate.Id != unitId || candidate.IsDead || candidate.OwnerPlayerIndex == ownerPlayerIndex || candidate.OwnerPlayerIndex < 0)
            {
                return false;
            }

            unit = candidate;
            return true;
        }
    }
}
