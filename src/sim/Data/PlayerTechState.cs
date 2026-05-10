using System.Collections.Generic;

namespace RtsGame.Sim.Data
{
    public sealed class PlayerTechState
    {
        public List<TechId> CompletedTechs { get; } = new List<TechId>();
        public List<ResearchQueueItem> ResearchQueue { get; } = new List<ResearchQueueItem>();
        public List<PlayerModifier> Modifiers { get; } = new List<PlayerModifier>();
    }

    public sealed class ResearchQueueItem
    {
        public TechId TechId { get; set; }
        public int ProgressTicks { get; set; }
        public int RequiredTicks { get; set; }

        public ResearchQueueItem(TechId techId, int requiredTicks)
        {
            TechId = techId;
            ProgressTicks = 0;
            RequiredTicks = requiredTicks;
        }
    }

    public sealed class PlayerModifier
    {
        public ModifierId ModifierId { get; set; }
        public int Value { get; set; }

        public PlayerModifier(ModifierId modifierId, int value)
        {
            ModifierId = modifierId;
            Value = value;
        }
    }
}
