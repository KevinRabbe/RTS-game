using System.Collections.Generic;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Data
{
    public sealed class EntityState
    {
        public int NextEntityId { get; set; } = 1;
        public List<Unit> Units { get; } = new List<Unit>();
        public List<Building> Buildings { get; } = new List<Building>();
        public Dictionary<int, EntityRef> EntityLookup { get; } = new Dictionary<int, EntityRef>();
    }

    public sealed class Unit
    {
        public int Id { get; set; }
        public int OwnerPlayerIndex { get; set; }
        public UnitTypeId UnitTypeId { get; set; }
        public FixedVector2 Position { get; set; }
        public bool HasMoveTarget { get; set; }
        public FixedVector2 MoveTarget { get; set; }
        public FixedVector2 Velocity { get; set; }
        public int LastSteeringDecisionTick { get; set; }
        public int CorridorVersion { get; set; }
        public int CorridorStepIndex { get; set; }
        public int RetargetCooldownUntilTick { get; set; }
        public MovementBlockReason MovementBlockedReason { get; set; }
        public int BlockedSinceTick { get; set; }
        public int LastMeaningfulProgressTick { get; set; }
        public int LastMovedTick { get; set; }
        public int LastNoProgressTicksWindow { get; set; }
        public int HitPoints { get; set; }
        public int CurrentBuildTargetId { get; set; }
        public int CurrentResourceAreaId { get; set; }
        public int CurrentResourceNodeId { get; set; }
        public int AssignedResourceNodeId { get; set; }
        public WorkerTaskPhase TaskPhase { get; set; }
        public InteractionReservationKind ReservedInteractionKind { get; set; }
        public int ReservedInteractionTargetId { get; set; }
        public int ReservedInteractionTileX { get; set; }
        public int ReservedInteractionTileY { get; set; }
        public int LastReservationRetargetTick { get; set; }
        public int ReservationChurnCountWindow { get; set; }
        public int ReservationChurnWindowStartTick { get; set; }
        public ReservationAttemptFailureReason LastReservationFailureReason { get; set; }
        public int LastReservationFailureTick { get; set; }
        public GatherFallbackReason LastGatherFallbackReason { get; set; }
        public MovementRetargetReason LastMovementRetargetReason { get; set; }
        public ResourceType CarriedResourceType { get; set; }
        public int CarriedAmount { get; set; }
        public int AttackTargetId { get; set; }
        public int AttackCooldownTicksRemaining { get; set; }
        public bool IsSiegeDeployed { get; set; }
        public int SiegeSetupTicksRemaining { get; set; }
        public int SiegeReloadTicksRemaining { get; set; }
        public int TradeRouteAId { get; set; }
        public int TradeRouteBId { get; set; }
        public int TradeDestinationId { get; set; }
        public int TradeIncomePerTrip { get; set; }
        public int DespawnTicksRemaining { get; set; }
        public bool IsDead { get; set; }
    }

    public enum InteractionReservationKind
    {
        None = 0,
        ResourceNode = 1,
        Dropoff = 2,
        BuildSite = 3,
        MoveDestination = 4,
        AttackSlot = 5
    }

    public enum WorkerTaskPhase
    {
        Idle = 0,
        MovingToResourceSlot = 1,
        Gathering = 2,
        MovingToDropoffSlot = 3,
        Depositing = 4,
        MovingToBuildSlot = 5,
        Building = 6,
        MovingToCommandMove = 7,
        BlockedWaiting = 8,
        MovingToAttackSlot = 9
    }

    public enum ReservationAttemptFailureReason
    {
        None = 0,
        NoCandidates = 1,
        SlotUnavailable = 2,
        NoReachablePath = 3
    }

    public enum GatherFallbackReason
    {
        None = 0,
        ExplicitRetarget = 1,
        NodeDepleted = 2,
        NodeInvalid = 3,
        StaleTimeout = 4,
        Unreachable = 5
    }

    public enum MovementRetargetReason
    {
        None = 0,
        NoProgressTimeout = 1,
        SharedDestinationConflict = 2,
        PathUnreachable = 3,
        StaticBlocked = 4
    }

    public enum MovementBlockReason
    {
        None = 0,
        OccupiedNextTile = 1,
        ReservedNextTile = 2,
        StaticBlocked = 3,
        NoPath = 4,
        SharedDestinationConflict = 5,
        SwapConflict = 6
    }

    public sealed class Building
    {
        public int Id { get; set; }
        public int OwnerPlayerIndex { get; set; }
        public BuildingTypeId BuildingTypeId { get; set; }
        public FixedVector2 Position { get; set; }
        public int HitPoints { get; set; }
        public bool IsUnderConstruction { get; set; }
        public int BuildProgressTicks { get; set; }
        public List<int> AssignedBuilderIds { get; } = new List<int>();
        public List<TrainingQueueItem> TrainingQueue { get; } = new List<TrainingQueueItem>();
        public bool IsCapital { get; set; }
        public int DespawnTicksRemaining { get; set; }
        public bool IsDead { get; set; }
    }

    public readonly struct EntityRef
    {
        public EntityKind Kind { get; }
        public int Index { get; }

        public EntityRef(EntityKind kind, int index)
        {
            Kind = kind;
            Index = index;
        }
    }

    public enum EntityKind
    {
        Unit = 1,
        Building = 2
    }
}
