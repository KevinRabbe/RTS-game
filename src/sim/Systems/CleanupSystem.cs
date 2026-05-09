using RtsGame.Sim.Core;

namespace RtsGame.Sim.Systems
{
    public sealed class CleanupSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            CleanupUnits(state);
            CleanupBuildings(state);
        }

        private static void CleanupUnits(GameState state)
        {
            for (int i = state.EntityState.Units.Count - 1; i >= 0; i--)
            {
                if (!state.EntityState.Units[i].IsDead)
                {
                    continue;
                }

                int removedId = state.EntityState.Units[i].Id;
                int lastIndex = state.EntityState.Units.Count - 1;
                state.EntityState.Units[i] = state.EntityState.Units[lastIndex];
                state.EntityState.Units.RemoveAt(lastIndex);
                state.EntityState.EntityLookup.Remove(removedId);

                if (i < state.EntityState.Units.Count)
                {
                    int movedId = state.EntityState.Units[i].Id;
                    state.EntityState.EntityLookup[movedId] = new RtsGame.Sim.Data.EntityRef(RtsGame.Sim.Data.EntityKind.Unit, i);
                }
            }
        }

        private static void CleanupBuildings(GameState state)
        {
            for (int i = state.EntityState.Buildings.Count - 1; i >= 0; i--)
            {
                if (!state.EntityState.Buildings[i].IsDead)
                {
                    continue;
                }

                int removedId = state.EntityState.Buildings[i].Id;
                int lastIndex = state.EntityState.Buildings.Count - 1;
                state.EntityState.Buildings[i] = state.EntityState.Buildings[lastIndex];
                state.EntityState.Buildings.RemoveAt(lastIndex);
                state.EntityState.EntityLookup.Remove(removedId);

                if (i < state.EntityState.Buildings.Count)
                {
                    int movedId = state.EntityState.Buildings[i].Id;
                    state.EntityState.EntityLookup[movedId] = new RtsGame.Sim.Data.EntityRef(RtsGame.Sim.Data.EntityKind.Building, i);
                }
            }
        }
    }
}
