using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Systems
{
    public sealed class CombatResolutionSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            // Contract: this system resolves explicit targets only. Auto-target
            // acquisition is handled elsewhere to keep this loop deterministic and bounded.
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit attacker = state.EntityState.Units[i];
                if (attacker.IsDead
                    || attacker.OwnerPlayerIndex < 0
                    || attacker.OwnerPlayerIndex >= state.PlayerStates.Players.Count
                    || GameData.IsSiege(attacker.UnitTypeId)
                    || GameData.IsAreaDamage(attacker.UnitTypeId))
                {
                    continue;
                }

                if (attacker.AttackTargetId != 0)
                {
                    // Explicit target cleanup is deterministic and local: invalid/dead/friendly
                    // targets clear immediately before cooldown/damage processing.
                    if (!TryGetTarget(state, attacker.AttackTargetId, out EntityTarget targetCheck)
                        || targetCheck.OwnerPlayerIndex == attacker.OwnerPlayerIndex
                        || targetCheck.IsDead)
                    {
                        attacker.AttackTargetId = 0;
                    }
                }

                if (attacker.AttackCooldownTicksRemaining > 0)
                {
                    attacker.AttackCooldownTicksRemaining--;
                    continue;
                }

                if (attacker.AttackTargetId == 0 || !TryGetTarget(state, attacker.AttackTargetId, out EntityTarget target))
                {
                    attacker.AttackTargetId = 0;
                    continue;
                }

                if (!IsInRange(attacker, target))
                {
                    continue;
                }

                int damage = TechRules.GetModifiedUnitAttackDamage(
                    attacker.UnitTypeId,
                    state.PlayerStates.Players[attacker.OwnerPlayerIndex]);
                if (damage <= 0)
                {
                    attacker.AttackTargetId = 0;
                    continue;
                }

                target.ApplyDamage(damage);
                attacker.AttackCooldownTicksRemaining = GameData.GetUnitAttackCooldownTicks(attacker.UnitTypeId);
            }
        }

        private static bool IsInRange(Unit attacker, EntityTarget target)
        {
            Fixed distance = FixedVector2.Distance(attacker.Position, target.Position);
            return distance <= GameData.GetUnitAttackRange(attacker.UnitTypeId);
        }

        private static bool TryGetTarget(GameState state, int entityId, out EntityTarget target)
        {
            target = default(EntityTarget);
            if (!state.EntityState.EntityLookup.TryGetValue(entityId, out EntityRef entityRef))
            {
                return false;
            }

            if (entityRef.Kind == EntityKind.Unit)
            {
                if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Units.Count)
                {
                    return false;
                }

                Unit unit = state.EntityState.Units[entityRef.Index];
                if (unit.Id != entityId)
                {
                    return false;
                }

                target = EntityTarget.ForUnit(unit);
                return true;
            }

            if (entityRef.Kind == EntityKind.Building)
            {
                if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Buildings.Count)
                {
                    return false;
                }

                Building building = state.EntityState.Buildings[entityRef.Index];
                if (building.Id != entityId)
                {
                    return false;
                }

                target = EntityTarget.ForBuilding(building);
                return true;
            }

            return false;
        }

        private readonly struct EntityTarget
        {
            private readonly Unit? _unit;
            private readonly Building? _building;

            public int OwnerPlayerIndex { get; }
            public FixedVector2 Position { get; }
            public bool IsDead
            {
                get
                {
                    if (_unit != null) return _unit.IsDead;
                    return _building != null && _building.IsDead;
                }
            }

            private EntityTarget(Unit? unit, Building? building, int ownerPlayerIndex, FixedVector2 position)
            {
                _unit = unit;
                _building = building;
                OwnerPlayerIndex = ownerPlayerIndex;
                Position = position;
            }

            public static EntityTarget ForUnit(Unit unit)
            {
                return new EntityTarget(unit, null, unit.OwnerPlayerIndex, unit.Position);
            }

            public static EntityTarget ForBuilding(Building building)
            {
                return new EntityTarget(null, building, building.OwnerPlayerIndex, building.Position);
            }

            public void ApplyDamage(int damage)
            {
                if (_unit != null)
                {
                    _unit.HitPoints -= damage;
                    return;
                }

                if (_building != null)
                {
                    _building.HitPoints -= damage;
                }
            }
        }
    }
}
