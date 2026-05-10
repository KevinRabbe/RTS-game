using System.Collections.Generic;

namespace RtsGame.Sim.Data
{
    public sealed class PlayerStateContainer
    {
        public List<PlayerState> Players { get; } = new List<PlayerState>();

        public static PlayerStateContainer Create(int playerCount)
        {
            var container = new PlayerStateContainer();
            for (int i = 0; i < playerCount; i++)
            {
                container.Players.Add(new PlayerState(i));
            }

            return container;
        }
    }

    public sealed class PlayerState
    {
        public int PlayerIndex { get; }
        public bool IsConnected { get; set; }
        public bool IsDefeated { get; set; }
        public bool IsResigned { get; set; }
        public int Placement { get; set; }
        public int PopulationUsed { get; set; }
        public int PopulationCap { get; set; }
        public ResourceStockpile Resources { get; }
        public CapitalStatus CapitalStatus { get; }
        public PlayerTechState TechState { get; }

        public PlayerState(int playerIndex)
        {
            PlayerIndex = playerIndex;
            IsConnected = true;
            IsDefeated = false;
            IsResigned = false;
            Placement = 0;
            PopulationUsed = 0;
            PopulationCap = 0;
            Resources = new ResourceStockpile();
            CapitalStatus = new CapitalStatus();
            TechState = new PlayerTechState();
        }
    }
}
