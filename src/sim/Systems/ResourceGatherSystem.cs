using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class ResourceGatherSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.UnitTypeId != UnitTypeId.Villager || unit.CurrentResourceNodeId == 0)
                {
                    continue;
                }

                if (unit.CarriedAmount >= GameData.VillagerCarryCapacity)
                {
                    continue;
                }

                ResourceNode? node = FindNode(state, unit.CurrentResourceNodeId);
                if (node == null || node.IsDepleted)
                {
                    unit.CurrentResourceNodeId = 0;
                    continue;
                }

                int carryRoom = GameData.VillagerCarryCapacity - unit.CarriedAmount;
                int gathered = Min(GameData.VillagerGatherPerTick, carryRoom, node.RemainingAmount);
                if (gathered <= 0)
                {
                    continue;
                }

                if (unit.CarriedResourceType == ResourceType.None)
                {
                    unit.CarriedResourceType = node.ResourceType;
                }

                if (unit.CarriedResourceType != node.ResourceType)
                {
                    continue;
                }

                unit.CarriedAmount += gathered;
                node.RemainingAmount -= gathered;
                if (node.IsDepleted)
                {
                    unit.CurrentResourceNodeId = 0;
                }
            }
        }

        private static ResourceNode? FindNode(GameState state, int nodeId)
        {
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                if (state.EconomyState.ResourceNodes[i].Id == nodeId)
                {
                    return state.EconomyState.ResourceNodes[i];
                }
            }

            return null;
        }

        private static int Min(int a, int b, int c)
        {
            int result = a < b ? a : b;
            return result < c ? result : c;
        }
    }
}
