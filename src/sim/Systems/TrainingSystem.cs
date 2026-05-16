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

                if (!TryResolveSpawnPosition(state, building, out FixedVector2 spawnPosition))
                {
                    item.ProgressTicks = item.RequiredTicks;
                    continue;
                }

                EntityFactory.CreateUnit(state, building.OwnerPlayerIndex, item.UnitTypeId, spawnPosition, false);
                building.TrainingQueue.RemoveAt(0);
            }
        }

        private static bool TryResolveSpawnPosition(GameState state, Building building, out FixedVector2 spawnPosition)
        {
            var interactionTiles = SpatialRules.EnumerateBuildInteractionTiles(state, building);
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                SpatialRules.TileCoord tile = interactionTiles[i];
                if (SpatialRules.IsTileAvailableForUnitSpawn(state, tile.X, tile.Y))
                {
                    spawnPosition = FixedVector2.FromInts(tile.X, tile.Y);
                    return true;
                }
            }

            spawnPosition = default;
            return false;
        }
    }
}
