using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.Visuals
{
    public enum VisualPrimitiveKind
    {
        UnitSquare = 1,
        BuildingRectangle = 2,
        WallRectangle = 3,
        TradeRouteLine = 4,
        HealthBar = 5,
        FogOverlay = 6,
        FoodResourceCircle = 7,
        WoodResourceCircle = 8,
        GoldResourceCircle = 9
    }

    public readonly struct VisualPrimitive
    {
        public VisualPrimitiveKind Kind { get; }
        public int EntityId { get; }
        public int TypeId { get; }
        public int OwnerPlayerIndex { get; }
        public FixedVector2 Position { get; }
        public FixedVector2 EndPosition { get; }
        public Fixed Size { get; }
        public Fixed Width { get; }
        public Fixed Height { get; }
        public int CurrentHitPoints { get; }
        public int MaxHitPoints { get; }
        public bool IsCapital { get; }

        public VisualPrimitive(
            VisualPrimitiveKind kind,
            int entityId,
            int typeId,
            int ownerPlayerIndex,
            FixedVector2 position,
            FixedVector2 endPosition,
            Fixed size,
            int currentHitPoints,
            int maxHitPoints,
            bool isCapital)
            : this(
                kind,
                entityId,
                typeId,
                ownerPlayerIndex,
                position,
                endPosition,
                size,
                size,
                currentHitPoints,
                maxHitPoints,
                isCapital)
        {
        }

        public VisualPrimitive(
            VisualPrimitiveKind kind,
            int entityId,
            int typeId,
            int ownerPlayerIndex,
            FixedVector2 position,
            FixedVector2 endPosition,
            Fixed width,
            Fixed height,
            int currentHitPoints,
            int maxHitPoints,
            bool isCapital)
        {
            Kind = kind;
            EntityId = entityId;
            TypeId = typeId;
            OwnerPlayerIndex = ownerPlayerIndex;
            Position = position;
            EndPosition = endPosition;
            Width = width;
            Height = height;
            Size = width.Raw >= height.Raw ? width : height;
            CurrentHitPoints = currentHitPoints;
            MaxHitPoints = maxHitPoints;
            IsCapital = isCapital;
        }
    }
}
