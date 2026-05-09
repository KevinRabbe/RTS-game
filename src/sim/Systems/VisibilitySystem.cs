using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class VisibilitySystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            ClearVisibleTiles(state);
            RevealUnits(state);
            RevealBuildings(state);
        }

        private static void ClearVisibleTiles(GameState state)
        {
            for (int player = 0; player < state.VisibilityState.Players.Count; player++)
            {
                bool[] visible = state.VisibilityState.Players[player].VisibleTiles;
                for (int i = 0; i < visible.Length; i++)
                {
                    visible[i] = false;
                }
            }
        }

        private static void RevealUnits(GameState state)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead)
                {
                    continue;
                }

                RevealCircle(state, unit.OwnerPlayerIndex, unit.Position.X.FloorToInt(), unit.Position.Y.FloorToInt(), GameData.GetUnitSightRadius(unit.UnitTypeId));
            }
        }

        private static void RevealBuildings(GameState state)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || building.IsUnderConstruction)
                {
                    continue;
                }

                RevealCircle(state, building.OwnerPlayerIndex, building.Position.X.FloorToInt(), building.Position.Y.FloorToInt(), GameData.GetBuildingSightRadius(building.BuildingTypeId));
            }
        }

        private static void RevealCircle(GameState state, int playerIndex, int centerX, int centerY, int radius)
        {
            if (radius <= 0 || playerIndex < 0 || playerIndex >= state.VisibilityState.Players.Count)
            {
                return;
            }

            int minX = Clamp(centerX - radius, 0, state.VisibilityState.WidthTiles - 1);
            int maxX = Clamp(centerX + radius, 0, state.VisibilityState.WidthTiles - 1);
            int minY = Clamp(centerY - radius, 0, state.VisibilityState.HeightTiles - 1);
            int maxY = Clamp(centerY + radius, 0, state.VisibilityState.HeightTiles - 1);
            int radiusSquared = radius * radius;
            PlayerVisibility visibility = state.VisibilityState.Players[playerIndex];

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    if (dx * dx + dy * dy > radiusSquared)
                    {
                        continue;
                    }

                    int index = state.VisibilityState.GetIndex(x, y);
                    visibility.VisibleTiles[index] = true;
                    visibility.ExploredTiles[index] = true;
                }
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
