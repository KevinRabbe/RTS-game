using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class DespawnSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.DespawnTicksRemaining <= 0)
                {
                    continue;
                }

                unit.DespawnTicksRemaining--;
                if (unit.DespawnTicksRemaining == 0)
                {
                    unit.IsDead = true;
                }
            }

            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.DespawnTicksRemaining <= 0)
                {
                    continue;
                }

                building.DespawnTicksRemaining--;
                if (building.DespawnTicksRemaining == 0)
                {
                    building.IsDead = true;
                }
            }
        }
    }
}
