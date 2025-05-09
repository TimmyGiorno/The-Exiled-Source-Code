namespace RandomMapGenerator
{
    public enum TileType
    {
        Ground,
        Stair,
        Empty // For areas outside the circular map
    }

    public enum StairDirection
    {
        None,
        North, // Stair ascends towards North (map Y- / world Z-)
        East,  // Stair ascends towards East (map X+ / world X+)
        South, // Stair ascends towards South (map Y+ / world Z+)
        West   // Stair ascends towards West (map X- / world X-)
    }

    public class Tile
    {
        public int X { get; }
        public int Y { get; } // Corresponds to map depth in the grid
        public int Height { get; set; } // Base height of the tile
        public TileType Type { get; set; }
        public StairDirection Direction { get; set; } // If Type is Stair, this is its ascent direction

        public Tile(int x, int y)
        {
            X = x;
            Y = y;
            Height = 0;
            Type = TileType.Ground; // Default to Ground; InitializeTiles will set Empty/specifics
            Direction = StairDirection.None;
        }
    }
}