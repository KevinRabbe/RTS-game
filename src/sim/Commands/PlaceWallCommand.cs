using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class PlaceWallCommand : ICommand
    {
        public FixedVector2 Position { get; }

        public PlaceWallCommand(FixedVector2 position)
        {
            Position = position;
        }

        public CommandType Type
        {
            get { return CommandType.PlaceWall; }
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
            ResourceStockpile cost = GameData.GetBuildingCost(BuildingTypeId.Wall, false);
            return player.IsConnected
                && !player.IsDefeated
                && !player.IsResigned
                && player.Resources.CanPay(cost)
                && PlacementRules.CanPlaceBuilding(state, BuildingTypeId.Wall, Position);
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            state.PlayerStates.Players[header.PlayerIndex].Resources.Subtract(GameData.GetBuildingCost(BuildingTypeId.Wall, false));
            EntityFactory.CreateWall(state, header.PlayerIndex, Position);
        }
    }
}
