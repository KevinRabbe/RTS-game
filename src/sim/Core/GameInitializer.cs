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
            CreateResourceAreaWithNodes(state, ResourceAreaType.BerryPatch, GatherProfileId.BerryBush, DryArabiaTest01MapDefinition.GetNearbyResourcePositions(playerIndex, ResourceType.Food), GameData.StartingFoodAmount);
            CreateResourceAreaWithNodes(state, ResourceAreaType.Forest, GatherProfileId.Tree, DryArabiaTest01MapDefinition.GetNearbyResourcePositions(playerIndex, ResourceType.Wood), GameData.StartingWoodAmount);
            CreateResourceAreaWithNodes(state, ResourceAreaType.GoldDeposit, GatherProfileId.GoldVeinSmall, DryArabiaTest01MapDefinition.GetNearbyResourcePositions(playerIndex, ResourceType.Gold), GameData.StartingGoldAmount);
        }

        private static void CreateDryArabiaCenterResources(GameState state)
        {
            CreateResourceAreaWithNodes(state, ResourceAreaType.GoldDeposit, GatherProfileId.GoldVeinLarge, new[] { FixedVector2.FromInts(64, 48), FixedVector2.FromInts(68, 50) }, GameData.CenterGoldAmount);
            CreateResourceAreaWithNodes(state, ResourceAreaType.BerryPatch, GatherProfileId.BerryBush, new[] { FixedVector2.FromInts(60, 44) }, GameData.CenterFoodAmount);
            CreateResourceAreaWithNodes(state, ResourceAreaType.Forest, GatherProfileId.Tree, new[] { FixedVector2.FromInts(60, 54) }, GameData.CenterWoodAmount);
        }

        private static void CreateStartingResources(GameState state, FixedVector2 spawn)
        {
            CreateResourceAreaWithNodes(state, ResourceAreaType.BerryPatch, GatherProfileId.BerryBush, new[] { new FixedVector2(spawn.X + Fixed.FromInt(6), spawn.Y) }, GameData.StartingFoodAmount);
            CreateResourceAreaWithNodes(state, ResourceAreaType.Forest, GatherProfileId.Tree, new[] { new FixedVector2(spawn.X, spawn.Y + Fixed.FromInt(6)) }, GameData.StartingWoodAmount);
            CreateResourceAreaWithNodes(state, ResourceAreaType.GoldDeposit, GatherProfileId.GoldVeinSmall, new[] { new FixedVector2(spawn.X + Fixed.FromInt(6), spawn.Y + Fixed.FromInt(6)) }, GameData.StartingGoldAmount);
        }

        private static void CreateCenterResources(GameState state)
        {
            Fixed centerX = Fixed.FromInt(GameData.MapWidthTiles / 2);
            Fixed centerY = Fixed.FromInt(GameData.MapHeightTiles / 2);
            CreateResourceAreaWithNodes(state, ResourceAreaType.GoldDeposit, GatherProfileId.GoldVeinLarge, new[] { new FixedVector2(centerX, centerY) }, GameData.CenterGoldAmount);
            CreateResourceAreaWithNodes(state, ResourceAreaType.BerryPatch, GatherProfileId.BerryBush, new[] { new FixedVector2(centerX + Fixed.FromInt(4), centerY) }, GameData.CenterFoodAmount);
            CreateResourceAreaWithNodes(state, ResourceAreaType.Forest, GatherProfileId.Tree, new[] { new FixedVector2(centerX, centerY + Fixed.FromInt(4)) }, GameData.CenterWoodAmount);
        }

        private static void CreateResourceAreaWithNodes(GameState state, ResourceAreaType areaType, GatherProfileId profileId, FixedVector2[] positions, int amount)
        {
            if (positions.Length == 0)
            {
                return;
            }

            GatherProfile profile = GameData.GetGatherProfile(profileId);
            int areaId = state.EconomyState.NextResourceAreaId++;
            state.EconomyState.ResourceAreas.Add(new ResourceArea
            {
                Id = areaId,
                AreaType = areaType,
                ResourceType = profile.ResourceType,
                GatherProfileId = profileId,
                Position = GetAreaCenter(positions)
            });

            for (int i = 0; i < positions.Length; i++)
            {
                CreateResourceNode(state, areaId, profile, positions[i], amount);
            }
        }

        private static void CreateResourceNode(GameState state, int areaId, GatherProfile profile, FixedVector2 position, int amount)
        {
            state.EconomyState.ResourceNodes.Add(new ResourceNode
            {
                Id = state.EconomyState.NextResourceNodeId++,
                ResourceAreaId = areaId,
                ResourceType = profile.ResourceType,
                NodeType = profile.NodeType,
                GatherProfileId = profile.Id,
                Position = position,
                RemainingAmount = amount
            });
        }

        private static FixedVector2 GetAreaCenter(FixedVector2[] positions)
        {
            long sumXRaw = 0;
            long sumYRaw = 0;
            for (int i = 0; i < positions.Length; i++)
            {
                sumXRaw += positions[i].X.Raw;
                sumYRaw += positions[i].Y.Raw;
            }

            return new FixedVector2(new Fixed(sumXRaw / positions.Length), new Fixed(sumYRaw / positions.Length));
        }

        private static FixedVector2 GetSpawnPosition(int playerIndex)
        {
            int x = (playerIndex % 3) * 40;
            int y = (playerIndex / 3) * 40;
            return FixedVector2.FromInts(x, y);
        }
    }
}
