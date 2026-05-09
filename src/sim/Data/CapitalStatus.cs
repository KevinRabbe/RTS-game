namespace RtsGame.Sim.Data
{
    public sealed class CapitalStatus
    {
        public bool HasCapitalBeenPlaced { get; set; }
        public int CapitalBuildingId { get; set; }
        public bool IsCapitalAlive { get; set; }
        public bool CapitalBonusActive { get; set; }

        public CapitalStatus()
        {
            HasCapitalBeenPlaced = false;
            CapitalBuildingId = 0;
            IsCapitalAlive = false;
            CapitalBonusActive = false;
        }
    }
}
