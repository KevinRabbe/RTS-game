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
        public LocalPlayerSnapshot LocalPlayer { get; }
        public MatchSnapshot Match { get; }

        public GameSnapshot(
            int tick,
            int localPlayerIndex,
            IReadOnlyList<UnitSnapshot> units,
            IReadOnlyList<BuildingSnapshot> buildings,
            LocalPlayerSnapshot localPlayer,
            MatchSnapshot match)
        {
            Tick = tick;
            LocalPlayerIndex = localPlayerIndex;
            Units = units;
            Buildings = buildings;
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

        public UnitSnapshot(int id, int ownerPlayerIndex, UnitTypeId unitTypeId, FixedVector2 position, int hitPoints)
        {
            Id = id;
            OwnerPlayerIndex = ownerPlayerIndex;
            UnitTypeId = unitTypeId;
            Position = position;
            HitPoints = hitPoints;
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
        public bool IsCapital { get; }

        public BuildingSnapshot(int id, int ownerPlayerIndex, BuildingTypeId buildingTypeId, FixedVector2 position, int hitPoints, bool isUnderConstruction, bool isCapital)
        {
            Id = id;
            OwnerPlayerIndex = ownerPlayerIndex;
            BuildingTypeId = buildingTypeId;
            Position = position;
            HitPoints = hitPoints;
            IsUnderConstruction = isUnderConstruction;
            IsCapital = isCapital;
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

        public LocalPlayerSnapshot(int food, int wood, int gold, int populationUsed, int populationCap, bool hasCapitalBeenPlaced, bool isCapitalAlive, bool capitalBonusActive)
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
