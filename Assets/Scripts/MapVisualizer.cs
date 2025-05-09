using UnityEngine;
using RandomMapGenerator; // Ensure this is your correct namespace

public class MapVisualizer : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int overallMapRadius = 20;    // Overall radius of the generated circular map area
    public int centerFlatRadius = 5;   // Radius of the flat landing zone in the center
    public int landingPadHeight = 1;   // Height of the landing pad (recommend >= 0)

    [Header("Platform Generation")]
    public int numberOfPlatforms = 15;
    public int minPlatformWidth = 3;
    public int maxPlatformWidth = 8;
    public int minPlatformLength = 3; // Corresponds to platform's depth/length along map's Y-grid axis
    public int maxPlatformLength = 8;
    public int minPlatformHeight = 1;  // Min height for platforms (can be relative or absolute)
    public int maxPlatformHeight = 5;  // Max height for platforms

    [Header("Generation Settings")]
    public double stairProbability = 0.75;
    public int? mapSeed = null; // Nullable int for optional seed

    [Header("Visuals")]
    public GameObject groundTilePrefab; // Prefab for ground and platform tops
    public GameObject stairTilePrefab;  // Prefab for stairs
    public float tileSpacing = 1.0f;   // Spacing between tile centers (usually tile size)
    public float heightStep = 0.5f;    // Actual Y-axis distance in Unity units per height unit
    public Color defaultTileColor = Color.gray; // Uniform color for all tiles

    private MapGenerator _mapGenerator;
    private GameObject _mapContainer; // Parent object for all instantiated tiles

    void Start()
    {
        GenerateAndDisplayMap();
    }

    // Allows triggering map generation from the Inspector context menu
    [ContextMenu("Generate and Display Map")]
    public void GenerateAndDisplayMap()
    {
        // Clean up old map if it exists
        if (_mapContainer != null)
        {
            // Use DestroyImmediate if called from editor mode (e.g., via ContextMenu)
            if (Application.isPlaying) Destroy(_mapContainer);
            else DestroyImmediate(_mapContainer);
        }
        _mapContainer = new GameObject("GeneratedMapContainer");
        _mapContainer.transform.SetParent(this.transform); // Make it a child of this GameObject

        // Ensure landingPadHeight is not negative for simpler platform height calculations later
        int effectiveLandingPadHeight = Mathf.Max(0, landingPadHeight);

        // Initialize the map generator with new parameters
        _mapGenerator = new MapGenerator(overallMapRadius, centerFlatRadius, effectiveLandingPadHeight, mapSeed);

        // Generate map data
        _mapGenerator.GenerateMap(
            numberOfPlatforms,
            minPlatformWidth, maxPlatformWidth,
            minPlatformLength, maxPlatformLength,
            minPlatformHeight, maxPlatformHeight, // maxPlatformHeight here is passed as actualMaxPlatformHeight
            stairProbability
        );

        // Visualize the generated map data
        DisplayMap();
    }
    
    void DisplayMap()
    {
        if (_mapGenerator == null || _mapGenerator.Tiles == null) 
        {
            Debug.LogError("Map data not generated!");
            return; 
        }
        if (groundTilePrefab == null) 
        {
            Debug.LogError("Ground Tile Prefab not assigned in MapVisualizer!");
            return; 
        }
        if (stairTilePrefab == null) 
        {
            Debug.LogError("Stair Tile Prefab not assigned in MapVisualizer!");
            return; 
        }

        // Calculate offset to center the map visuals in the world based on the grid dimensions
        float mapHalfGridWidth = _mapGenerator.GridWidth / 2.0f;
        float mapHalfGridDepth = _mapGenerator.GridHeight / 2.0f; // GridHeight corresponds to map's Z-axis depth

        for (int y = 0; y < _mapGenerator.GridHeight; y++) // Iterate through map's depth (grid Y)
        {
            for (int x = 0; x < _mapGenerator.GridWidth; x++) // Iterate through map's width (grid X)
            {
                Tile currentTileData = _mapGenerator.Tiles[x, y];

                // Skip rendering Empty tiles
                if (currentTileData.Type == TileType.Empty)
                {
                    continue;
                }

                GameObject tilePrefabToUse = null;
                Quaternion tileRotation = Quaternion.identity;
                
                // Select prefab and rotation based on tile type and direction
                if (currentTileData.Type == TileType.Ground) 
                { 
                    tilePrefabToUse = groundTilePrefab; 
                }
                else if (currentTileData.Type == TileType.Stair) 
                { 
                    tilePrefabToUse = stairTilePrefab;
                    // Assuming default stair model ascends towards its local +Z axis (Unity's forward)
                    switch (currentTileData.Direction)
                    {
                        case StairDirection.North: // Ascends towards world Z- (if map Y is world Z)
                            tileRotation = Quaternion.Euler(0, 180f, 0); 
                            break;
                        case StairDirection.East:  // Ascends towards world X+
                            tileRotation = Quaternion.Euler(0, 90f, 0);  
                            break;
                        case StairDirection.South: // Ascends towards world Z+
                            tileRotation = Quaternion.Euler(0, 0, 0); // Default orientation
                            break;
                        case StairDirection.West:  // Ascends towards world X-
                            tileRotation = Quaternion.Euler(0, -90f, 0); 
                            break;
                    }
                }
                 
                if (tilePrefabToUse == null) continue; // Should not happen if Empty is handled

                // Calculate world position for the base of the tile
                // Map X -> World X
                // Tile Height -> World Y
                // Map Y (depth) -> World Z
                Vector3 tileBasePosition = new Vector3(
                    (x - mapHalfGridWidth + 0.5f) * tileSpacing,  // Center X
                    currentTileData.Height * heightStep,          // Set Y based on tile's base height
                    (y - mapHalfGridDepth + 0.5f) * tileSpacing   // Center Z (using map's Y as depth)
                );

                GameObject tileInstance = Instantiate(tilePrefabToUse, tileBasePosition, tileRotation);
                tileInstance.transform.SetParent(_mapContainer.transform);
                tileInstance.name = $"Tile_{x}_{y} (H:{currentTileData.Height}, T:{currentTileData.Type}, Dir:{currentTileData.Direction})";
                
                // --- Appearance Adjustment: Apply uniform color ---
                Renderer tileRenderer = tileInstance.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material.color = defaultTileColor;
                }

                // --- Generate Support Blocks ---
                // Support blocks are needed if the tile's base height is above 0 (or above a designated ground level)
                if (currentTileData.Height > 0) // Or currentTileData.Height > absoluteGroundLevel if you have one
                {
                    for (int h_support = 0; h_support < currentTileData.Height; h_support++)
                    {
                        Vector3 supportPosition = new Vector3(
                            tileBasePosition.x,     // Align X with the tile above
                            h_support * heightStep, // Y position for each support block layer
                            tileBasePosition.z      // Align Z with the tile above
                        );

                        GameObject supportTileInstance = Instantiate(groundTilePrefab, supportPosition, Quaternion.identity);
                        supportTileInstance.transform.SetParent(_mapContainer.transform);
                        supportTileInstance.name = $"Tile_{x}_{y}_Support_H{h_support}";
                        
                        Renderer supportRenderer = supportTileInstance.GetComponent<Renderer>();
                        if (supportRenderer != null)
                        {
                            supportRenderer.material.color = defaultTileColor; // Also use default color for supports
                        }
                    }
                }
            }
        }
        Debug.Log("Circular map with flat center displayed!");
    }
}