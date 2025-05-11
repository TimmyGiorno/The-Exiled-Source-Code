namespace RandomMapGenerator
{
    public enum TileType
    {
        Ground,
        Stair,
        Empty 
    }

    public enum StairDirection
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
        public int Y { get; } 
        public int Height { get; set; } 
        public TileType Type { get; set; }
        public StairDirection Direction { get; set; } 
        public Tile(int x, int y)
        {
            X = x;
            Y = y;
            Height = 0;
            Type = TileType.Ground; 
            Direction = StairDirection.None;
        }
    }
}