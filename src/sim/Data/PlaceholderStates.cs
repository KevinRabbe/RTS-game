namespace RtsGame.Sim.Data
{
    public sealed class EconomyState
    {
        public uint PlaceholderVersion { get; set; } = 1;
        public int NextResourceNodeId { get; set; } = 1;
        public System.Collections.Generic.List<ResourceNode> ResourceNodes { get; } = new System.Collections.Generic.List<ResourceNode>();
    }

    public sealed class PopulationState
    {
        public uint PlaceholderVersion { get; set; } = 1;
    }

    public sealed class MapState
    {
        public ulong MapSeed { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public uint PlaceholderVersion { get; set; } = 1;

        public MapState(ulong mapSeed, int widthTiles, int heightTiles)
        {
            MapSeed = mapSeed;
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
        }
    }

    public sealed class VisibilityState
    {
        public uint PlaceholderVersion { get; set; } = 1;
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public System.Collections.Generic.List<PlayerVisibility> Players { get; } = new System.Collections.Generic.List<PlayerVisibility>();

        public VisibilityState(int playerCount, int widthTiles, int heightTiles)
        {
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
            int tileCount = widthTiles * heightTiles;
            for (int i = 0; i < playerCount; i++)
            {
                Players.Add(new PlayerVisibility(tileCount));
            }
        }

        public int GetIndex(int x, int y)
        {
            return y * WidthTiles + x;
        }
    }

    public sealed class PlayerVisibility
    {
        public bool[] VisibleTiles { get; }
        public bool[] ExploredTiles { get; }

        public PlayerVisibility(int tileCount)
        {
            VisibleTiles = new bool[tileCount];
            ExploredTiles = new bool[tileCount];
        }
    }
}
