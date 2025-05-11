using UnityEngine;
using RandomMapGenerator; // Ensure this is your correct namespace

public class MapVisualizer : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int overallMapRadius = 20;
    public int centerFlatRadius = 5;
    public int landingPadHeight = 1; 

    [Header("Platform Generation")]
    public int numberOfPlatforms = 15;
    public int minPlatformWidth = 3;
    public int maxPlatformWidth = 8;
    public int minPlatformLength = 3;
    public int maxPlatformLength = 8;
    public int minPlatformHeight = 1; 
    public int maxPlatformHeight = 5; 

    [Header("Terrain Noise")]
    public float noiseScale = 0.1f;
    [Range(0, 10)]
    public int noiseAmplitude = 2; 
    public float noiseOffsetX = 100f; 
    public float noiseOffsetY = 100f;

    [Header("Generation Settings")]
    public double stairProbability = 0.75;
    public int? mapSeed = null; 

    [Header("Visuals")]
    public GameObject groundTilePrefab;
    public GameObject stairTilePrefab;
    public float tileSpacing = 1.0f;
    public float heightStep = 0.5f;
    public Color tileColor = Color.gray; 

    private MapGenerator _mapGenerator;
    private GameObject _mapContainer;

    void Start()
    {
        GenerateAndDisplayMap();
    }

    [ContextMenu("Generate and Display Map")]
    public void GenerateAndDisplayMap()
    {
        if (_mapContainer != null)
        {
            if (Application.isPlaying) Destroy(_mapContainer);
            else DestroyImmediate(_mapContainer);
        }
        _mapContainer = new GameObject("GeneratedMapContainer");
        _mapContainer.transform.SetParent(this.transform);

        int effectiveLandingPadHeight = landingPadHeight;

        _mapGenerator = new MapGenerator(
            overallMapRadius, centerFlatRadius, effectiveLandingPadHeight,
            noiseScale, noiseAmplitude, noiseOffsetX, noiseOffsetY,
            mapSeed
        );

        _mapGenerator.GenerateMap(
            numberOfPlatforms,
            minPlatformWidth, maxPlatformWidth,
            minPlatformLength, maxPlatformLength,
            minPlatformHeight, maxPlatformHeight,
            stairProbability
        );

        DisplayMapWithOcclusionCulling();
    }

    // Helper function to determine if a conceptual cell (x,y,h) is solid
    private bool IsCellSolid(int x, int y, int h_level)
    {
        // Check grid boundaries
        if (x < 0 || x >= _mapGenerator.GridWidth || y < 0 || y >= _mapGenerator.GridHeight)
        {
            return false; // Outside grid is considered air
        }

        Tile tileData = _mapGenerator.Tiles[x, y];
        if (tileData.Type == TileType.Empty)
        {
            return false; // Empty-typed tiles are air
        }

        // A cell is solid if its height 'h_level' is at or below the surface height of its column
        return h_level <= tileData.Height;
    }
    
    void DisplayMapWithOcclusionCulling() 
    {
        if (_mapGenerator == null || _mapGenerator.Tiles == null) { Debug.LogError("Map data not generated!"); return; }
        if (groundTilePrefab == null) { Debug.LogError("Ground Tile Prefab not assigned!"); return; }
        if (stairTilePrefab == null) { Debug.LogError("Stair Tile Prefab not assigned!"); return; }

        float mapHalfGridWidth = _mapGenerator.GridWidth / 2.0f;
        float mapHalfGridDepth = _mapGenerator.GridHeight / 2.0f;

        // 1. Determine the actual min and max surface heights in the generated map
        int minSurfaceHeight = int.MaxValue;
        int maxSurfaceHeight = int.MinValue;
        bool mapHasAnySolidTiles = false;

        for (int y_scan = 0; y_scan < _mapGenerator.GridHeight; y_scan++)
        {
            for (int x_scan = 0; x_scan < _mapGenerator.GridWidth; x_scan++)
            {
                Tile tile = _mapGenerator.Tiles[x_scan, y_scan];
                if (tile.Type != TileType.Empty)
                {
                    mapHasAnySolidTiles = true;
                    if (tile.Height < minSurfaceHeight) minSurfaceHeight = tile.Height;
                    if (tile.Height > maxSurfaceHeight) maxSurfaceHeight = tile.Height;
                }
            }
        }

        if (!mapHasAnySolidTiles)
        {
            Debug.LogWarning("Map contains no solid tiles to display.");
            return;
        }
        // If minSurfaceHeight remained int.MaxValue, it means all were Empty, handled by mapHasAnySolidTiles
        // If noise can go very low, minSurfaceHeight could be quite negative.

        // 2. Iterate through all conceptual block cells (x, y, h)
        for (int h = minSurfaceHeight; h <= maxSurfaceHeight; h++) // Iterate through relevant height levels
        {
            for (int y_grid = 0; y_grid < _mapGenerator.GridHeight; y_grid++)
            {
                for (int x_grid = 0; x_grid < _mapGenerator.GridWidth; x_grid++)
                {
                    // 3. Is the current cell (x_grid, y_grid, h) solid?
                    if (!IsCellSolid(x_grid, y_grid, h))
                    {
                        continue; // This cell is air, nothing to render here.
                    }

                    // 4. Cell is solid. Check if it's exposed by checking its 6 neighbors.
                    bool isExposed = false;
                    if (!IsCellSolid(x_grid + 1, y_grid, h)) isExposed = true; // Right face exposed
                    if (!isExposed && !IsCellSolid(x_grid - 1, y_grid, h)) isExposed = true; // Left face exposed
                    if (!isExposed && !IsCellSolid(x_grid, y_grid + 1, h)) isExposed = true; // Front face (map Y+) exposed
                    if (!isExposed && !IsCellSolid(x_grid, y_grid - 1, h)) isExposed = true; // Back face (map Y-) exposed
                    if (!isExposed && !IsCellSolid(x_grid, y_grid, h + 1)) isExposed = true; // Top face exposed
                    if (!isExposed && !IsCellSolid(x_grid, y_grid, h - 1)) isExposed = true; // Bottom face exposed

                    if (isExposed)
                    {
                        // This block needs to be rendered.
                        Tile surfaceColumnData = _mapGenerator.Tiles[x_grid, y_grid]; // Get data for the column's top
                        GameObject prefabToUse;
                        Quaternion rotation = Quaternion.identity;

                        // 5. Select prefab and rotation
                        // If current block is the surface block of its column AND it's a stair type:
                        if (h == surfaceColumnData.Height && surfaceColumnData.Type == TileType.Stair)
                        {
                            prefabToUse = stairTilePrefab;
                            switch (surfaceColumnData.Direction)
                            {
                                case StairDirection.North: rotation = Quaternion.Euler(0, 180f, 0); break;
                                case StairDirection.East:  rotation = Quaternion.Euler(0, 90f, 0);  break;
                                case StairDirection.South: rotation = Quaternion.Euler(0, 0, 0);   break;
                                case StairDirection.West:  rotation = Quaternion.Euler(0, -90f, 0); break;
                            }
                        }
                        else
                        {
                            // Otherwise, it's a ground block (either a surface ground block or an internal block that's exposed)
                            prefabToUse = groundTilePrefab;
                        }
                        
                        Vector3 position = new Vector3(
                            (x_grid - mapHalfGridWidth + 0.5f) * tileSpacing,
                            h * heightStep, // Use the current height level 'h' for this block's Y position
                            (y_grid - mapHalfGridDepth + 0.5f) * tileSpacing
                        );

                        GameObject instance = Instantiate(prefabToUse, position, rotation);
                        instance.transform.SetParent(_mapContainer.transform);
                        // Naming can be more specific if needed, e.g., including 'h'
                        instance.name = $"Block_{x_grid}_{y_grid}_H{h}"; 
                        
                        Renderer rend = instance.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            rend.material.color = tileColor; // Apply uniform color
                        }
                    }
                }
            }
        }
        Debug.Log("Map displayed with hidden block culling!");
    }
}