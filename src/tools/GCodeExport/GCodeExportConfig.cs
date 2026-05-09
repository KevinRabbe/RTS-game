namespace RtsGame.GCodeExport
{
    public readonly struct GCodeExportConfig
    {
        public decimal ScaleMmPerTile { get; }
        public decimal SafeZ { get; }
        public decimal DrawZ { get; }
        public decimal FeedRate { get; }

        public GCodeExportConfig(decimal scaleMmPerTile, decimal safeZ, decimal drawZ, decimal feedRate)
        {
            ScaleMmPerTile = scaleMmPerTile;
            SafeZ = safeZ;
            DrawZ = drawZ;
            FeedRate = feedRate;
        }

        public static GCodeExportConfig Default
        {
            get { return new GCodeExportConfig(10m, 5m, -1m, 600m); }
        }
    }
}
