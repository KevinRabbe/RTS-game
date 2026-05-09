using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class PlaceTownCenterCommand : ICommand
    {
        public FixedVector2 Position { get; }

        public PlaceTownCenterCommand(FixedVector2 position)
        {
            Position = position;
        }

        public CommandType Type
        {
            get { return CommandType.PlaceTownCenter; }
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
            ResourceStockpile cost = GameData.GetBuildingCost(BuildingTypeId.TownCenter, !player.CapitalStatus.HasCapitalBeenPlaced);
            return player.IsConnected
                && !player.IsDefeated
                && !player.IsResigned
                && player.Resources.CanPay(cost)
                && PlacementRules.CanPlaceBuilding(state, BuildingTypeId.TownCenter, Position);
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            PlayerState player = state.PlayerStates.Players[header.PlayerIndex];
            ResourceStockpile cost = GameData.GetBuildingCost(BuildingTypeId.TownCenter, !player.CapitalStatus.HasCapitalBeenPlaced);
            player.Resources.Subtract(cost);
            EntityFactory.CreateTownCenter(state, header.PlayerIndex, Position);
        }
    }
}
