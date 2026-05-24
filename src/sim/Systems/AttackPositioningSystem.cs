using System.Collections.Generic;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Systems
{
    public sealed class AttackPositioningSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit attacker = state.EntityState.Units[i];
                if (attacker.IsDead
                    || attacker.OwnerPlayerIndex < 0
                    || attacker.OwnerPlayerIndex >= state.PlayerStates.Players.Count
                    || GameData.IsSiege(attacker.UnitTypeId)
                    || GameData.IsAreaDamage(attacker.UnitTypeId)
                    || GameData.GetUnitAttackDamage(attacker.UnitTypeId) <= 0)
                {
                    if (attacker.ReservedInteractionKind == InteractionReservationKind.AttackSlot)
                    {
                        SpatialRules.ClearInteractionReservation(state, attacker);
                    }
                    continue;
                }

                bool hasAttackIntentState = attacker.AttackTargetId != 0
                    || attacker.ReservedInteractionKind == InteractionReservationKind.AttackSlot
                    || attacker.TaskPhase == WorkerTaskPhase.MovingToAttackSlot;
                if (!hasAttackIntentState && !attacker.HasAttackMoveTarget)
                {
                    continue;
                }

                if (attacker.AttackTargetId == 0 || !TryGetTarget(state, attacker.AttackTargetId, out EntityTarget target))
                {
                    if (attacker.ReservedInteractionKind == InteractionReservationKind.AttackSlot)
                    {
                        SpatialRules.ClearInteractionReservation(state, attacker);
                    }

                    if (attacker.TaskPhase == WorkerTaskPhase.MovingToAttackSlot)
                    {
                        attacker.TaskPhase = WorkerTaskPhase.Idle;
                    }

                    ResumeAttackMoveTravel(state, attacker);
                    continue;
                }

                if (target.OwnerPlayerIndex == attacker.OwnerPlayerIndex || target.IsDead)
                {
                    attacker.AttackTargetId = 0;
                    if (attacker.ReservedInteractionKind == InteractionReservationKind.AttackSlot)
                    {
                        SpatialRules.ClearInteractionReservation(state, attacker);
                    }

                    ResumeAttackMoveTravel(state, attacker);
                    continue;
                }

                if (IsInRange(attacker, target))
                {
                    if (attacker.ReservedInteractionKind == InteractionReservationKind.AttackSlot)
                    {
                        SpatialRules.ClearInteractionReservation(state, attacker);
                    }

                    attacker.HasMoveTarget = false;
                    if (attacker.TaskPhase == WorkerTaskPhase.MovingToAttackSlot || attacker.TaskPhase == WorkerTaskPhase.BlockedWaiting)
                    {
                        attacker.TaskPhase = WorkerTaskPhase.Idle;
                    }

                    continue;
                }

                List<SpatialRules.TileCoord> slots = target.BuildAttackSlots(state);
                if (slots.Count == 0)
                {
                    attacker.HasMoveTarget = false;
                    attacker.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                    continue;
                }

                if (SpatialRules.ShouldRetainInteractionReservation(
                    state,
                    attacker,
                    InteractionReservationKind.AttackSlot,
                    attacker.AttackTargetId,
                    slots))
                {
                    attacker.MoveTarget = FixedVector2.FromInts(attacker.ReservedInteractionTileX, attacker.ReservedInteractionTileY);
                    attacker.HasMoveTarget = true;
                    attacker.TaskPhase = WorkerTaskPhase.MovingToAttackSlot;
                    continue;
                }

                if (attacker.ReservedInteractionKind == InteractionReservationKind.AttackSlot)
                {
                    SpatialRules.ClearInteractionReservation(state, attacker, ReservationReleaseReason.Timeout);
                }

                if (SpatialRules.TryReserveNearestReachableInteractionTile(
                    state,
                    attacker,
                    InteractionReservationKind.AttackSlot,
                    attacker.AttackTargetId,
                    slots,
                    out SpatialRules.TileCoord selected))
                {
                    attacker.MoveTarget = FixedVector2.FromInts(selected.X, selected.Y);
                    attacker.HasMoveTarget = true;
                    attacker.TaskPhase = WorkerTaskPhase.MovingToAttackSlot;
                    continue;
                }

                // Overflow attackers keep explicit target intent but wait instead of
                // forcing stack collapse on one tile.
                attacker.HasMoveTarget = false;
                attacker.TaskPhase = WorkerTaskPhase.BlockedWaiting;
            }
        }

        private static void ResumeAttackMoveTravel(GameState state, Unit unit)
        {
            if (!unit.HasAttackMoveTarget)
            {
                unit.HasMoveTarget = false;
                if (unit.TaskPhase == WorkerTaskPhase.MovingToAttackSlot)
                {
                    unit.TaskPhase = WorkerTaskPhase.Idle;
                }

                return;
            }

            int attackMoveTargetTileX = SpatialRules.GetTileX(unit.AttackMoveTarget);
            int attackMoveTargetTileY = SpatialRules.GetTileY(unit.AttackMoveTarget);
            int currentTileX = SpatialRules.GetTileX(unit.Position);
            int currentTileY = SpatialRules.GetTileY(unit.Position);
            if (currentTileX == attackMoveTargetTileX && currentTileY == attackMoveTargetTileY)
            {
                unit.HasAttackMoveTarget = false;
                unit.HasMoveTarget = false;
                unit.TaskPhase = WorkerTaskPhase.Idle;
                if (unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)
                {
                    SpatialRules.ClearInteractionReservation(state, unit);
                }

                return;
            }

            int attackMoveReservationTarget = SpatialRules.EncodeTileKey(attackMoveTargetTileX, attackMoveTargetTileY);
            if (unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination
                && unit.ReservedInteractionTargetId == attackMoveReservationTarget
                && !SpatialRules.IsTileBlockedForUnitMovement(state, unit.ReservedInteractionTileX, unit.ReservedInteractionTileY))
            {
                unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                unit.HasMoveTarget = true;
                unit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
                return;
            }

            if (unit.ReservedInteractionKind != InteractionReservationKind.None)
            {
                SpatialRules.ClearInteractionReservation(state, unit);
            }

            if (SpatialRules.TryReserveNearestReachableMoveDestinationTile(
                state,
                unit,
                attackMoveTargetTileX,
                attackMoveTargetTileY,
                6,
                out SpatialRules.TileCoord destination))
            {
                unit.MoveTarget = FixedVector2.FromInts(destination.X, destination.Y);
                unit.HasMoveTarget = true;
                unit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
                return;
            }

            unit.HasMoveTarget = false;
            unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
        }

        private static bool IsInRange(Unit attacker, EntityTarget target)
        {
            Fixed distance = FixedVector2.Distance(attacker.Position, target.Position);
            return distance <= GameData.GetUnitAttackRange(attacker.UnitTypeId);
        }

        private static bool TryGetTarget(GameState state, int entityId, out EntityTarget target)
        {
            target = default;
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
                    if (_unit != null)
                    {
                        return _unit.IsDead;
                    }

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

            public List<SpatialRules.TileCoord> BuildAttackSlots(GameState state)
            {
                if (_building != null)
                {
                    return SpatialRules.EnumerateBuildingInteractionTiles(state, _building);
                }

                if (_unit != null)
                {
                    return BuildUnitAttackSlots(state, _unit);
                }

                return new List<SpatialRules.TileCoord>(0);
            }

            private static List<SpatialRules.TileCoord> BuildUnitAttackSlots(GameState state, Unit unit)
            {
                int tileX = SpatialRules.GetTileX(unit.Position);
                int tileY = SpatialRules.GetTileY(unit.Position);
                var tiles = new List<SpatialRules.TileCoord>(8);
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        int x = tileX + offsetX;
                        int y = tileY + offsetY;
                        if (!SpatialRules.IsTileInBounds(state, x, y) || SpatialRules.IsTileBlockedForUnitMovement(state, x, y))
                        {
                            continue;
                        }

                        tiles.Add(new SpatialRules.TileCoord(x, y));
                    }
                }

                tiles.Sort((left, right) =>
                {
                    int yCompare = left.Y.CompareTo(right.Y);
                    return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
                });
                return tiles;
            }
        }
    }
}
