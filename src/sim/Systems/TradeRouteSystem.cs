using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using System.Diagnostics.CodeAnalysis;

namespace RtsGame.Sim.Systems
{
    public sealed class TradeRouteSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit cart = state.EntityState.Units[i];
                if (cart.IsDead || cart.UnitTypeId != UnitTypeId.TradeCart || cart.TradeRouteAId == 0 || cart.TradeRouteBId == 0)
                {
                    continue;
                }

                if (!TryGetTradePost(state, cart.TradeDestinationId, out Building? destination))
                {
                    ClearRoute(cart);
                    continue;
                }

                if (cart.HasMoveTarget)
                {
                    continue;
                }

                if (!IsAtPosition(cart, destination.Position))
                {
                    cart.HasMoveTarget = true;
                    cart.MoveTarget = destination.Position;
                    continue;
                }

                if (cart.TradeIncomePerTrip > 0 && cart.OwnerPlayerIndex >= 0 && cart.OwnerPlayerIndex < state.PlayerStates.Players.Count)
                {
                    state.PlayerStates.Players[cart.OwnerPlayerIndex].Resources.Gold += cart.TradeIncomePerTrip;
                }

                int nextDestinationId = cart.TradeDestinationId == cart.TradeRouteAId ? cart.TradeRouteBId : cart.TradeRouteAId;
                if (!TryGetTradePost(state, nextDestinationId, out Building? nextDestination))
                {
                    ClearRoute(cart);
                    continue;
                }

                cart.TradeDestinationId = nextDestinationId;
                cart.HasMoveTarget = true;
                cart.MoveTarget = nextDestination.Position;
            }
        }

        private static bool TryGetTradePost(GameState state, int buildingId, [NotNullWhen(true)] out Building? building)
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
            return building.Id == buildingId && !building.IsDead && !building.IsUnderConstruction && building.BuildingTypeId == BuildingTypeId.TradePost;
        }

        private static void ClearRoute(Unit cart)
        {
            cart.TradeRouteAId = 0;
            cart.TradeRouteBId = 0;
            cart.TradeDestinationId = 0;
            cart.TradeIncomePerTrip = 0;
            cart.HasMoveTarget = false;
        }

        private static bool IsAtPosition(Unit unit, RtsGame.Sim.Determinism.FixedVector2 position)
        {
            return unit.Position.X.Raw == position.X.Raw && unit.Position.Y.Raw == position.Y.Raw;
        }
    }
}
