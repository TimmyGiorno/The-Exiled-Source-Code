namespace RandomMapGenerator
{
    public enum TileType
    {
        Ground,    // 普通地面或平台方块
        Stair,     // 表示这个格子是一个楼梯的底部，楼梯会从这个格子向上延伸
        Empty      // （可选）如果你的地图可以有虚空区域
    }

    public enum StairDirection // 重命名并明确含义
    {
        None,
        // 以下表示楼梯从当前格子向该方向【上升】
        North, // 楼梯从此格向北（地图Y轴负方向）上升
        East,  // 楼梯从此格向东（地图X轴正方向）上升
        South, // 楼梯从此格向南（地图Y轴正方向）上升
        West   // 楼梯从此格向西（地图X轴负方向）上升
    }

    public class Tile
    {
        public int X { get; }
        public int Y { get; } // 在 MapVisualizer 中对应 mapDepth
        public int Height { get; set; } // 格子的基础高度
        public TileType Type { get; set; }
        public StairDirection Direction { get; set; } // 如果 Type 是 Stair，此字段表示楼梯上升方向

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