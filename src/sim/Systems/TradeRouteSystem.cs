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

                if (!SpatialRules.IsUnitInBuildInteractionRange(cart, destination))
                {
                    if (TryChooseTradePostApproachTile(state, cart, destination, out int approachX, out int approachY))
                    {
                        cart.HasMoveTarget = true;
                        cart.MoveTarget = RtsGame.Sim.Determinism.FixedVector2.FromInts(approachX, approachY);
                    }

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
                if (TryChooseTradePostApproachTile(state, cart, nextDestination, out int nextX, out int nextY))
                {
                    cart.HasMoveTarget = true;
                    cart.MoveTarget = RtsGame.Sim.Determinism.FixedVector2.FromInts(nextX, nextY);
                }
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

        private static bool TryChooseTradePostApproachTile(GameState state, Unit cart, Building destination, out int selectedX, out int selectedY)
        {
            selectedX = 0;
            selectedY = 0;
            int unitTileX = SpatialRules.GetTileX(cart.Position);
            int unitTileY = SpatialRules.GetTileY(cart.Position);
            var interactionTiles = SpatialRules.EnumerateBuildInteractionTiles(state, destination);
            bool found = false;
            int bestScore = int.MaxValue;

            for (int i = 0; i < interactionTiles.Count; i++)
            {
                SpatialRules.TileCoord tile = interactionTiles[i];
                if (SpatialRules.IsTileOccupiedByLiveUnit(state, tile.X, tile.Y, cart.Id))
                {
                    continue;
                }

                if (!DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, tile.X, tile.Y, out _, out _))
                {
                    continue;
                }

                int score = Abs(unitTileX - tile.X) + Abs(unitTileY - tile.Y);
                if (!found || score < bestScore || (score == bestScore && (tile.Y < selectedY || (tile.Y == selectedY && tile.X < selectedX))))
                {
                    selectedX = tile.X;
                    selectedY = tile.Y;
                    bestScore = score;
                    found = true;
                }
            }

            return found;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }
    }
}
