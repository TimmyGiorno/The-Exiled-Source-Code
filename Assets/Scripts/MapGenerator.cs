using System;
using System.Linq;
using UnityEngine; 
using RandomMapGenerator; 

public class MapGenerator
{
    public int GridWidth { get; }
    public int GridHeight { get; }
    public Tile[,] Tiles { get; private set; }

    private System.Random _random;
    private Vector2 _mapGridCenter;

    private int _overallMapRadius;
    private int _centerFlatRadius;
    private int _landingPadHeight;

    // Perlin Noise Parameters
    private float _noiseScale;
    private int _noiseAmplitude;
    private float _noiseOffsetX;
    private float _noiseOffsetY;

    public MapGenerator(int overallRadius, int centerFlatRadius, int landingPadHeight, 
                        float noiseScale, int noiseAmplitude, float noiseOffsetX, float noiseOffsetY, 
                        int? seed = null)
    {
        _overallMapRadius = Mathf.Max(1, overallRadius);
        _centerFlatRadius = Mathf.Clamp(centerFlatRadius, 0, _overallMapRadius);
        _landingPadHeight = landingPadHeight;

        _noiseScale = Mathf.Max(0.001f, noiseScale); 
        _noiseAmplitude = noiseAmplitude;
        _noiseOffsetX = noiseOffsetX;
        _noiseOffsetY = noiseOffsetY;

        GridWidth = _overallMapRadius * 2;
        GridHeight = _overallMapRadius * 2;
        Tiles = new Tile[GridWidth, GridHeight];
        _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        _mapGridCenter = new Vector2((GridWidth - 1) * 0.5f, (GridHeight - 1) * 0.5f);
    }

    public void GenerateMap(
        int numberOfPlatforms,
        int minPlatformWidth, int maxPlatformWidth,
        int minPlatformLength, int maxPlatformLength,
        int minPlatformHeight, int actualMaxPlatformHeight,
        double stairProbability)
    {
        InitializeTilesAndApplyNoise(); 
        CreatePlatforms(numberOfPlatforms, minPlatformWidth, maxPlatformWidth, minPlatformLength, maxPlatformLength, minPlatformHeight, actualMaxPlatformHeight);
        
        int maxIterations = Math.Max(3, actualMaxPlatformHeight + Mathf.Abs(_landingPadHeight) + _noiseAmplitude + 5);
        CreateStairsIterative(stairProbability, maxIterations);
    }

    private void InitializeTilesAndApplyNoise()
    {
        for (var y = 0; y < GridHeight; y++)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                Tiles[x, y] = new Tile(x, y);
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), _mapGridCenter);

                if (distanceToCenter <= _centerFlatRadius)
                {
                    // Tiles within the flat center (landing pad)
                    Tiles[x, y].Height = _landingPadHeight;
                    Tiles[x, y].Type = TileType.Ground;
                    Tiles[x, y].Direction = StairDirection.None;
                }
                else if (distanceToCenter <= _overallMapRadius)
                {
                    // Tiles outside flat center but inside map radius: apply Perlin noise
                    float perlinValue = Mathf.PerlinNoise(
                        (x + _noiseOffsetX) * _noiseScale, 
                        (y + _noiseOffsetY) * _noiseScale
                    );
                    // Mathf.PerlinNoise returns [0,1]. We map it to roughly [-1,1] then scale by amplitude.
                    // (value - 0.5f) * 2 gives [-1,1].
                    int heightOffset = Mathf.RoundToInt((perlinValue - 0.5f) * 2f * _noiseAmplitude);
                    
                    Tiles[x, y].Height = heightOffset; // Base height is now the noise offset from 0
                    Tiles[x, y].Type = TileType.Ground;
                    Tiles[x, y].Direction = StairDirection.None;
                }
                else
                {
                    // Tiles outside the overall map radius
                    Tiles[x, y].Type = TileType.Empty;
                    Tiles[x, y].Height = 0; 
                }
            }
        }
    }

    private void CreatePlatforms(
        int numberOfPlatforms,
        int minPlatformWidth, int maxPlatformWidth,
        int minPlatformLength, int maxPlatformLength,
        int minPlatformHeight, int actualMaxPlatformHeight) // Heights here are absolute
    {
        for (var i = 0; i < numberOfPlatforms; i++)
        {
            var platformWidth = _random.Next(minPlatformWidth, maxPlatformWidth + 1);
            var platformLength = _random.Next(minPlatformLength, maxPlatformLength + 1);
            // Platform heights are absolute, chosen within a range.
            var platformHeight = _random.Next(
                Mathf.Max(_landingPadHeight, minPlatformHeight), 
                actualMaxPlatformHeight + 1
            );
            
            if (platformWidth <= 0 || platformLength <= 0) continue;
            
            var startX = _random.Next(0, GridWidth - platformWidth + 1);
            var startY = _random.Next(0, GridHeight - platformLength + 1);

            for (var y_plat = startY; y_plat < startY + platformLength; y_plat++)
            {
                for (var x_plat = startX; x_plat < startX + platformWidth; x_plat++)
                {
                    if (x_plat < 0 || x_plat >= GridWidth || y_plat < 0 || y_plat >= GridHeight) continue;

                    float distToCenter = Vector2.Distance(new Vector2(x_plat, y_plat), _mapGridCenter);

                    if (distToCenter <= _centerFlatRadius) continue; 
                    if (distToCenter > _overallMapRadius) continue;
                    if (Tiles[x_plat, y_plat].Type == TileType.Empty) continue;

                    // Platforms set their absolute height, overwriting the Perlin noise base for their footprint
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
        var neighborChecks = new[] 
        {
            new { dx = 0, dy = -1, dirToNeighbor = StairDirection.North, dirFromNeighbor = StairDirection.South },
            new { dx = 1, dy = 0,  dirToNeighbor = StairDirection.East,  dirFromNeighbor = StairDirection.West  },
            new { dx = 0, dy = 1,  dirToNeighbor = StairDirection.South, dirFromNeighbor = StairDirection.North },
            new { dx = -1, dy = 0, dirToNeighbor = StairDirection.West,  dirFromNeighbor = StairDirection.East  }
        };

        do
        {
            changesMadeThisPass = false;
            iterations++;

            for (int y_curr = 0; y_curr < GridHeight; y_curr++)
            {
                for (int x_curr = 0; x_curr < GridWidth; x_curr++)
                {
                    Tile currentTile = Tiles[x_curr, y_curr];
                    if (currentTile.Type == TileType.Empty) continue;
                    
                    bool currentTileWasModifiedIntoStair = false;
                    var randomizedChecks = neighborChecks.OrderBy(_ => _random.Next()).ToList();

                    foreach (var check in randomizedChecks)
                    {
                        int nx = x_curr + check.dx;
                        int ny = y_curr + check.dy;

                        if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < GridHeight)
                        {
                            Tile neighborTile = Tiles[nx, ny];
                            if (neighborTile.Type == TileType.Empty) continue;

                            float dist_ct_to_center = Vector2.Distance(new Vector2(x_curr, y_curr), _mapGridCenter);
                            float dist_nt_to_center = Vector2.Distance(new Vector2(nx, ny), _mapGridCenter);

                            Tile potentialLow = null, potentialHigh = null;
                            StairDirection dirFromLowToHigh = StairDirection.None;
                            bool currentTileIsPotentialLow = false;
                            float dist_low_to_center = -1, dist_high_to_center = -1;

                            if (currentTile.Height < neighborTile.Height) {
                                potentialLow = currentTile; potentialHigh = neighborTile;
                                dirFromLowToHigh = check.dirToNeighbor; currentTileIsPotentialLow = true;
                                dist_low_to_center = dist_ct_to_center; dist_high_to_center = dist_nt_to_center;
                            } else if (neighborTile.Height < currentTile.Height) {
                                potentialLow = neighborTile; potentialHigh = currentTile;
                                dirFromLowToHigh = check.dirFromNeighbor; 
                                currentTileIsPotentialLow = false;
                                dist_low_to_center = dist_nt_to_center; dist_high_to_center = dist_ct_to_center;
                            } else {
                                continue; 
                            }

                            if (!(potentialLow.Type == TileType.Ground &&
                                  (potentialHigh.Type == TileType.Ground || potentialHigh.Type == TileType.Stair))) {
                                continue;
                            }

                            if (dist_low_to_center <= _centerFlatRadius) { 
                                continue;
                            }

                            if (dist_high_to_center <= _centerFlatRadius && potentialHigh.Height != _landingPadHeight) {
                                continue;
                            }
                            
                            if (_random.NextDouble() < stairProbability)
                            {
                                bool modifiedThisPair = false;
                                if (potentialHigh.Height == potentialLow.Height + 1) 
                                {
                                    potentialLow.Type = TileType.Stair;
                                    potentialLow.Direction = dirFromLowToHigh;
                                    modifiedThisPair = true;
                                }
                                else if (potentialHigh.Height > potentialLow.Height + 1) 
                                {
                                    int newLowHeight = potentialHigh.Height - 1;
                                    if (dist_high_to_center <= _centerFlatRadius) 
                                    {
                                        newLowHeight = _landingPadHeight - 1;
                                    }

                                    potentialLow.Height = newLowHeight;
                                    potentialLow.Type = TileType.Stair;
                                    potentialLow.Direction = dirFromLowToHigh;
                                    modifiedThisPair = true;
                                }

                                if (modifiedThisPair)
                                {
                                    changesMadeThisPass = true;
                                    if (currentTileIsPotentialLow)
                                    {
                                        currentTileWasModifiedIntoStair = true;
                                    }
                                }
                            }
                        }
                        if (currentTileWasModifiedIntoStair) break; 
                    }
                }
            }
        } while (changesMadeThisPass && iterations < maxIterations);
    }
}