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
        public int LastMovedTick { get; set; }
        public int HitPoints { get; set; }
        public int CurrentBuildTargetId { get; set; }
        public int CurrentResourceNodeId { get; set; }
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
