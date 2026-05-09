using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Data
{
    public sealed class ResourceNode
    {
        public int Id { get; set; }
        public ResourceType ResourceType { get; set; }
        public FixedVector2 Position { get; set; }
        public int RemainingAmount { get; set; }
        public bool IsDepleted
        {
            get { return RemainingAmount <= 0; }
        }
    }
}
