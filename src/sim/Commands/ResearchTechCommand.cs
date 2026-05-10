using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class ResearchTechCommand : ICommand
    {
        public int BuildingId { get; }
        public TechId TechId { get; }

        public ResearchTechCommand(int buildingId, TechId techId)
        {
            BuildingId = buildingId;
            TechId = techId;
        }

        public CommandType Type
        {
            get { return CommandType.ResearchTech; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteInt32(BuildingId);
            writer.WriteUInt16((ushort)TechId);
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return false;
            }

            if (!TryGetBuilding(state, BuildingId, out Building? building))
            {
                return false;
            }

            if (building.OwnerPlayerIndex != header.PlayerIndex || building.IsDead || building.IsUnderConstruction)
            {
                return false;
            }

            if (!GameData.CanResearch(building.BuildingTypeId, TechId) || GameData.GetResearchTicks(TechId) <= 0)
            {
                return false;
            }

            PlayerState player = state.PlayerStates.Players[header.PlayerIndex];
            if (TechRules.IsCompleted(player, TechId) || TechRules.IsQueued(player, TechId))
            {
                return false;
            }

            return player.Resources.CanPay(GameData.GetResearchCost(TechId));
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            PlayerState player = state.PlayerStates.Players[header.PlayerIndex];
            player.Resources.Subtract(GameData.GetResearchCost(TechId));
            player.TechState.ResearchQueue.Add(new ResearchQueueItem(TechId, GameData.GetResearchTicks(TechId)));
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
    }
}
