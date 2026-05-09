using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class TrainingSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || building.IsUnderConstruction || building.TrainingQueue.Count == 0)
                {
                    continue;
                }

                TrainingQueueItem item = building.TrainingQueue[0];
                item.ProgressTicks++;
                if (item.ProgressTicks < item.RequiredTicks)
                {
                    continue;
                }

                EntityFactory.CreateUnit(state, building.OwnerPlayerIndex, item.UnitTypeId, building.Position, false);
                building.TrainingQueue.RemoveAt(0);
            }
        }
    }
}
