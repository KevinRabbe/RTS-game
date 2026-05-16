using System.Collections.Generic;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.Snapshots
{
    public sealed class GameSnapshot
    {
        public int Tick { get; }
        public int LocalPlayerIndex { get; }
        public IReadOnlyList<UnitSnapshot> Units { get; }
        public IReadOnlyList<BuildingSnapshot> Buildings { get; }
        public IReadOnlyList<ResourceNodeSnapshot> Resources { get; }
        public LocalPlayerSnapshot LocalPlayer { get; }
        public MatchSnapshot Match { get; }

        public GameSnapshot(
            int tick,
            int localPlayerIndex,
            IReadOnlyList<UnitSnapshot> units,
            IReadOnlyList<BuildingSnapshot> buildings,
            IReadOnlyList<ResourceNodeSnapshot> resources,
            LocalPlayerSnapshot localPlayer,
            MatchSnapshot match)
        {
            Tick = tick;
            LocalPlayerIndex = localPlayerIndex;
            Units = units;
            Buildings = buildings;
            Resources = resources;
            LocalPlayer = localPlayer;
            Match = match;
        }
    }

    public readonly struct UnitSnapshot
    {
        public int Id { get; }
        public int OwnerPlayerIndex { get; }
        public UnitTypeId UnitTypeId { get; }
        public FixedVector2 Position { get; }
        public int HitPoints { get; }
        public bool HasMoveTarget { get; }
        public FixedVector2 MoveTarget { get; }
        public int CurrentBuildTargetId { get; }
        public int CurrentResourceNodeId { get; }
        public WorkerTaskPhase TaskPhase { get; }
        public InteractionReservationKind ReservedInteractionKind { get; }
        public int ReservedInteractionTargetId { get; }
        public int ReservedInteractionTileX { get; }
        public int ReservedInteractionTileY { get; }
        public bool InResourceInteractionRange { get; }
        public bool InDropoffInteractionRange { get; }
        public bool InBuildInteractionRange { get; }
        public ResourceType CarriedResourceType { get; }
        public int CarriedAmount { get; }
        public int AttackTargetId { get; }
        public int AttackCooldownTicksRemaining { get; }
        public int TradeRouteAId { get; }
        public int TradeRouteBId { get; }

        public UnitSnapshot(
            int id,
            int ownerPlayerIndex,
            UnitTypeId unitTypeId,
            FixedVector2 position,
            int hitPoints,
            bool hasMoveTarget,
            FixedVector2 moveTarget,
            int currentBuildTargetId,
            int currentResourceNodeId,
            WorkerTaskPhase taskPhase,
            InteractionReservationKind reservedInteractionKind,
            int reservedInteractionTargetId,
            int reservedInteractionTileX,
            int reservedInteractionTileY,
            bool inResourceInteractionRange,
            bool inDropoffInteractionRange,
            bool inBuildInteractionRange,
            ResourceType carriedResourceType,
            int carriedAmount,
            int attackTargetId,
            int attackCooldownTicksRemaining,
            int tradeRouteAId,
            int tradeRouteBId)
        {
            Id = id;
            OwnerPlayerIndex = ownerPlayerIndex;
            UnitTypeId = unitTypeId;
            Position = position;
            HitPoints = hitPoints;
            HasMoveTarget = hasMoveTarget;
            MoveTarget = moveTarget;
            CurrentBuildTargetId = currentBuildTargetId;
            CurrentResourceNodeId = currentResourceNodeId;
            TaskPhase = taskPhase;
            ReservedInteractionKind = reservedInteractionKind;
            ReservedInteractionTargetId = reservedInteractionTargetId;
            ReservedInteractionTileX = reservedInteractionTileX;
            ReservedInteractionTileY = reservedInteractionTileY;
            InResourceInteractionRange = inResourceInteractionRange;
            InDropoffInteractionRange = inDropoffInteractionRange;
            InBuildInteractionRange = inBuildInteractionRange;
            CarriedResourceType = carriedResourceType;
            CarriedAmount = carriedAmount;
            AttackTargetId = attackTargetId;
            AttackCooldownTicksRemaining = attackCooldownTicksRemaining;
            TradeRouteAId = tradeRouteAId;
            TradeRouteBId = tradeRouteBId;
        }
    }

    public readonly struct BuildingSnapshot
    {
        public int Id { get; }
        public int OwnerPlayerIndex { get; }
        public BuildingTypeId BuildingTypeId { get; }
        public FixedVector2 Position { get; }
        public int HitPoints { get; }
        public bool IsUnderConstruction { get; }
        public int BuildProgressTicks { get; }
        public int RequiredBuildTicks { get; }
        public int TrainingQueueCount { get; }
        public UnitTypeId TrainingUnitTypeId { get; }
        public int TrainingProgressTicks { get; }
        public int TrainingRequiredTicks { get; }
        public bool IsCapital { get; }

        public BuildingSnapshot(
            int id,
            int ownerPlayerIndex,
            BuildingTypeId buildingTypeId,
            FixedVector2 position,
            int hitPoints,
            bool isUnderConstruction,
            int buildProgressTicks,
            int requiredBuildTicks,
            int trainingQueueCount,
            UnitTypeId trainingUnitTypeId,
            int trainingProgressTicks,
            int trainingRequiredTicks,
            bool isCapital)
        {
            Id = id;
            OwnerPlayerIndex = ownerPlayerIndex;
            BuildingTypeId = buildingTypeId;
            Position = position;
            HitPoints = hitPoints;
            IsUnderConstruction = isUnderConstruction;
            BuildProgressTicks = buildProgressTicks;
            RequiredBuildTicks = requiredBuildTicks;
            TrainingQueueCount = trainingQueueCount;
            TrainingUnitTypeId = trainingUnitTypeId;
            TrainingProgressTicks = trainingProgressTicks;
            TrainingRequiredTicks = trainingRequiredTicks;
            IsCapital = isCapital;
        }
    }

    public readonly struct ResourceNodeSnapshot
    {
        public int Id { get; }
        public int ResourceAreaId { get; }
        public ResourceType ResourceType { get; }
        public ResourceNodeType NodeType { get; }
        public GatherProfileId GatherProfileId { get; }
        public FixedVector2 Position { get; }
        public int RemainingAmount { get; }

        public ResourceNodeSnapshot(int id, ResourceType resourceType, FixedVector2 position, int remainingAmount)
            : this(id, 0, resourceType, ResourceNodeType.None, GatherProfileId.None, position, remainingAmount)
        {
        }

        public ResourceNodeSnapshot(
            int id,
            int resourceAreaId,
            ResourceType resourceType,
            ResourceNodeType nodeType,
            GatherProfileId gatherProfileId,
            FixedVector2 position,
            int remainingAmount)
        {
            Id = id;
            ResourceAreaId = resourceAreaId;
            ResourceType = resourceType;
            NodeType = nodeType;
            GatherProfileId = gatherProfileId;
            Position = position;
            RemainingAmount = remainingAmount;
        }
    }

    public readonly struct LocalPlayerSnapshot
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
        public IReadOnlyList<TechId> CompletedTechs { get; }
        public IReadOnlyList<ResearchSnapshot> ResearchQueue { get; }
        public IReadOnlyList<ModifierSnapshot> Modifiers { get; }

        public LocalPlayerSnapshot(
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
                new TechId[0],
                new ResearchSnapshot[0],
                new ModifierSnapshot[0])
        {
        }

        public LocalPlayerSnapshot(
            int food,
            int wood,
            int gold,
            int populationUsed,
            int populationCap,
            bool hasCapitalBeenPlaced,
            bool isCapitalAlive,
            bool capitalBonusActive,
            IReadOnlyList<TechId> completedTechs,
            IReadOnlyList<ResearchSnapshot> researchQueue,
            IReadOnlyList<ModifierSnapshot> modifiers)
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
                completedTechs,
                researchQueue,
                modifiers)
        {
        }

        public LocalPlayerSnapshot(
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
            IReadOnlyList<TechId> completedTechs,
            IReadOnlyList<ResearchSnapshot> researchQueue,
            IReadOnlyList<ModifierSnapshot> modifiers)
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
            CompletedTechs = completedTechs;
            ResearchQueue = researchQueue;
            Modifiers = modifiers;
        }
    }

    public readonly struct ResearchSnapshot
    {
        public TechId TechId { get; }
        public int ProgressTicks { get; }
        public int RequiredTicks { get; }

        public ResearchSnapshot(TechId techId, int progressTicks, int requiredTicks)
        {
            TechId = techId;
            ProgressTicks = progressTicks;
            RequiredTicks = requiredTicks;
        }
    }

    public readonly struct ModifierSnapshot
    {
        public ModifierId ModifierId { get; }
        public int Value { get; }

        public ModifierSnapshot(ModifierId modifierId, int value)
        {
            ModifierId = modifierId;
            Value = value;
        }
    }

    public readonly struct MatchSnapshot
    {
        public bool IsFinished { get; }
        public int WinnerPlayerIndex { get; }
        public int FinishedTick { get; }

        public MatchSnapshot(bool isFinished, int winnerPlayerIndex, int finishedTick)
        {
            IsFinished = isFinished;
            WinnerPlayerIndex = winnerPlayerIndex;
            FinishedTick = finishedTick;
        }
    }
}
