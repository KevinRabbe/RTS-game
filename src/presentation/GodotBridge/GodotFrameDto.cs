namespace RtsGame.Presentation.GodotBridge
{
    public sealed class GodotFrameDto
    {
        public int Tick { get; }
        public string MapName { get; }
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
            : this(tick, "Unknown", localPlayerIndex, localPlayer, match, primitives, unitStatuses, buildingStatuses)
        {
        }

        public GodotFrameDto(
            int tick,
            string mapName,
            int localPlayerIndex,
            GodotLocalPlayerDto localPlayer,
            GodotMatchDto match,
            GodotPrimitiveDto[] primitives,
            GodotUnitStatusDto[] unitStatuses,
            GodotBuildingStatusDto[] buildingStatuses)
        {
            Tick = tick;
            MapName = mapName;
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
        public int PositionTileX { get; }
        public int PositionTileY { get; }
        public long PositionXRaw { get; }
        public long PositionYRaw { get; }
        public int TaskPhaseId { get; }
        public int ReservedInteractionKindId { get; }
        public int ReservedInteractionTargetId { get; }
        public int ReservedInteractionTileX { get; }
        public int ReservedInteractionTileY { get; }
        public bool InResourceInteractionRange { get; }
        public bool InDropoffInteractionRange { get; }
        public bool InBuildInteractionRange { get; }
        public int LastMovedTick { get; }
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
            : this(
                unitId,
                unitTypeId,
                hasMoveTarget,
                moveTargetXRaw,
                moveTargetYRaw,
                currentBuildTargetId,
                currentResourceNodeId,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                false,
                false,
                -1,
                carriedResourceTypeId,
                carriedAmount,
                attackTargetId,
                attackCooldownTicksRemaining)
        {
        }

        public GodotUnitStatusDto(
            int unitId,
            int unitTypeId,
            bool hasMoveTarget,
            long moveTargetXRaw,
            long moveTargetYRaw,
            int currentBuildTargetId,
            int currentResourceNodeId,
            int positionTileX,
            int positionTileY,
            long positionXRaw,
            long positionYRaw,
            int taskPhaseId,
            int reservedInteractionKindId,
            int reservedInteractionTargetId,
            int reservedInteractionTileX,
            int reservedInteractionTileY,
            bool inResourceInteractionRange,
            bool inDropoffInteractionRange,
            bool inBuildInteractionRange,
            int lastMovedTick,
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
            PositionTileX = positionTileX;
            PositionTileY = positionTileY;
            PositionXRaw = positionXRaw;
            PositionYRaw = positionYRaw;
            TaskPhaseId = taskPhaseId;
            ReservedInteractionKindId = reservedInteractionKindId;
            ReservedInteractionTargetId = reservedInteractionTargetId;
            ReservedInteractionTileX = reservedInteractionTileX;
            ReservedInteractionTileY = reservedInteractionTileY;
            InResourceInteractionRange = inResourceInteractionRange;
            InDropoffInteractionRange = inDropoffInteractionRange;
            InBuildInteractionRange = inBuildInteractionRange;
            LastMovedTick = lastMovedTick;
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
        public bool IsConnected { get; }
        public bool IsDefeated { get; }
        public bool IsResigned { get; }
        public int[] CompletedTechIds { get; }
        public GodotResearchStatusDto[] ResearchQueue { get; }
        public GodotModifierStatusDto[] Modifiers { get; }

        public GodotLocalPlayerDto(
            int food,
            int wood,
            int gold,
            int populationUsed,
            int populationCap,
            bool hasCapitalBeenPlaced,
            bool isCapitalAlive,
            bool capitalBonusActive)
            : this(
                food,
                wood,
                gold,
                populationUsed,
                populationCap,
                hasCapitalBeenPlaced,
                isCapitalAlive,
                capitalBonusActive,
                true,
                false,
                false,
                new int[0],
                new GodotResearchStatusDto[0],
                new GodotModifierStatusDto[0])
        {
        }

        public GodotLocalPlayerDto(
            int food,
            int wood,
            int gold,
            int populationUsed,
            int populationCap,
            bool hasCapitalBeenPlaced,
            bool isCapitalAlive,
            bool capitalBonusActive,
            int[] completedTechIds,
            GodotResearchStatusDto[] researchQueue,
            GodotModifierStatusDto[] modifiers)
            : this(
                food,
                wood,
                gold,
                populationUsed,
                populationCap,
                hasCapitalBeenPlaced,
                isCapitalAlive,
                capitalBonusActive,
                true,
                false,
                false,
                completedTechIds,
                researchQueue,
                modifiers)
        {
        }

        public GodotLocalPlayerDto(
            int food,
            int wood,
            int gold,
            int populationUsed,
            int populationCap,
            bool hasCapitalBeenPlaced,
            bool isCapitalAlive,
            bool capitalBonusActive,
            bool isConnected,
            bool isDefeated,
            bool isResigned,
            int[] completedTechIds,
            GodotResearchStatusDto[] researchQueue,
            GodotModifierStatusDto[] modifiers)
        {
            Food = food;
            Wood = wood;
            Gold = gold;
            PopulationUsed = populationUsed;
            PopulationCap = populationCap;
            HasCapitalBeenPlaced = hasCapitalBeenPlaced;
            IsCapitalAlive = isCapitalAlive;
            CapitalBonusActive = capitalBonusActive;
            IsConnected = isConnected;
            IsDefeated = isDefeated;
            IsResigned = isResigned;
            CompletedTechIds = completedTechIds;
            ResearchQueue = researchQueue;
            Modifiers = modifiers;
        }
    }

    public sealed class GodotResearchStatusDto
    {
        public int TechId { get; }
        public int ProgressTicks { get; }
        public int RequiredTicks { get; }

        public GodotResearchStatusDto(int techId, int progressTicks, int requiredTicks)
        {
            TechId = techId;
            ProgressTicks = progressTicks;
            RequiredTicks = requiredTicks;
        }
    }

    public sealed class GodotModifierStatusDto
    {
        public int ModifierId { get; }
        public int Value { get; }

        public GodotModifierStatusDto(int modifierId, int value)
        {
            ModifierId = modifierId;
            Value = value;
        }
    }

    public sealed class GodotMatchDto
    {
        public bool IsFinished { get; }
        public int WinnerPlayerIndex { get; }
        public int FinishedTick { get; }
        public int ExecutedCommandCount { get; }
        public int RejectedCommandCount { get; }

        public GodotMatchDto(bool isFinished, int winnerPlayerIndex, int finishedTick, int rejectedCommandCount)
            : this(isFinished, winnerPlayerIndex, finishedTick, 0, rejectedCommandCount)
        {
        }

        public GodotMatchDto(bool isFinished, int winnerPlayerIndex, int finishedTick, int executedCommandCount, int rejectedCommandCount)
        {
            IsFinished = isFinished;
            WinnerPlayerIndex = winnerPlayerIndex;
            FinishedTick = finishedTick;
            ExecutedCommandCount = executedCommandCount;
            RejectedCommandCount = rejectedCommandCount;
        }
    }
}
