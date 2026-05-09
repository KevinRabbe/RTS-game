using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class CapitalSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsCapital)
                {
                    continue;
                }

                if (building.OwnerPlayerIndex < 0 || building.OwnerPlayerIndex >= state.PlayerStates.Players.Count)
                {
                    continue;
                }

                PlayerState player = state.PlayerStates.Players[building.OwnerPlayerIndex];
                if (!building.IsUnderConstruction && !building.IsDead && !player.CapitalStatus.IsCapitalAlive)
                {
                    player.CapitalStatus.CapitalBuildingId = building.Id;
                    player.CapitalStatus.IsCapitalAlive = true;
                    player.CapitalStatus.CapitalBonusActive = true;
                    player.PopulationCap += GameData.CapitalPopulationBonus;
                }

                if (building.IsDead && player.CapitalStatus.IsCapitalAlive)
                {
                    player.CapitalStatus.IsCapitalAlive = false;
                    player.CapitalStatus.CapitalBonusActive = false;
                    player.PopulationCap -= GameData.CapitalPopulationBonus;
                }
            }
        }
    }
}
