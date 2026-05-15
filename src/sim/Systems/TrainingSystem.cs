using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

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

                FixedVector2 spawnPosition = ResolveSpawnPosition(state, building);
                EntityFactory.CreateUnit(state, building.OwnerPlayerIndex, item.UnitTypeId, spawnPosition, false);
                building.TrainingQueue.RemoveAt(0);
            }
        }

        private static FixedVector2 ResolveSpawnPosition(GameState state, Building building)
        {
            var interactionTiles = SpatialRules.EnumerateBuildInteractionTiles(state, building);
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                SpatialRules.TileCoord tile = interactionTiles[i];
                if (!SpatialRules.IsTileOccupiedByLiveUnit(state, tile.X, tile.Y))
                {
                    return FixedVector2.FromInts(tile.X, tile.Y);
                }
            }

            return building.Position;
        }
    }
}
