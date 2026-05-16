using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Core
{
    public static class EntityFactory
    {
        public static int CreateUnit(GameState state, int ownerPlayerIndex, UnitTypeId unitTypeId, FixedVector2 position)
        {
            return CreateUnit(state, ownerPlayerIndex, unitTypeId, position, true);
        }

        public static int CreateUnit(GameState state, int ownerPlayerIndex, UnitTypeId unitTypeId, FixedVector2 position, bool countPopulation)
        {
            int index = state.EntityState.Units.Count;
            int id = state.EntityState.NextEntityId++;
            var unit = new Unit
            {
                Id = id,
                OwnerPlayerIndex = ownerPlayerIndex,
                UnitTypeId = unitTypeId,
                Position = position,
                HasMoveTarget = false,
                MoveTarget = position,
                LastMovedTick = -1,
                HitPoints = GameData.GetUnitHitPoints(unitTypeId),
                CurrentBuildTargetId = 0,
                CurrentResourceAreaId = 0,
                CurrentResourceNodeId = 0,
                TaskPhase = WorkerTaskPhase.Idle,
                ReservedInteractionKind = InteractionReservationKind.None,
                ReservedInteractionTargetId = 0,
                ReservedInteractionTileX = 0,
                ReservedInteractionTileY = 0,
                CarriedResourceType = ResourceType.None,
                CarriedAmount = 0,
                AttackTargetId = 0,
                AttackCooldownTicksRemaining = 0,
                IsSiegeDeployed = false,
                SiegeSetupTicksRemaining = 0,
                SiegeReloadTicksRemaining = 0,
                TradeRouteAId = 0,
                TradeRouteBId = 0,
                TradeDestinationId = 0,
                TradeIncomePerTrip = 0,
                DespawnTicksRemaining = 0,
                IsDead = false
            };

            state.EntityState.Units.Add(unit);
            state.EntityState.EntityLookup[id] = new EntityRef(EntityKind.Unit, index);
            if (countPopulation && ownerPlayerIndex >= 0 && ownerPlayerIndex < state.PlayerStates.Players.Count)
            {
                state.PlayerStates.Players[ownerPlayerIndex].PopulationUsed += GameData.GetUnitPopulation(unitTypeId);
            }

            return id;
        }

        public static int CreateTownCenter(GameState state, int ownerPlayerIndex, FixedVector2 position)
        {
            int index = state.EntityState.Buildings.Count;
            int id = state.EntityState.NextEntityId++;
            PlayerState player = state.PlayerStates.Players[ownerPlayerIndex];
            bool isCapital = !player.CapitalStatus.HasCapitalBeenPlaced;

            var building = new Building
            {
                Id = id,
                OwnerPlayerIndex = ownerPlayerIndex,
                BuildingTypeId = BuildingTypeId.TownCenter,
                Position = position,
                HitPoints = GameData.TownCenterHitPoints + (isCapital ? GameData.CapitalHitPointBonus : 0),
                IsUnderConstruction = true,
                BuildProgressTicks = 0,
                IsCapital = isCapital,
                DespawnTicksRemaining = 0,
                IsDead = false
            };

            state.EntityState.Buildings.Add(building);
            state.EntityState.EntityLookup[id] = new EntityRef(EntityKind.Building, index);
            if (isCapital)
            {
                player.CapitalStatus.HasCapitalBeenPlaced = true;
                player.CapitalStatus.CapitalBuildingId = id;
            }

            return id;
        }

        public static int CreateWall(GameState state, int ownerPlayerIndex, FixedVector2 position)
        {
            int index = state.EntityState.Buildings.Count;
            int id = state.EntityState.NextEntityId++;
            var building = new Building
            {
                Id = id,
                OwnerPlayerIndex = ownerPlayerIndex,
                BuildingTypeId = BuildingTypeId.Wall,
                Position = position,
                HitPoints = GameData.WallUnderConstructionHitPoints,
                IsUnderConstruction = true,
                BuildProgressTicks = 0,
                IsCapital = false,
                DespawnTicksRemaining = 0,
                IsDead = false
            };

            state.EntityState.Buildings.Add(building);
            state.EntityState.EntityLookup[id] = new EntityRef(EntityKind.Building, index);
            return id;
        }

        public static int CreateTradePost(GameState state, int ownerPlayerIndex, FixedVector2 position)
        {
            return CreateTradePost(state, ownerPlayerIndex, position, true);
        }

        public static int CreateTradePost(GameState state, int ownerPlayerIndex, FixedVector2 position, bool completed)
        {
            int index = state.EntityState.Buildings.Count;
            int id = state.EntityState.NextEntityId++;
            var building = new Building
            {
                Id = id,
                OwnerPlayerIndex = ownerPlayerIndex,
                BuildingTypeId = BuildingTypeId.TradePost,
                Position = position,
                HitPoints = completed ? GameData.TradePostHitPoints : GameData.TradePostUnderConstructionHitPoints,
                IsUnderConstruction = !completed,
                BuildProgressTicks = completed ? GameData.TradePostBuildTicks : 0,
                IsCapital = false,
                DespawnTicksRemaining = 0,
                IsDead = false
            };

            state.EntityState.Buildings.Add(building);
            state.EntityState.EntityLookup[id] = new EntityRef(EntityKind.Building, index);
            return id;
        }
    }
}
