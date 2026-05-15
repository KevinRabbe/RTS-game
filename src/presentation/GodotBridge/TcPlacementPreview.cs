namespace RtsGame.Presentation.GodotBridge
{
    /// <summary>
    /// Result of a read-only Town Center placement preview check.
    /// NOTE: This is client-side feedback only. The final placement authority
    /// is PlaceTownCenterCommand.IsValid() / PlacementRules.CanPlaceBuilding().
    /// </summary>
    public enum TcPlacementPreviewResult
    {
        Valid = 0,
        OutsideMap = 1,
        OverlapsBuilding = 2,
        OverlapsResource = 3,
        Unknown = 4,
        MissingResources = 5,
        PlayerStateBlocked = 6
    }

    /// <summary>
    /// Stateless read-only preview validator for Town Center placement.
    /// Operates entirely on GodotFrameDto — never touches GameState.
    /// Mirrors PlacementRules.CanPlaceBuilding for client feedback purposes.
    /// </summary>
    public static class TcPlacementPreview
    {
        // Fixed-point scale: 1 tile = 65536 raw units (Fixed.OneRaw).
        private const long FixedOneRaw = 1L << 16;

        // Mirror GameData constants - copied to avoid a cross-layer reference.
        // If GameData changes these, update here too.
        private const int TcRadiusTiles = 2;           // GameData.TownCenterPlacementRadiusTiles
        private const int ResourceRadiusTiles = 1;     // GameData.ResourcePlacementRadiusTiles
        private const int MapWidthTiles = 128;         // GameData.MapWidthTiles
        private const int MapHeightTiles = 96;         // GameData.MapHeightTiles
        private const int TownCenterWoodCost = 275;    // GameData.TownCenterWoodCost

        /// <summary>
        /// Evaluates whether placing a Town Center at the given tile is likely valid.
        /// Pure read — does not mutate any state.
        /// </summary>
        public static TcPlacementPreviewResult Evaluate(GodotFrameDto frame, int tileX, int tileY)
        {
            long posXRaw = (long)tileX * FixedOneRaw;
            long posYRaw = (long)tileY * FixedOneRaw;

            if (!IsInsideMap(tileX, tileY))
            {
                return TcPlacementPreviewResult.OutsideMap;
            }

            if (!frame.LocalPlayer.IsConnected || frame.LocalPlayer.IsDefeated || frame.LocalPlayer.IsResigned)
            {
                return TcPlacementPreviewResult.PlayerStateBlocked;
            }

            bool isFirstTownCenter = !frame.LocalPlayer.HasCapitalBeenPlaced;
            if (!isFirstTownCenter && frame.LocalPlayer.Wood < TownCenterWoodCost)
            {
                return TcPlacementPreviewResult.MissingResources;
            }

            // Check against all existing buildings.
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                GodotPrimitiveDrawKind kind = GodotPrimitiveDrawKindResolver.Resolve(primitive);
                if (kind != GodotPrimitiveDrawKind.Building)
                {
                    continue;
                }

                // Mirror PlacementRules: combined radius = TC radius + other building radius.
                // We conservatively treat every existing building as having radius 2 (TC) or 1 (wall).
                // The exact building type id is not available in the DTO; use TypeId hint if needed.
                int otherRadiusTiles = ResolveOtherBuildingRadius(primitive);
                if (IsWithinCombinedRadius(posXRaw, posYRaw, TcRadiusTiles, primitive.XRaw, primitive.YRaw, otherRadiusTiles))
                {
                    return TcPlacementPreviewResult.OverlapsBuilding;
                }
            }

            // Check against all resource nodes.
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                GodotPrimitiveDrawKind kind = GodotPrimitiveDrawKindResolver.Resolve(primitive);
                if (kind != GodotPrimitiveDrawKind.Resource)
                {
                    continue;
                }

                if (IsWithinCombinedRadius(posXRaw, posYRaw, TcRadiusTiles, primitive.XRaw, primitive.YRaw, ResourceRadiusTiles))
                {
                    return TcPlacementPreviewResult.OverlapsResource;
                }
            }

            return TcPlacementPreviewResult.Valid;
        }

        private static bool IsInsideMap(int tileX, int tileY)
        {
            return tileX >= 0 && tileY >= 0 && tileX < MapWidthTiles && tileY < MapHeightTiles;
        }

        private static bool IsWithinCombinedRadius(
            long posXRaw, long posYRaw, int radiusTiles,
            long otherXRaw, long otherYRaw, int otherRadiusTiles)
        {
            long combinedRaw = (long)(radiusTiles + otherRadiusTiles) * FixedOneRaw;
            long combinedSquaredRaw = checked(combinedRaw * combinedRaw);
            long dx = posXRaw - otherXRaw;
            long dy = posYRaw - otherYRaw;
            long distSquaredRaw = checked(dx * dx + dy * dy);
            return distSquaredRaw < combinedSquaredRaw;
        }

        private static int ResolveOtherBuildingRadius(GodotPrimitiveDto primitive)
        {
            // TypeId corresponds to BuildingTypeId enum values from the sim.
            // TownCenter = 1 (radius 2), Wall = 2 (radius 1), TradePost = 3 (radius 2).
            // Default conservatively to 1 if unknown.
            switch (primitive.TypeId)
            {
                case 1: return 2; // TownCenter
                case 2: return 1; // Wall
                case 3: return 2; // TradePost
                default: return 1;
            }
        }
    }
}
