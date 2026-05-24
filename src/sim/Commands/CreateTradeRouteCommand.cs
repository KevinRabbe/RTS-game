using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class CreateTradeRouteCommand : ICommand
    {
        public int TradeCartId { get; }
        public int TradePostAId { get; }
        public int TradePostBId { get; }

        public CreateTradeRouteCommand(int tradeCartId, int tradePostAId, int tradePostBId)
        {
            TradeCartId = tradeCartId;
            TradePostAId = tradePostAId;
            TradePostBId = tradePostBId;
        }

        public CommandType Type
        {
            get { return CommandType.CreateTradeRoute; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteInt32(TradeCartId);
            writer.WriteInt32(TradePostAId);
            writer.WriteInt32(TradePostBId);
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return false;
            }

            if (TradePostAId == TradePostBId)
            {
                return false;
            }

            if (!TryGetUnit(state, TradeCartId, out Unit? cart) || cart.OwnerPlayerIndex != header.PlayerIndex || cart.IsDead || cart.UnitTypeId != UnitTypeId.TradeCart)
            {
                return false;
            }

            return TryGetCompletedTradePost(state, TradePostAId, out _)
                && TryGetCompletedTradePost(state, TradePostBId, out _);
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            Unit cart = GetUnit(state, TradeCartId);
            Building postA = GetTradePost(state, TradePostAId);
            Building postB = GetTradePost(state, TradePostBId);
            cart.TradeRouteAId = TradePostAId;
            cart.TradeRouteBId = TradePostBId;
            cart.TradeDestinationId = TradePostBId;
            cart.TradeIncomePerTrip = CalculateIncome(postA, postB);
            cart.CurrentBuildTargetId = 0;
            cart.CurrentResourceAreaId = 0;
            cart.CurrentResourceNodeId = 0;
            cart.AssignedResourceNodeId = 0;
            SpatialRules.ClearInteractionReservation(state, cart);
            cart.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
            cart.AttackTargetId = 0;
            cart.HasAttackMoveTarget = false;
            cart.NextAttackMoveAcquireTick = 0;
            cart.HasMoveTarget = true;
            cart.MoveTarget = postB.Position;
        }

        private static int CalculateIncome(Building postA, Building postB)
        {
            Fixed distance = FixedVector2.Distance(postA.Position, postB.Position);
            int tiles = distance.FloorToInt();
            return tiles * GameData.TradeIncomePerTile;
        }

        private static bool TryGetCompletedTradePost(GameState state, int buildingId, [NotNullWhen(true)] out Building? building)
        {
            building = null;
            if (!TryGetBuilding(state, buildingId, out Building? found))
            {
                return false;
            }

            if (found.IsDead || found.IsUnderConstruction || found.BuildingTypeId != BuildingTypeId.TradePost)
            {
                return false;
            }

            building = found;
            return true;
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

        private static Building GetTradePost(GameState state, int buildingId)
        {
            TryGetCompletedTradePost(state, buildingId, out Building? building);
            return building!;
        }
    }
}

