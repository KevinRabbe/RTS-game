namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotCoordinateMapper
    {
        public const long FixedOneRaw = 1L << 16;

        public static float RawToPixels(long raw, float tilePixels)
        {
            return (float)((raw / (double)FixedOneRaw) * tilePixels);
        }

        public static long ScreenToRaw(float screenCoordinate, float tilePixels)
        {
            return (long)((screenCoordinate / tilePixels) * FixedOneRaw);
        }

        public static int ScreenToTile(float screenCoordinate, float tilePixels)
        {
            return FloorToInt(screenCoordinate / tilePixels);
        }

        private static int FloorToInt(float value)
        {
            int truncated = (int)value;
            if (value < truncated)
            {
                return truncated - 1;
            }

            return truncated;
        }
    }
}
