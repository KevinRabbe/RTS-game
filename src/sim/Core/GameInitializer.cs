using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Core
{
    public static class GameInitializer
    {
        public static GameState CreateNomadStart(ulong matchSeed, int playerCount)
        {
            var state = new GameState(matchSeed, playerCount);

            for (int player = 0; player < playerCount; player++)
            {
                FixedVector2 spawn = GetSpawnPosition(player);
                for (int i = 0; i < 4; i++)
                {
                    EntityFactory.CreateUnit(state, player, UnitTypeId.Villager, new FixedVector2(spawn.X + Fixed.FromInt(i), spawn.Y));
                }

                EntityFactory.CreateUnit(state, player, UnitTypeId.Scout, new FixedVector2(spawn.X, spawn.Y + Fixed.FromInt(2)));
                CreateStartingResources(state, spawn);
            }

            CreateCenterResources(state);
            return state;
        }

        private static void CreateStartingResources(GameState state, FixedVector2 spawn)
        {
            CreateResourceNode(state, ResourceType.Food, new FixedVector2(spawn.X + Fixed.FromInt(6), spawn.Y), GameData.StartingFoodAmount);
            CreateResourceNode(state, ResourceType.Wood, new FixedVector2(spawn.X, spawn.Y + Fixed.FromInt(6)), GameData.StartingWoodAmount);
            CreateResourceNode(state, ResourceType.Gold, new FixedVector2(spawn.X + Fixed.FromInt(6), spawn.Y + Fixed.FromInt(6)), GameData.StartingGoldAmount);
        }

        private static void CreateCenterResources(GameState state)
        {
            Fixed centerX = Fixed.FromInt(GameData.MapWidthTiles / 2);
            Fixed centerY = Fixed.FromInt(GameData.MapHeightTiles / 2);
            CreateResourceNode(state, ResourceType.Gold, new FixedVector2(centerX, centerY), GameData.CenterGoldAmount);
            CreateResourceNode(state, ResourceType.Food, new FixedVector2(centerX + Fixed.FromInt(4), centerY), GameData.CenterFoodAmount);
            CreateResourceNode(state, ResourceType.Wood, new FixedVector2(centerX, centerY + Fixed.FromInt(4)), GameData.CenterWoodAmount);
        }

        private static void CreateResourceNode(GameState state, ResourceType resourceType, FixedVector2 position, int amount)
        {
            state.EconomyState.ResourceNodes.Add(new ResourceNode
            {
                Id = state.EconomyState.NextResourceNodeId++,
                ResourceType = resourceType,
                Position = position,
                RemainingAmount = amount
            });
        }

        private static FixedVector2 GetSpawnPosition(int playerIndex)
        {
            int x = (playerIndex % 3) * 40;
            int y = (playerIndex / 3) * 40;
            return FixedVector2.FromInts(x, y);
        }
    }
}
