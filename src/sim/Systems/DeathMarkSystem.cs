using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class DeathMarkSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.HitPoints <= 0 && !unit.IsDead)
                {
                    ReleaseUnitPopulation(state, unit);
                    unit.IsDead = true;
                    unit.HasMoveTarget = false;
                    unit.CurrentBuildTargetId = 0;
                    unit.CurrentResourceAreaId = 0;
                    unit.CurrentResourceNodeId = 0;
                    SpatialRules.ClearInteractionReservation(unit);
                    unit.TaskPhase = WorkerTaskPhase.Idle;
                    unit.AttackTargetId = 0;
                }
            }

            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.HitPoints <= 0)
                {
                    ReleaseQueuedPopulation(state, building);
                    building.IsDead = true;
                    building.AssignedBuilderIds.Clear();
                    building.TrainingQueue.Clear();
                }
            }
        }

        private static void ReleaseUnitPopulation(GameState state, Unit unit)
        {
            if (unit.OwnerPlayerIndex < 0 || unit.OwnerPlayerIndex >= state.PlayerStates.Players.Count)
            {
                return;
            }

            PlayerState player = state.PlayerStates.Players[unit.OwnerPlayerIndex];
            player.PopulationUsed -= GameData.GetUnitPopulation(unit.UnitTypeId);
            if (player.PopulationUsed < 0)
            {
                player.PopulationUsed = 0;
            }
        }

        private static void ReleaseQueuedPopulation(GameState state, Building building)
        {
            if (building.IsDead || building.OwnerPlayerIndex < 0 || building.OwnerPlayerIndex >= state.PlayerStates.Players.Count)
            {
                return;
            }

            int reservedPopulation = 0;
            for (int i = 0; i < building.TrainingQueue.Count; i++)
            {
                reservedPopulation += GameData.GetUnitPopulation(building.TrainingQueue[i].UnitTypeId);
            }

            PlayerState player = state.PlayerStates.Players[building.OwnerPlayerIndex];
            player.PopulationUsed -= reservedPopulation;
            if (player.PopulationUsed < 0)
            {
                player.PopulationUsed = 0;
            }
        }
    }
}
