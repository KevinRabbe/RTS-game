using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class TrainUnitCommand : ICommand
    {
        public int BuildingId { get; }
        public UnitTypeId UnitTypeId { get; }

        public TrainUnitCommand(int buildingId, UnitTypeId unitTypeId)
        {
            BuildingId = buildingId;
            UnitTypeId = unitTypeId;
        }

        public CommandType Type
        {
            get { return CommandType.TrainUnit; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteInt32(BuildingId);
            writer.WriteUInt16((ushort)UnitTypeId);
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            return CommandValidationInspector.IsAccepted(GetValidationReason(state, rules, header));
        }

        public CommandValidationReason GetValidationReason(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return CommandValidationReason.InvalidHeader;
            }

            if (!TryGetBuilding(state, BuildingId, out Building? building))
            {
                return CommandValidationReason.TargetMissing;
            }

            if (building.OwnerPlayerIndex != header.PlayerIndex)
            {
                return CommandValidationReason.WrongOwner;
            }

            if (building.IsDead)
            {
                return CommandValidationReason.TargetMissing;
            }

            if (building.IsUnderConstruction)
            {
                return CommandValidationReason.TargetComplete;
            }

            if (!GameData.CanTrain(building.BuildingTypeId, UnitTypeId))
            {
                return CommandValidationReason.InvalidTargetType;
            }

            PlayerState player = state.PlayerStates.Players[header.PlayerIndex];
            int unitPopulation = GameData.GetUnitPopulation(UnitTypeId);
            if (player.PopulationUsed + unitPopulation > player.PopulationCap)
            {
                return CommandValidationReason.PopulationBlocked;
            }

            ResourceStockpile cost = GameData.GetUnitCost(UnitTypeId);
            return player.Resources.CanPay(cost) ? CommandValidationReason.Accepted : CommandValidationReason.MissingResources;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            Building building = GetBuilding(state, BuildingId);
            PlayerState player = state.PlayerStates.Players[header.PlayerIndex];
            ResourceStockpile cost = GameData.GetUnitCost(UnitTypeId);
            player.Resources.Subtract(cost);
            player.PopulationUsed += GameData.GetUnitPopulation(UnitTypeId);
            building.TrainingQueue.Add(new TrainingQueueItem(UnitTypeId, GameData.GetUnitTrainTicks(UnitTypeId)));
        }

        private static bool TryGetBuilding(GameState state, int buildingId, [NotNullWhen(true)] out Building? building)
        {
            building = null;
            if (!state.EntityState.EntityLookup.TryGetValue(buildingId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Building)
            {
                return false;
            }

            if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Buildings.Count)
            {
                return false;
            }

            building = state.EntityState.Buildings[entityRef.Index];
            return building.Id == buildingId;
        }

        private static Building GetBuilding(GameState state, int buildingId)
        {
            TryGetBuilding(state, buildingId, out Building? building);
            return building!;
        }
    }
}
