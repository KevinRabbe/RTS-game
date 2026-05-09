using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class PlaceTradePostCommand : ICommand
    {
        public FixedVector2 Position { get; }

        public PlaceTradePostCommand(FixedVector2 position)
        {
            Position = position;
        }

        public CommandType Type
        {
            get { return CommandType.PlaceTradePost; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteFixed(Position.X);
            writer.WriteFixed(Position.Y);
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return false;
            }

            PlayerState player = state.PlayerStates.Players[header.PlayerIndex];
            ResourceStockpile cost = GameData.GetBuildingCost(BuildingTypeId.TradePost, false);
            return player.IsConnected
                && !player.IsDefeated
                && !player.IsResigned
                && player.Resources.CanPay(cost)
                && HasCompletedTownCenter(state, header.PlayerIndex)
                && PlacementRules.CanPlaceBuilding(state, BuildingTypeId.TradePost, Position);
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            state.PlayerStates.Players[header.PlayerIndex].Resources.Subtract(GameData.GetBuildingCost(BuildingTypeId.TradePost, false));
            EntityFactory.CreateTradePost(state, header.PlayerIndex, Position, false);
        }

        private static bool HasCompletedTownCenter(GameState state, int ownerPlayerIndex)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsDead
                    && !building.IsUnderConstruction
                    && building.OwnerPlayerIndex == ownerPlayerIndex
                    && building.BuildingTypeId == BuildingTypeId.TownCenter)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
