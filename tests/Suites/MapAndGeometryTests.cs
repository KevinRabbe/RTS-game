using RtsGame.Net.Lockstep;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;
using RtsGame.Sim.Systems;
using RtsGame.Stress;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void NomadStartCreatesInitialUnits()
        {
            GameState state = GameInitializer.CreateNomadStart(5, 3);
            AssertEqual(15, state.EntityState.Units.Count, "nomad start should create five units per player");
            AssertEqual(0, state.EntityState.Buildings.Count, "nomad start should not create town centers");

            for (int player = 0; player < 3; player++)
            {
                AssertEqual(5, state.PlayerStates.Players[player].PopulationUsed, "each player should start with five population used");
                AssertEqual(0, state.PlayerStates.Players[player].PopulationCap, "capital bonus should not exist before TC placement");
                AssertFalse(state.PlayerStates.Players[player].CapitalStatus.HasCapitalBeenPlaced, "capital should not exist before TC placement");
            }
        }

        private static void NomadMapCreatesCenterResources()
        {
            GameState state = GameInitializer.CreateNomadStart(6, 3);
            AssertEqual(12, state.EconomyState.ResourceNodes.Count, "nomad map should create player resources plus center resources");

            ResourceNode centerGold = state.EconomyState.ResourceNodes[9];
            AssertEqual(ResourceType.Gold, centerGold.ResourceType, "first center resource should be gold");
            AssertEqual(FixedVector2.FromInts(GameData.MapWidthTiles / 2, GameData.MapHeightTiles / 2), centerGold.Position, "center gold should be placed at map center");
            AssertEqual(GameData.CenterGoldAmount, centerGold.RemainingAmount, "center gold should be high value");
        }

        private static void ResourceVisualOverhangDoesNotChangeSimFootprint()
        {
            GameState state = CreateOccupancyState(603, 1);
            ResourceNode tree = CreateTestResourceNode(state, GatherProfileId.Tree, FixedVector2.FromInts(10, 10), GameData.StartingWoodAmount);
            GatherProfile profile = GameData.GetGatherProfile(tree.GatherProfileId);

            AssertEqual(true, profile.VisualRadiusTiles >= profile.FootprintRadiusTiles, "tree profile visual radius should not undershoot sim footprint");
            AssertEqual(true, SpatialRules.IsTileInsideResourceFootprint(tree, 10, 10), "tree trunk tile should be the sim footprint");
            AssertEqual(false, SpatialRules.IsTileInsideResourceFootprint(tree, 12, 10), "visual overhang tile should not be inside sim footprint");
            AssertEqual(false, SpatialRules.IsTileBlockedForUnitMovement(state, 12, 10), "visual overhang tile should not block movement");
            AssertEqual(true, GodotPrimitiveHitTest.ContainsPointForInteraction(
                CreateGodotPrimitive(VisualPrimitiveKind.WoodResourceCircle, tree.Id, GameData.NeutralOwnerPlayerIndex, 10, 10),
                Fixed.FromInt(11).Raw,
                Fixed.FromInt(10).Raw),
                "presentation click bounds can be generous without changing sim footprint");
        }

        private static void ResourceInteractionRingUsesSimFootprint()
        {
            GameState state = CreateOccupancyState(604, 1);
            ResourceNode berries = CreateTestResourceNode(state, GatherProfileId.BerryBush, FixedVector2.FromInts(10, 10), GameData.StartingFoodAmount);

            List<SpatialRules.TileCoord> footprint = SpatialRules.EnumerateResourceFootprintTiles(state, berries);
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateResourceInteractionTiles(state, berries);

            AssertEqual(1, footprint.Count, "1x1 resource footprint should enumerate one sim tile");
            AssertEqual(8, ring.Count, "1x1 resource footprint should expose eight surrounding interaction slots");
            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 9, 9), "diagonal resource slot should be valid");
            AssertEqual(false, SpatialRules.ContainsInteractionTile(ring, 10, 10), "resource footprint tile should not be an interaction slot");
        }

        private static void LargeResourceInteractionRingUsesLargerSimFootprint()
        {
            GameState state = CreateOccupancyState(605, 1);
            ResourceNode largeGold = CreateTestResourceNode(state, GatherProfileId.GoldVeinLarge, FixedVector2.FromInts(20, 20), GameData.CenterGoldAmount);

            List<SpatialRules.TileCoord> footprint = SpatialRules.EnumerateResourceFootprintTiles(state, largeGold);
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateResourceInteractionTiles(state, largeGold);

            AssertEqual(true, footprint.Count > 1, "large gold should have a larger sim footprint than a 1x1 node");
            AssertEqual(true, ring.Count > 8, "larger resource footprint should expose a larger interaction ring");
            AssertEqual(true, SpatialRules.IsTileBlockedForUnitMovement(state, 19, 20), "large gold footprint should block pathing");
            AssertEqual(false, SpatialRules.ContainsInteractionTile(ring, 20, 20), "large gold center should not be an interaction slot");
        }

        private static void BuildingInteractionRingUsesSimFootprint()
        {
            GameState state = CreateOccupancyState(606, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];

            List<SpatialRules.TileCoord> footprint = SpatialRules.EnumerateBuildingFootprintTiles(state, tc);
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateBuildingInteractionTiles(state, tc);

            AssertEqual(true, footprint.Count > 1, "town center should have a multi-tile sim footprint");
            AssertEqual(true, ring.Count > 8, "town center footprint should expose a larger interaction ring");
            AssertEqual(true, SpatialRules.IsTileBlockedForUnitMovement(state, 20, 20), "town center footprint should block pathing");
            AssertEqual(false, SpatialRules.ContainsInteractionTile(ring, 20, 20), "town center footprint tile should not be an interaction slot");
        }

        private static void TownCenterRingAcceptsWorkersOnEverySide()
        {
            GameState state = CreateOccupancyState(607, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateBuildingInteractionTiles(state, tc);

            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 20, 17), "TC ring should include north side around full footprint");
            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 20, 22), "TC ring should include south side around full footprint");
            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 17, 20), "TC ring should include west side around full footprint");
            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 22, 20), "TC ring should include east side around full footprint");
            AssertEqual(true, SpatialRules.IsUnitInBuildInteractionRange(new Unit { Position = FixedVector2.FromInts(20, 17) }, tc), "north ring worker should be in build range");
            AssertEqual(true, SpatialRules.IsUnitInBuildingInteractionRange(new Unit { Position = FixedVector2.FromInts(20, 22) }, tc), "south ring carrier should be in dropoff range");
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(tc, 20, 17), "ring tile should not be inside the TC footprint");
        }

        private static void BerryVisualRadiusMatchesSimulationFootprintRadius()
        {
            GatherProfile profile = GameData.GetGatherProfile(GatherProfileId.BerryBush);
            AssertEqual(profile.FootprintRadiusTiles, profile.VisualRadiusTiles, "berry visual radius should match node simulation footprint radius");
        }

    }
}
