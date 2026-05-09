using System.Collections.Generic;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Systems
{
    public sealed class AreaDamageSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            var targets = new List<AreaDamageTarget>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit attacker = state.EntityState.Units[i];
                if (attacker.IsDead || !GameData.IsAreaDamage(attacker.UnitTypeId))
                {
                    continue;
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

                if (target.OwnerPlayerIndex == attacker.OwnerPlayerIndex || target.IsDead || !IsInRange(attacker, target))
                {
                    continue;
                }

                targets.Clear();
                CollectTargets(state, attacker, target.Position, targets);
                targets.Sort((left, right) => left.EntityId.CompareTo(right.EntityId));
                int damage = GameData.GetAreaDamage(attacker.UnitTypeId);
                for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    targets[targetIndex].ApplyDamage(damage);
                }

                if (targets.Count > 0)
                {
                    attacker.AttackCooldownTicksRemaining = GameData.GetAreaDamageCooldownTicks(attacker.UnitTypeId);
                }
            }
        }

        private static void CollectTargets(GameState state, Unit attacker, FixedVector2 center, List<AreaDamageTarget> targets)
        {
            Fixed radius = GameData.GetAreaDamageRadius(attacker.UnitTypeId);
            long radiusSquared = checked(radius.Raw * radius.Raw);

            if (GameData.CanAreaDamageHitUnits(attacker.UnitTypeId))
            {
                for (int i = 0; i < state.EntityState.Units.Count; i++)
                {
                    Unit target = state.EntityState.Units[i];
                    if (target.IsDead || target.OwnerPlayerIndex == attacker.OwnerPlayerIndex || !IsWithinRadius(center, target.Position, radiusSquared))
                    {
                        continue;
                    }

                    targets.Add(AreaDamageTarget.ForUnit(target));
                }
            }

            if (GameData.CanAreaDamageHitBuildings(attacker.UnitTypeId))
            {
                for (int i = 0; i < state.EntityState.Buildings.Count; i++)
                {
                    Building target = state.EntityState.Buildings[i];
                    if (target.IsDead || target.OwnerPlayerIndex == attacker.OwnerPlayerIndex || !IsWithinRadius(center, target.Position, radiusSquared))
                    {
                        continue;
                    }

                    targets.Add(AreaDamageTarget.ForBuilding(target));
                }
            }
        }

        private static bool IsWithinRadius(FixedVector2 center, FixedVector2 position, long radiusSquared)
        {
            return (position - center).LengthSquaredRaw() <= radiusSquared;
        }

        private static bool IsInRange(Unit attacker, EntityTarget target)
        {
            Fixed distance = FixedVector2.Distance(attacker.Position, target.Position);
            return distance <= GameData.GetAreaDamageAttackRange(attacker.UnitTypeId);
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
        }

        private readonly struct AreaDamageTarget
        {
            private readonly Unit? _unit;
            private readonly Building? _building;

            public int EntityId { get; }

            private AreaDamageTarget(Unit? unit, Building? building, int entityId)
            {
                _unit = unit;
                _building = building;
                EntityId = entityId;
            }

            public static AreaDamageTarget ForUnit(Unit unit)
            {
                return new AreaDamageTarget(unit, null, unit.Id);
            }

            public static AreaDamageTarget ForBuilding(Building building)
            {
                return new AreaDamageTarget(null, building, building.Id);
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
