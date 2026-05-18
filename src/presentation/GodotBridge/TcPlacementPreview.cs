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

        // Mirror GameData footprint constants - copied to avoid a cross-layer reference.
        // If GameData changes these, update here too.
        private const int TcFootprintWidthTiles = 4;   // GameData.GetBuildingFootprintWidthTiles(TownCenter)
        private const int TcFootprintHeightTiles = 4;  // GameData.GetBuildingFootprintHeightTiles(TownCenter)
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

            GetFootprintBounds(posXRaw, posYRaw, TcFootprintWidthTiles, TcFootprintHeightTiles, out long tcMinXRaw, out long tcMinYRaw, out long tcMaxXRaw, out long tcMaxYRaw);

            // Check against all existing buildings.
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                GodotPrimitiveDrawKind kind = GodotPrimitiveDrawKindResolver.Resolve(primitive);
                if (kind != GodotPrimitiveDrawKind.Building)
                {
                    continue;
                }

                if (IsOverlappingFootprint(tcMinXRaw, tcMinYRaw, tcMaxXRaw, tcMaxYRaw, primitive))
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

                if (IsOverlappingFootprint(tcMinXRaw, tcMinYRaw, tcMaxXRaw, tcMaxYRaw, primitive))
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

        private static bool IsOverlappingFootprint(
            long tcMinXRaw,
            long tcMinYRaw,
            long tcMaxXRaw,
            long tcMaxYRaw,
            GodotPrimitiveDto primitive)
        {
            long primitiveHalfWidthRaw = primitive.WidthRaw / 2;
            long primitiveHalfHeightRaw = primitive.HeightRaw / 2;
            long primitiveMinXRaw = primitive.XRaw - primitiveHalfWidthRaw;
            long primitiveMaxXRaw = primitive.XRaw + primitiveHalfWidthRaw;
            long primitiveMinYRaw = primitive.YRaw - primitiveHalfHeightRaw;
            long primitiveMaxYRaw = primitive.YRaw + primitiveHalfHeightRaw;

            return tcMinXRaw <= primitiveMaxXRaw
                && tcMaxXRaw >= primitiveMinXRaw
                && tcMinYRaw <= primitiveMaxYRaw
                && tcMaxYRaw >= primitiveMinYRaw;
        }

        private static void GetFootprintBounds(
            long centerXRaw,
            long centerYRaw,
            int widthTiles,
            int heightTiles,
            out long minXRaw,
            out long minYRaw,
            out long maxXRaw,
            out long maxYRaw)
        {
            long halfWidthRaw = ((long)widthTiles * FixedOneRaw) / 2;
            long halfHeightRaw = ((long)heightTiles * FixedOneRaw) / 2;
            minXRaw = centerXRaw - halfWidthRaw;
            maxXRaw = centerXRaw + halfWidthRaw;
            minYRaw = centerYRaw - halfHeightRaw;
            maxYRaw = centerYRaw + halfHeightRaw;
        }
    }
}
