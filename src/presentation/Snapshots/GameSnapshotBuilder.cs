using System;
using System.Collections.Generic;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.Snapshots
{
    public static class GameSnapshotBuilder
    {
        public static GameSnapshot Build(GameState state, int localPlayerIndex)
        {
            if (localPlayerIndex < 0 || localPlayerIndex >= state.PlayerStates.Players.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(localPlayerIndex), "Local player index must exist in GameState.");
            }

            var units = new List<UnitSnapshot>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || !IsVisibleToLocalPlayer(state, localPlayerIndex, unit.Position))
                {
                    continue;
                }

                units.Add(new UnitSnapshot(unit.Id, unit.OwnerPlayerIndex, unit.UnitTypeId, unit.Position, unit.HitPoints, unit.TradeRouteAId, unit.TradeRouteBId));
            }

            var buildings = new List<BuildingSnapshot>();
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || !IsVisibleToLocalPlayer(state, localPlayerIndex, building.Position))
                {
                    continue;
                }

                buildings.Add(new BuildingSnapshot(
                    building.Id,
                    building.OwnerPlayerIndex,
                    building.BuildingTypeId,
                    building.Position,
                    building.HitPoints,
                    building.IsUnderConstruction,
                    building.IsCapital));
            }

            PlayerState player = state.PlayerStates.Players[localPlayerIndex];
            var localPlayer = new LocalPlayerSnapshot(
                player.Resources.Food,
                player.Resources.Wood,
                player.Resources.Gold,
                player.PopulationUsed,
                player.PopulationCap,
                player.CapitalStatus.HasCapitalBeenPlaced,
                player.CapitalStatus.IsCapitalAlive,
                player.CapitalStatus.CapitalBonusActive);

            var match = new MatchSnapshot(
                state.MatchResultState.IsFinished,
                state.MatchResultState.WinnerPlayerIndex,
                state.MatchResultState.FinishedTick);

            return new GameSnapshot(state.Tick, localPlayerIndex, units, buildings, localPlayer, match);
        }

        private static bool IsVisibleToLocalPlayer(GameState state, int localPlayerIndex, FixedVector2 position)
        {
            PlayerVisibility visibility = state.VisibilityState.Players[localPlayerIndex];
            int tileX = SpatialRules.GetTileX(position);
            int tileY = SpatialRules.GetTileY(position);
            if (tileX < 0 || tileY < 0 || tileX >= state.VisibilityState.WidthTiles || tileY >= state.VisibilityState.HeightTiles)
            {
                return false;
            }

            return visibility.VisibleTiles[state.VisibilityState.GetIndex(tileX, tileY)];
        }
    }
}
