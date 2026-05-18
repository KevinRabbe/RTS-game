using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Data
{
    public sealed class ResourceArea
    {
        public int Id { get; set; }
        public ResourceAreaType AreaType { get; set; }
        public ResourceType ResourceType { get; set; }
        public GatherProfileId GatherProfileId { get; set; }
        public FixedVector2 Position { get; set; }
    }

    public enum ResourceAreaType : ushort
    {
        None = 0,
        Forest = 1,
        BerryPatch = 2,
        GoldDeposit = 3
    }

    public enum ResourceNodeType : ushort
    {
        None = 0,
        Tree = 1,
        BerryBush = 2,
        GoldVeinSmall = 3,
        GoldVeinLarge = 4
    }

    public enum GatherProfileId : ushort
    {
        None = 0,
        Tree = 1,
        BerryBush = 2,
        GoldVeinSmall = 3,
        GoldVeinLarge = 4
    }

    public enum DropOffCategory : ushort
    {
        None = 0,
        Food = 1,
        Wood = 2,
        Gold = 3
    }

    public enum ResourceDepletedBehavior : ushort
    {
        RemoveNodeBlocker = 1
    }

    public enum ResourceAutoContinuationMode : ushort
    {
        None = 0,
        SameArea = 1
    }

    public readonly struct GatherProfile
    {
        public GatherProfileId Id { get; }
        public ResourceType ResourceType { get; }
        public ResourceNodeType NodeType { get; }
        public DropOffCategory DropOffCategory { get; }
        public int CarryCapacity { get; }
        public int GatherAmountPerTick { get; }
        public int FootprintWidthTiles { get; }
        public int FootprintHeightTiles { get; }
        public int FootprintRadiusTiles { get; }
        public int VisualRadiusTiles { get; }
        public bool BlocksMovement { get; }
        public bool BlocksPlacement { get; }
        public ResourceDepletedBehavior DepletedBehavior { get; }
        public ResourceAutoContinuationMode AutoContinuationMode { get; }

        public GatherProfile(
            GatherProfileId id,
            ResourceType resourceType,
            ResourceNodeType nodeType,
            DropOffCategory dropOffCategory,
            int carryCapacity,
            int gatherAmountPerTick,
            int footprintWidthTiles,
            int footprintHeightTiles,
            int footprintRadiusTiles,
            int visualRadiusTiles,
            bool blocksMovement,
            bool blocksPlacement,
            ResourceDepletedBehavior depletedBehavior,
            ResourceAutoContinuationMode autoContinuationMode)
        {
            Id = id;
            ResourceType = resourceType;
            NodeType = nodeType;
            DropOffCategory = dropOffCategory;
            CarryCapacity = carryCapacity;
            GatherAmountPerTick = gatherAmountPerTick;
            FootprintWidthTiles = footprintWidthTiles;
            FootprintHeightTiles = footprintHeightTiles;
            FootprintRadiusTiles = footprintRadiusTiles;
            VisualRadiusTiles = visualRadiusTiles;
            BlocksMovement = blocksMovement;
            BlocksPlacement = blocksPlacement;
            DepletedBehavior = depletedBehavior;
            AutoContinuationMode = autoContinuationMode;
        }
    }
}
