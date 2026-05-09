namespace RtsGame.Presentation.GodotBridge
{
    public sealed class GodotFrameDto
    {
        public int Tick { get; }
        public int LocalPlayerIndex { get; }
        public GodotLocalPlayerDto LocalPlayer { get; }
        public GodotMatchDto Match { get; }
        public GodotPrimitiveDto[] Primitives { get; }
        public GodotUnitStatusDto[] UnitStatuses { get; }
        public GodotBuildingStatusDto[] BuildingStatuses { get; }

        public GodotFrameDto(
            int tick,
            int localPlayerIndex,
            GodotLocalPlayerDto localPlayer,
            GodotMatchDto match,
            GodotPrimitiveDto[] primitives,
            GodotUnitStatusDto[] unitStatuses,
            GodotBuildingStatusDto[] buildingStatuses)
        {
            Tick = tick;
            LocalPlayerIndex = localPlayerIndex;
            LocalPlayer = localPlayer;
            Match = match;
            Primitives = primitives;
            UnitStatuses = unitStatuses;
            BuildingStatuses = buildingStatuses;
        }
    }

    public sealed class GodotPrimitiveDto
    {
        public int Kind { get; }
        public int EntityId { get; }
        public int TypeId { get; }
        public int OwnerPlayerIndex { get; }
        public long XRaw { get; }
        public long YRaw { get; }
        public long EndXRaw { get; }
        public long EndYRaw { get; }
        public long SizeRaw { get; }
        public int CurrentHitPoints { get; }
        public int MaxHitPoints { get; }
        public bool IsCapital { get; }

        public GodotPrimitiveDto(
            int kind,
            int entityId,
            int typeId,
            int ownerPlayerIndex,
            long xRaw,
            long yRaw,
            long endXRaw,
            long endYRaw,
            long sizeRaw,
            int currentHitPoints,
            int maxHitPoints,
            bool isCapital)
        {
            Kind = kind;
            EntityId = entityId;
            TypeId = typeId;
            OwnerPlayerIndex = ownerPlayerIndex;
            XRaw = xRaw;
            YRaw = yRaw;
            EndXRaw = endXRaw;
            EndYRaw = endYRaw;
            SizeRaw = sizeRaw;
            CurrentHitPoints = currentHitPoints;
            MaxHitPoints = maxHitPoints;
            IsCapital = isCapital;
        }
    }

    public sealed class GodotUnitStatusDto
    {
        public int UnitId { get; }
        public int UnitTypeId { get; }
        public bool HasMoveTarget { get; }
        public long MoveTargetXRaw { get; }
        public long MoveTargetYRaw { get; }
        public int CurrentBuildTargetId { get; }
        public int CurrentResourceNodeId { get; }
        public int CarriedResourceTypeId { get; }
        public int CarriedAmount { get; }
        public int AttackTargetId { get; }
        public int AttackCooldownTicksRemaining { get; }

        public GodotUnitStatusDto(
            int unitId,
            int unitTypeId,
            bool hasMoveTarget,
            long moveTargetXRaw,
            long moveTargetYRaw,
            int currentBuildTargetId,
            int currentResourceNodeId,
            int carriedResourceTypeId,
            int carriedAmount,
            int attackTargetId,
            int attackCooldownTicksRemaining)
        {
            UnitId = unitId;
            UnitTypeId = unitTypeId;
            HasMoveTarget = hasMoveTarget;
            MoveTargetXRaw = moveTargetXRaw;
            MoveTargetYRaw = moveTargetYRaw;
            CurrentBuildTargetId = currentBuildTargetId;
            CurrentResourceNodeId = currentResourceNodeId;
            CarriedResourceTypeId = carriedResourceTypeId;
            CarriedAmount = carriedAmount;
            AttackTargetId = attackTargetId;
            AttackCooldownTicksRemaining = attackCooldownTicksRemaining;
        }
    }

    public sealed class GodotBuildingStatusDto
    {
        public int BuildingId { get; }
        public int BuildingTypeId { get; }
        public bool IsUnderConstruction { get; }
        public int BuildProgressTicks { get; }
        public int RequiredBuildTicks { get; }
        public int TrainingQueueCount { get; }
        public int TrainingUnitTypeId { get; }
        public int TrainingProgressTicks { get; }
        public int TrainingRequiredTicks { get; }

        public GodotBuildingStatusDto(
            int buildingId,
            int buildingTypeId,
            bool isUnderConstruction,
            int buildProgressTicks,
            int requiredBuildTicks,
            int trainingQueueCount,
            int trainingUnitTypeId,
            int trainingProgressTicks,
            int trainingRequiredTicks)
        {
            BuildingId = buildingId;
            BuildingTypeId = buildingTypeId;
            IsUnderConstruction = isUnderConstruction;
            BuildProgressTicks = buildProgressTicks;
            RequiredBuildTicks = requiredBuildTicks;
            TrainingQueueCount = trainingQueueCount;
            TrainingUnitTypeId = trainingUnitTypeId;
            TrainingProgressTicks = trainingProgressTicks;
            TrainingRequiredTicks = trainingRequiredTicks;
        }
    }

    public sealed class GodotLocalPlayerDto
    {
        public int Food { get; }
        public int Wood { get; }
        public int Gold { get; }
        public int PopulationUsed { get; }
        public int PopulationCap { get; }
        public bool HasCapitalBeenPlaced { get; }
        public bool IsCapitalAlive { get; }
        public bool CapitalBonusActive { get; }

        public GodotLocalPlayerDto(
            int food,
            int wood,
            int gold,
            int populationUsed,
            int populationCap,
            bool hasCapitalBeenPlaced,
            bool isCapitalAlive,
            bool capitalBonusActive)
        {
            Food = food;
            Wood = wood;
            Gold = gold;
            PopulationUsed = populationUsed;
            PopulationCap = populationCap;
            HasCapitalBeenPlaced = hasCapitalBeenPlaced;
            IsCapitalAlive = isCapitalAlive;
            CapitalBonusActive = capitalBonusActive;
        }
    }

    public sealed class GodotMatchDto
    {
        public bool IsFinished { get; }
        public int WinnerPlayerIndex { get; }
        public int FinishedTick { get; }

        public GodotMatchDto(bool isFinished, int winnerPlayerIndex, int finishedTick)
        {
            IsFinished = isFinished;
            WinnerPlayerIndex = winnerPlayerIndex;
            FinishedTick = finishedTick;
        }
    }
}
