// MapGenerator.cs
using System;
// 确保你的 Tile 类和相关枚举在正确的命名空间下，或者移除/调整 using 语句
using RandomMapGenerator; // 假设你的 Tile.cs 和枚举在这个命名空间

public class MapGenerator
{
    public int Width { get; }
    public int HeightMap { get; } // 地图的“深度”或Y轴格子数
    public Tile[,] Tiles { get; private set; }
    private Random _random;
    private int _maxPlatformHeightForSlopeGeneration; // 用于存储最大平台高度，供CreateSlopes参考

    public MapGenerator(int width, int heightMap, int? seed = null)
    {
        Width = width;
        HeightMap = heightMap;
        Tiles = new Tile[width, heightMap];
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public void GenerateMap(
        int numberOfPlatforms,
        int minPlatformWidth, int maxPlatformWidth,
        int minPlatformLength, int maxPlatformLength,
        int minPlatformHeight, int actualMaxPlatformHeight, // 将参数重命名以区分
        double slopeProbability)
    {
        // 存储最大平台高度，供 CreateSlopes 方法使用
        _maxPlatformHeightForSlopeGeneration = actualMaxPlatformHeight;

        InitializeTiles();
        CreatePlatforms(numberOfPlatforms, minPlatformWidth, maxPlatformWidth, minPlatformLength, maxPlatformLength, minPlatformHeight, actualMaxPlatformHeight);
        CreateSlopes(slopeProbability);
    }

    private void InitializeTiles()
    {
        for (var y = 0; y < HeightMap; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                Tiles[x, y] = new Tile(x, y)
                {
                    Height = 0,
                    Type = TileType.Ground
                };
            }
        }
    }

    private void CreatePlatforms(
        int numberOfPlatforms,
        int minPlatformWidth, int maxPlatformWidth,
        int minPlatformLength, int maxPlatformLength,
        int minPlatformHeight, int actualMaxPlatformHeight)
    {
        for (var i = 0; i < numberOfPlatforms; i++)
        {
            var platformWidth = _random.Next(minPlatformWidth, maxPlatformWidth + 1);
            var platformLength = _random.Next(minPlatformLength, maxPlatformLength + 1);
            var platformHeight = _random.Next(Math.Max(1, minPlatformHeight), actualMaxPlatformHeight + 1);
            
            // Edge Condition
            if (platformWidth <= 0 || platformLength <= 0) continue;
            if (Width - platformWidth < 0 || HeightMap - platformLength < 0) continue;
            
            // Select Start Point
            var startX = _random.Next(0, Width - platformWidth + 1);
            var startY = _random.Next(0, HeightMap - platformLength + 1);

            for (var y = startY; y < startY + platformLength; y++)
            {
                for (var x = startX; x < startX + platformWidth; x++)
                {
                    // Overwrite
                    Tiles[x, y].Height = platformHeight;
                    Tiles[x, y].Type = TileType.Ground; 
                    Tiles[x, y].SlopeDir = SlopeDirection.None;
                }
            }
        }
    }

    private void CreateSlopes(double slopeProbability)
    {
        bool slopesWereMadeThisPass;
        // 最大迭代次数，防止无限循环，也间接控制了能搭建的“楼梯”的最大高度差
        int maxIterations = this._maxPlatformHeightForSlopeGeneration + 2; // +2 作为一些缓冲
        int iterations = 0;

        do
        {
            slopesWereMadeThisPass = false;
            iterations++;

            for (int y = 0; y < HeightMap; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Tile currentTile = Tiles[x, y];

                    // 斜坡只能从 TileType.Ground 的地块开始延伸
                    if (currentTile.Type == TileType.Slope || currentTile.Type != TileType.Ground)
                    {
                        continue;
                    }

                    int[] dx = { 0, 1, 0, -1 }; // 邻居相对于当前: X方向偏移 (北边的邻居X不变, 东边的邻居X+1, 南边的X不变, 西边的X-1)
                    int[] dy = { -1, 0, 1, 0 }; // 邻居相对于当前: Y方向偏移 (北边的邻居Y-1, 东边的Y不变, 南边的Y+1, 西边的Y不变)
                                                // Unity中Y向上通常为正，这里是数组索引，Y向下为正
                                                // 我们的SlopeDirection语义是“斜坡向下指向的方向”
                                                // (0, -1) 是北方邻居，若斜坡向北，则SlopeDir=North
                                                // (1,  0) 是东方邻居，若斜坡向东，则SlopeDir=East
                                                // (0,  1) 是南方邻居，若斜坡向南，则SlopeDir=South
                                                // (-1, 0) 是西方邻居，若斜坡向西，则SlopeDir=West
                    SlopeDirection[] directions = { SlopeDirection.North, SlopeDirection.East, SlopeDirection.South, SlopeDirection.West };

                    for (int i = 0; i < 4; i++) 
                    {
                        int neighborX = x + dx[i];
                        int neighborY = y + dy[i];

                        if (neighborX >= 0 && neighborX < Width && neighborY >= 0 && neighborY < HeightMap)
                        {
                            Tile neighborTile = Tiles[neighborX, neighborY];

                            if (currentTile.Height == neighborTile.Height + 1) // 标准1-unit slope
                            {
                                if (_random.NextDouble() < slopeProbability)
                                {
                                    currentTile.Type = TileType.Slope;
                                    currentTile.SlopeDir = directions[i];
                                    slopesWereMadeThisPass = true;
                                    break; 
                                }
                            }
                            else if (currentTile.Height > neighborTile.Height + 1) // Gap is > 1 unit
                            {
                                if (_random.NextDouble() < slopeProbability) 
                                {
                                    neighborTile.Height = currentTile.Height - 1;
                                    neighborTile.Type = TileType.Ground; 
                                    neighborTile.SlopeDir = SlopeDirection.None;

                                    currentTile.Type = TileType.Slope;
                                    currentTile.SlopeDir = directions[i];
                                    slopesWereMadeThisPass = true; 
                                    break; 
                                }
                            }
                        }
                    }
                }
            }
        } while (slopesWereMadeThisPass && iterations < maxIterations);
    }
}