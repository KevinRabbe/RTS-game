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

        public static GameState CreateDryArabiaTest01(ulong matchSeed)
        {
            var state = new GameState(matchSeed, 2);

            for (int player = 0; player < 2; player++)
            {
                FixedVector2 spawn = DryArabiaTest01MapDefinition.GetSpawnPosition(player);
                for (int i = 0; i < 4; i++)
                {
                    EntityFactory.CreateUnit(state, player, UnitTypeId.Villager, new FixedVector2(spawn.X + Fixed.FromInt(i - 1), spawn.Y));
                }

                EntityFactory.CreateUnit(state, player, UnitTypeId.Scout, new FixedVector2(spawn.X, spawn.Y + Fixed.FromInt(2)));
                CreateDryArabiaStartingResources(state, player);
            }

            CreateDryArabiaCenterResources(state);
            EntityFactory.CreateTradePost(state, GameData.NeutralOwnerPlayerIndex, FixedVector2.FromInts(64, 32));
            EntityFactory.CreateTradePost(state, GameData.NeutralOwnerPlayerIndex, FixedVector2.FromInts(64, 64));
            return state;
        }

        private static void CreateDryArabiaStartingResources(GameState state, int playerIndex)
        {
            CreateResourceNodes(state, ResourceType.Food, DryArabiaTest01MapDefinition.GetNearbyResourcePositions(playerIndex, ResourceType.Food), GameData.StartingFoodAmount);
            CreateResourceNodes(state, ResourceType.Wood, DryArabiaTest01MapDefinition.GetNearbyResourcePositions(playerIndex, ResourceType.Wood), GameData.StartingWoodAmount);
            CreateResourceNodes(state, ResourceType.Gold, DryArabiaTest01MapDefinition.GetNearbyResourcePositions(playerIndex, ResourceType.Gold), GameData.StartingGoldAmount);
        }

        private static void CreateDryArabiaCenterResources(GameState state)
        {
            CreateResourceNode(state, ResourceType.Gold, FixedVector2.FromInts(64, 48), GameData.CenterGoldAmount);
            CreateResourceNode(state, ResourceType.Gold, FixedVector2.FromInts(68, 50), GameData.CenterGoldAmount);
            CreateResourceNode(state, ResourceType.Food, FixedVector2.FromInts(60, 44), GameData.CenterFoodAmount);
            CreateResourceNode(state, ResourceType.Wood, FixedVector2.FromInts(60, 54), GameData.CenterWoodAmount);
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

        private static void CreateResourceNodes(GameState state, ResourceType resourceType, FixedVector2[] positions, int amount)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                CreateResourceNode(state, resourceType, positions[i], amount);
            }
        }

        private static FixedVector2 GetSpawnPosition(int playerIndex)
        {
            int x = (playerIndex % 3) * 40;
            int y = (playerIndex / 3) * 40;
            return FixedVector2.FromInts(x, y);
        }
    }
}
