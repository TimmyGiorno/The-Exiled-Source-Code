namespace RandomMapGenerator
{
    public enum TileType
    {
        Empty,
        Ground,
        Slope,
        Water,
        Lava,
        Wall,
    }
    
    public enum SlopeDirection
    {
        None,
        North,
        East,
        South,
        West
    }
    
    public class Tile
    {
        public int X { get; }
        public int Y { get; } // 在 Unity 中，这通常映射到 Z 轴
        public int Height { get; set; } // 在 Unity 中，这通常映射到 Y 轴
        public TileType Type { get; set; }
        public SlopeDirection SlopeDir { get; set; }
    
        public Tile(int x, int y)
        {
            X = x;
            Y = y;
            Height = 0;
            Type = TileType.Empty;
            SlopeDir = SlopeDirection.None;
        }
    }
}
