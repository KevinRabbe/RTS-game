namespace RtsGame.GCodeExport
{
    public readonly struct GCodePathPoint
    {
        public int Tick { get; }
        public int XTile { get; }
        public int YTile { get; }

        public GCodePathPoint(int tick, int xTile, int yTile)
        {
            Tick = tick;
            XTile = xTile;
            YTile = yTile;
        }
    }
}
