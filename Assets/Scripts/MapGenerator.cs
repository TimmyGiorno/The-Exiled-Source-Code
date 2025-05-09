using System;
using System.Linq;
using UnityEngine; // Required for Vector2 and Mathf
using RandomMapGenerator; // Ensure this is your correct namespace

public class MapGenerator
{
    // Public properties for the dimensions of the 2D grid storing tile data
    public int GridWidth { get; }
    public int GridHeight { get; }
    public Tile[,] Tiles { get; private set; }

    private System.Random _random;
    private Vector2 _mapGridCenter; // Precise center of the grid for distance calculations

    // Parameters defining the map's shape and features
    private int _overallMapRadius;    // Overall circular radius of the playable map area
    private int _centerFlatRadius;  // Radius of the flat landing area in the center
    private int _landingPadHeight;  // Fixed height of the landing pad

    public MapGenerator(int overallRadius, int centerFlatRadius, int landingPadHeight, int? seed = null)
    {
        _overallMapRadius = Mathf.Max(1, overallRadius); // Ensure radius is at least 1
        _centerFlatRadius = Mathf.Clamp(centerFlatRadius, 0, _overallMapRadius); // Center radius cannot exceed overall radius
        _landingPadHeight = landingPadHeight;

        // The grid must be large enough to encompass the circular map
        GridWidth = _overallMapRadius * 2;
        GridHeight = _overallMapRadius * 2;
        Tiles = new Tile[GridWidth, GridHeight];

        _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

        // Calculate the precise geometric center of the grid
        // E.g., if GridWidth is 30 (indices 0-29), center is 14.5f
        _mapGridCenter = new Vector2((GridWidth - 1) * 0.5f, (GridHeight - 1) * 0.5f);
    }

    public void GenerateMap(
        int numberOfPlatforms,
        int minPlatformWidth, int maxPlatformWidth,
        int minPlatformLength, int maxPlatformLength,
        int minPlatformHeight, int actualMaxPlatformHeight, // actualMaxPlatformHeight comes from MapVisualizer.maxPlatformHeight
        double stairProbability)
    {
        InitializeTiles(); // Sets up circular boundaries and the flat center
        CreatePlatforms(numberOfPlatforms, minPlatformWidth, maxPlatformWidth, minPlatformLength, maxPlatformLength, minPlatformHeight, actualMaxPlatformHeight);
        
        // Iterations for stair generation; can be based on max height differences
        int maxIterations = Math.Max(3, actualMaxPlatformHeight + _landingPadHeight + 5); // Added a small buffer
        CreateStairsIterative(stairProbability, maxIterations);
    }

    private void InitializeTiles()
    {
        for (var y = 0; y < GridHeight; y++)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                Tiles[x, y] = new Tile(x, y); // Create a new Tile object
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), _mapGridCenter);

                if (distanceToCenter <= _centerFlatRadius)
                {
                    // Tiles within the flat center radius
                    Tiles[x, y].Height = _landingPadHeight;
                    Tiles[x, y].Type = TileType.Ground;
                    Tiles[x, y].Direction = StairDirection.None;
                }
                else if (distanceToCenter <= _overallMapRadius)
                {
                    // Tiles outside the flat center but within the overall map radius
                    // Initialize as flat ground; platforms and stairs will modify them later
                    Tiles[x, y].Height = 0; 
                    Tiles[x, y].Type = TileType.Ground;
                    Tiles[x, y].Direction = StairDirection.None;
                }
                else
                {
                    // Tiles outside the overall map radius
                    Tiles[x, y].Type = TileType.Empty;
                    Tiles[x, y].Height = 0; // Height for Empty tiles can be 0 or an indicator like -1
                }
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
            // Platform height can be relative to landing pad or absolute min/max
            var platformHeight = _random.Next(Math.Max(_landingPadHeight, minPlatformHeight), actualMaxPlatformHeight + 1);
            
            if (platformWidth <= 0 || platformLength <= 0) continue;
            
            // Randomly select a starting point for the platform within the grid
            var startX = _random.Next(0, GridWidth - platformWidth + 1);
            var startY = _random.Next(0, GridHeight - platformLength + 1);

            for (var y_plat = startY; y_plat < startY + platformLength; y_plat++)
            {
                for (var x_plat = startX; x_plat < startX + platformWidth; x_plat++)
                {
                    // Ensure the current platform tile is within grid bounds (mostly for safety)
                    if (x_plat < 0 || x_plat >= GridWidth || y_plat < 0 || y_plat >= GridHeight) continue;

                    float distToCenter = Vector2.Distance(new Vector2(x_plat, y_plat), _mapGridCenter);

                    // Do not place platform tiles in the flat center area
                    if (distToCenter <= _centerFlatRadius) continue; 
                    // Do not place platform tiles outside the overall map radius
                    if (distToCenter > _overallMapRadius) continue;
                    
                    // Do not overwrite tiles that should remain Empty (though InitializeTiles sets Ground within radius)
                    if (Tiles[x_plat, y_plat].Type == TileType.Empty) continue;

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
        // Define neighbor check array once outside the loop for minor optimization
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
                    // Skip processing Empty tiles as the starting point for stair checks
                    if (currentTile.Type == TileType.Empty) continue;
                    
                    bool currentTileWasModifiedIntoStair = false;
                    var randomizedChecks = neighborChecks.OrderBy(_ => _random.Next()).ToList(); // Randomize neighbor check order

                    foreach (var check in randomizedChecks)
                    {
                        int nx = x_curr + check.dx;
                        int ny = y_curr + check.dy;

                        // Check if neighbor is within grid bounds
                        if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < GridHeight)
                        {
                            Tile neighborTile = Tiles[nx, ny];
                            // Skip if neighbor is an Empty tile
                            if (neighborTile.Type == TileType.Empty) continue;

                            float dist_ct_to_center = Vector2.Distance(new Vector2(x_curr, y_curr), _mapGridCenter);
                            float dist_nt_to_center = Vector2.Distance(new Vector2(nx, ny), _mapGridCenter);

                            Tile potentialLow = null, potentialHigh = null;
                            StairDirection dirFromLowToHigh = StairDirection.None;
                            bool currentTileIsPotentialLow = false; // Was currentTile the lower one in the pair?
                            float dist_low_to_center = -1, dist_high_to_center = -1; // Distances for potentialLow and potentialHigh

                            // Determine which tile is lower and which is higher
                            if (currentTile.Height < neighborTile.Height) {
                                potentialLow = currentTile; potentialHigh = neighborTile;
                                dirFromLowToHigh = check.dirToNeighbor; currentTileIsPotentialLow = true;
                                dist_low_to_center = dist_ct_to_center; dist_high_to_center = dist_nt_to_center;
                            } else if (neighborTile.Height < currentTile.Height) {
                                potentialLow = neighborTile; potentialHigh = currentTile;
                                dirFromLowToHigh = check.dirFromNeighbor; // Direction from potentialLow (neighbor) to potentialHigh (current)
                                currentTileIsPotentialLow = false;
                                dist_low_to_center = dist_nt_to_center; dist_high_to_center = dist_ct_to_center;
                            } else {
                                continue; // Tiles are at the same height, no stair needed
                            }

                            // --- Conditions for stair placement ---
                            // 1. The tile to become a stair (potentialLow) must be Ground.
                            // 2. The tile it connects to (potentialHigh, the anchor) must be Ground or Stair.
                            // 3. The tile to become a stair (potentialLow) CANNOT be in the flat center.
                            // 4. If the anchor (potentialHigh) is in the flat center, its height MUST be _landingPadHeight.

                            if (!(potentialLow.Type == TileType.Ground &&
                                  (potentialHigh.Type == TileType.Ground || potentialHigh.Type == TileType.Stair))) {
                                continue; // Invalid types for stair formation
                            }

                            if (dist_low_to_center <= _centerFlatRadius) { 
                                continue; // The base of the stair cannot be within the flat center area
                            }

                            if (dist_high_to_center <= _centerFlatRadius && potentialHigh.Height != _landingPadHeight) {
                                // If anchor is in flat center but not at landing pad height, it's an invalid connection
                                continue;
                            }
                            
                            // --- Stair Creation Logic ---
                            if (_random.NextDouble() < stairProbability)
                            {
                                bool modifiedThisPair = false;
                                if (potentialHigh.Height == potentialLow.Height + 1) // Perfect 1-unit difference
                                {
                                    potentialLow.Type = TileType.Stair;
                                    potentialLow.Direction = dirFromLowToHigh;
                                    modifiedThisPair = true;
                                }
                                else if (potentialHigh.Height > potentialLow.Height + 1) // Gap > 1 unit, need to raise base of potentialLow
                                {
                                    int newLowHeight = potentialHigh.Height - 1;
                                    // If the anchor is the center landing pad, the new base height must be landingPadHeight - 1
                                    if (dist_high_to_center <= _centerFlatRadius) 
                                    {
                                        // This implies potentialHigh.Height == _landingPadHeight (due to earlier check)
                                        newLowHeight = _landingPadHeight - 1;
                                    }
                                    
                                    // Prevent creating stairs with a base height < 0, unless landing pad is at 0 and newLowHeight becomes -1.
                                    // This specific case might need special handling or visual representation. For now, skip if newLowHeight is < 0.
                                    // An exception might be if landingPadHeight is 0, making newLowHeight -1. We'll allow it but it might look odd if not handled.
                                    if (newLowHeight < 0) {
                                        // If landingPadHeight is 0 and newLowHeight is -1, this is an edge case.
                                        // For simplicity, let's prevent negative height stairs unless _landingPadHeight itself is 0
                                        // and the stair is leading down from it.
                                        if (!(_landingPadHeight == 0 && newLowHeight == -1) ) {
                                            continue;
                                        }
                                        // If we allow newLowHeight = -1, the visualizer must handle it.
                                        // For safety, one might prefer: if (newLowHeight < 0) continue;
                                    }


                                    potentialLow.Height = newLowHeight;
                                    potentialLow.Type = TileType.Stair;
                                    potentialLow.Direction = dirFromLowToHigh;
                                    modifiedThisPair = true;
                                }

                                if (modifiedThisPair)
                                {
                                    changesMadeThisPass = true;
                                    if (currentTileIsPotentialLow) // If currentTile (outer loop) was the one modified
                                    {
                                        currentTileWasModifiedIntoStair = true;
                                    }
                                }
                            }
                            // --- End Stair Creation Logic ---
                        } // End of neighbor validity check (within grid bounds)

                        if (currentTileWasModifiedIntoStair) {
                            // If currentTile became a stair, break from checking its other neighbors in this pass
                            break; 
                        }
                    } // End foreach neighbor check
                } // End for x_curr
            } // End for y_curr
        } while (changesMadeThisPass && iterations < maxIterations); // Continue if changes were made and max iterations not reached
    }
}