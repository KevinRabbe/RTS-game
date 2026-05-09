namespace RtsGame.Sim.Data
{
    public sealed class TrainingQueueItem
    {
        public UnitTypeId UnitTypeId { get; set; }
        public int ProgressTicks { get; set; }
        public int RequiredTicks { get; set; }

        public TrainingQueueItem(UnitTypeId unitTypeId, int requiredTicks)
        {
            UnitTypeId = unitTypeId;
            RequiredTicks = requiredTicks;
            ProgressTicks = 0;
        }
    }
}
