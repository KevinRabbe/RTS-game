using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class ResignationSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int playerIndex = 0; playerIndex < state.PlayerStates.Players.Count; playerIndex++)
            {
                PlayerState player = state.PlayerStates.Players[playerIndex];
                if (!player.IsResigned || player.IsDefeated)
                {
                    continue;
                }

                NeutralizeUnits(state, playerIndex);
                NeutralizeBuildings(state, playerIndex);
                player.IsDefeated = true;
                player.PopulationUsed = 0;
                player.PopulationCap = 0;
                player.CapitalStatus.IsCapitalAlive = false;
                player.CapitalStatus.CapitalBonusActive = false;
            }
        }

        private static void NeutralizeUnits(GameState state, int playerIndex)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.OwnerPlayerIndex != playerIndex || unit.IsDead)
                {
                    continue;
                }

                unit.OwnerPlayerIndex = GameData.NeutralOwnerPlayerIndex;
                unit.HasMoveTarget = false;
                unit.CurrentBuildTargetId = 0;
                unit.CurrentResourceNodeId = 0;
                SpatialRules.ClearInteractionReservation(unit);
                unit.TaskPhase = WorkerTaskPhase.Idle;
                unit.CarriedResourceType = ResourceType.None;
                unit.CarriedAmount = 0;
                unit.AttackTargetId = 0;
                unit.AttackCooldownTicksRemaining = 0;
                unit.IsSiegeDeployed = false;
                unit.SiegeSetupTicksRemaining = 0;
                unit.SiegeReloadTicksRemaining = 0;
                unit.DespawnTicksRemaining = GameData.ResignedAssetDespawnTicks;
            }
        }

        private static void NeutralizeBuildings(GameState state, int playerIndex)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.OwnerPlayerIndex != playerIndex || building.IsDead)
                {
                    continue;
                }

                building.OwnerPlayerIndex = GameData.NeutralOwnerPlayerIndex;
                building.AssignedBuilderIds.Clear();
                building.TrainingQueue.Clear();
                building.DespawnTicksRemaining = GameData.ResignedAssetDespawnTicks;
            }
        }
    }
}
