// MapGenerator.cs
using System;
using System.Linq;
using RandomMapGenerator; // 确保这个 using 是正确的

public class MapGenerator
{
    public int Width { get; }
    public int HeightMap { get; }
    public Tile[,] Tiles { get; private set; }
    private Random _random;

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
        int minPlatformHeight, int actualMaxPlatformHeight,
        double stairProbability)
    {
        InitializeTiles();
        CreatePlatforms(numberOfPlatforms, minPlatformWidth, maxPlatformWidth, minPlatformLength, maxPlatformLength, minPlatformHeight, actualMaxPlatformHeight);
        
        int maxIterations = Math.Max(3, actualMaxPlatformHeight + 2); 
        CreateStairsIterative(stairProbability, maxIterations);
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
                    Type = TileType.Ground,
                    Direction = StairDirection.None
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
            
            if (platformWidth <= 0 || platformLength <= 0) continue;
            if (Width - platformWidth < 0 || HeightMap - platformLength < 0) continue;
            
            var startX = _random.Next(0, Width - platformWidth + 1);
            var startY = _random.Next(0, HeightMap - platformLength + 1);

            for (var y_plat = startY; y_plat < startY + platformLength; y_plat++)
            {
                for (var x_plat = startX; x_plat < startX + platformWidth; x_plat++)
                {
                    Tiles[x_plat, y_plat].Height = platformHeight;
                    Tiles[x_plat, y_plat].Type = TileType.Ground; 
                    Tiles[x_plat, y_plat].Direction = StairDirection.None;
                }
            }
        }
    }

    private void CreateStairsIterative(double stairProbability, int maxIterations)
    {
        bool changesMadeThisPass;
        int iterations = 0;

        do
        {
            changesMadeThisPass = false;
            iterations++;

            for (int y_curr = 0; y_curr < HeightMap; y_curr++)
            {
                for (int x_curr = 0; x_curr < Width; x_curr++)
                {
                    Tile currentTile = Tiles[x_curr, y_curr];
                    bool currentTileWasModifiedIntoStair = false; // 用于标记 currentTile 本身是否在本轮邻居检查中变成了楼梯

                    var neighborChecks = new[]
                    {
                        new { dx = 0, dy = -1, dirToNeighbor = StairDirection.North, dirFromNeighbor = StairDirection.South },
                        new { dx = 1, dy = 0,  dirToNeighbor = StairDirection.East,  dirFromNeighbor = StairDirection.West  },
                        new { dx = 0, dy = 1,  dirToNeighbor = StairDirection.South, dirFromNeighbor = StairDirection.North },
                        new { dx = -1, dy = 0, dirToNeighbor = StairDirection.West,  dirFromNeighbor = StairDirection.East  }
                    };
                    
                    var randomizedChecks = neighborChecks.OrderBy(_ => _random.Next()).ToList();

                    foreach (var check in randomizedChecks)
                    {
                        int nx = x_curr + check.dx;
                        int ny = y_curr + check.dy;

                        if (nx >= 0 && nx < Width && ny >= 0 && ny < HeightMap)
                        {
                            Tile neighborTile = Tiles[nx, ny];

                            // 情况1: currentTile (较低的) 尝试成为楼梯，连接到 neighborTile (较高的锚点)
                            // currentTile 必须是 Ground 才能变成楼梯。
                            // neighborTile (锚点) 可以是 Ground 或另一个 Stair (连接到其底部)。
                            if (currentTile.Type == TileType.Ground && currentTile.Height < neighborTile.Height &&
                                (neighborTile.Type == TileType.Ground || neighborTile.Type == TileType.Stair))
                            {
                                if (_random.NextDouble() < stairProbability)
                                {
                                    if (neighborTile.Height == currentTile.Height + 1) // 正好一级落差
                                    {
                                        currentTile.Type = TileType.Stair;
                                        currentTile.Direction = check.dirToNeighbor;
                                        changesMadeThisPass = true;
                                        currentTileWasModifiedIntoStair = true;
                                    }
                                    else if (neighborTile.Height > currentTile.Height + 1) // 落差大于一级
                                    {
                                        currentTile.Height = neighborTile.Height - 1; // 抬高 currentTile 地基
                                        currentTile.Type = TileType.Stair;            // 在新地基上放楼梯
                                        currentTile.Direction = check.dirToNeighbor;
                                        changesMadeThisPass = true;
                                        currentTileWasModifiedIntoStair = true;
                                    }
                                }
                            }
                            // 情况2: neighborTile (较低的) 尝试成为楼梯，连接到 currentTile (较高的锚点)
                            // neighborTile 必须是 Ground 才能变成楼梯。
                            // currentTile (锚点) 可以是 Ground 或另一个 Stair (连接到其底部)。
                            else if (neighborTile.Type == TileType.Ground && neighborTile.Height < currentTile.Height &&
                                     (currentTile.Type == TileType.Ground || currentTile.Type == TileType.Stair))
                            {
                                 if (_random.NextDouble() < stairProbability)
                                {
                                    if (currentTile.Height == neighborTile.Height + 1) // 正好一级落差
                                    {
                                        neighborTile.Type = TileType.Stair;
                                        neighborTile.Direction = check.dirFromNeighbor;
                                        changesMadeThisPass = true;
                                        // currentTile 本身没变，currentTileWasModifiedIntoStair 依然是 false
                                    }
                                    else if (currentTile.Height > neighborTile.Height + 1) // 落差大于一级
                                    {
                                        neighborTile.Height = currentTile.Height - 1; // 抬高 neighborTile 地基
                                        neighborTile.Type = TileType.Stair;           // 在新地基上放楼梯
                                        neighborTile.Direction = check.dirFromNeighbor;
                                        changesMadeThisPass = true;
                                    }
                                }
                            }

                            if (currentTileWasModifiedIntoStair)
                            {
                                break; // currentTile 已经变成了楼梯，停止检查它的其他邻居
                            }
                        }
                    } 
                }
            } 
        } while (changesMadeThisPass && iterations < maxIterations);
    }
}