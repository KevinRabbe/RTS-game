using System;
using RtsGame.Sim.Data;

namespace RtsGame.Presentation.GodotBridge
{
    public enum GodotSpriteAssetId
    {
        Villager = 1,
        Infantry = 2,
        Scout = 3,
        TradeCart = 4,
        Capital = 5,
        Wall = 6
    }

    public readonly struct GodotSpriteSheetMetadata
    {
        public GodotSpriteSheetMetadata(
            GodotSpriteAssetId id,
            string displayName,
            string fileName,
            int columns,
            int rows,
            int defaultFrameIndex,
            bool supportsDirectionalFrames,
            int underConstructionFrameIndex)
        {
            Id = id;
            DisplayName = displayName;
            FileName = fileName;
            Columns = columns;
            Rows = rows;
            DefaultFrameIndex = defaultFrameIndex;
            SupportsDirectionalFrames = supportsDirectionalFrames;
            UnderConstructionFrameIndex = underConstructionFrameIndex;
        }

        public GodotSpriteAssetId Id { get; }
        public string DisplayName { get; }
        public string FileName { get; }
        public int Columns { get; }
        public int Rows { get; }
        public int DefaultFrameIndex { get; }
        public bool SupportsDirectionalFrames { get; }
        public int UnderConstructionFrameIndex { get; }
    }

    public readonly struct GodotSpriteFrameRect
    {
        public GodotSpriteFrameRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
    }

    public static class GodotSpriteSheetLayout
    {
        private static readonly GodotSpriteSheetMetadata[] Metadata =
        {
            new GodotSpriteSheetMetadata(GodotSpriteAssetId.Villager, "Villager", "villager_sheet.png", 3, 3, 1, true, 0),
            new GodotSpriteSheetMetadata(GodotSpriteAssetId.Infantry, "Infantry", "infantry_sheet.png", 3, 3, 1, true, 0),
            new GodotSpriteSheetMetadata(GodotSpriteAssetId.Scout, "Scout", "scout_sheet.png", 3, 3, 1, true, 0),
            new GodotSpriteSheetMetadata(GodotSpriteAssetId.TradeCart, "TradeCart", "trade_cart_sheet.png", 3, 3, 1, true, 0),
            new GodotSpriteSheetMetadata(GodotSpriteAssetId.Capital, "Capital", "capital.png", 1, 1, 0, false, 0),
            new GodotSpriteSheetMetadata(GodotSpriteAssetId.Wall, "Wall", "wall_sheet.png", 3, 2, 0, false, 3)
        };

        public static int ExpectedAssetCount => Metadata.Length;

        public static GodotSpriteSheetMetadata[] GetAllMetadata()
        {
            return Metadata;
        }

        public static bool TryGetMetadata(GodotSpriteAssetId id, out GodotSpriteSheetMetadata metadata)
        {
            for (int i = 0; i < Metadata.Length; i++)
            {
                if (Metadata[i].Id == id)
                {
                    metadata = Metadata[i];
                    return true;
                }
            }

            metadata = default;
            return false;
        }

        public static bool TryResolveUnitAsset(int unitTypeId, out GodotSpriteAssetId assetId)
        {
            switch ((UnitTypeId)unitTypeId)
            {
                case UnitTypeId.Villager:
                    assetId = GodotSpriteAssetId.Villager;
                    return true;
                case UnitTypeId.Infantry:
                    assetId = GodotSpriteAssetId.Infantry;
                    return true;
                case UnitTypeId.Scout:
                case UnitTypeId.Cavalry:
                    assetId = GodotSpriteAssetId.Scout;
                    return true;
                case UnitTypeId.TradeCart:
                    assetId = GodotSpriteAssetId.TradeCart;
                    return true;
                default:
                    assetId = default;
                    return false;
            }
        }

        public static bool TryResolveBuildingAsset(int buildingTypeId, out GodotSpriteAssetId assetId)
        {
            switch ((BuildingTypeId)buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    assetId = GodotSpriteAssetId.Capital;
                    return true;
                case BuildingTypeId.Wall:
                    assetId = GodotSpriteAssetId.Wall;
                    return true;
                default:
                    assetId = default;
                    return false;
            }
        }

        public static int ResolveDirectionalFrameIndex(GodotSpriteSheetMetadata metadata, bool hasMoveTarget, int dx, int dy)
        {
            if (!metadata.SupportsDirectionalFrames || !hasMoveTarget)
            {
                return metadata.DefaultFrameIndex;
            }

            if (dx < -1) dx = -1;
            if (dx > 1) dx = 1;
            if (dy < -1) dy = -1;
            if (dy > 1) dy = 1;

            switch ((dx, dy))
            {
                case (-1, -1):
                    return 0;
                case (0, -1):
                    return 1;
                case (1, -1):
                    return 2;
                case (-1, 0):
                    return 3;
                case (0, 0):
                    return metadata.DefaultFrameIndex;
                case (1, 0):
                    return 5;
                case (-1, 1):
                    return 6;
                case (0, 1):
                    return 7;
                case (1, 1):
                    return 8;
                default:
                    return metadata.DefaultFrameIndex;
            }
        }

        public static GodotSpriteFrameRect ResolveFrameRect(GodotSpriteSheetMetadata metadata, int textureWidth, int textureHeight, int frameIndex)
        {
            int columns = Math.Max(1, metadata.Columns);
            int rows = Math.Max(1, metadata.Rows);
            int maxFrame = columns * rows - 1;
            int clampedFrame = Math.Max(0, Math.Min(frameIndex, maxFrame));

            int frameWidth = Math.Max(1, textureWidth / columns);
            int frameHeight = Math.Max(1, textureHeight / rows);
            int x = (clampedFrame % columns) * frameWidth;
            int y = (clampedFrame / columns) * frameHeight;
            return new GodotSpriteFrameRect(x, y, frameWidth, frameHeight);
        }
    }
}
