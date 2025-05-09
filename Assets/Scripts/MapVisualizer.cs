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

    [Header("Terrain Noise")] // New section for Perlin noise parameters
    public float noiseScale = 0.1f;
    [Range(0, 10)] // Amplitude as integer units of height change
    public int noiseAmplitude = 2; 
    public float noiseOffsetX = 100f; // Offset to get different noise patterns
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

        int effectiveLandingPadHeight = landingPadHeight; // Can directly use landingPadHeight

        _mapGenerator = new MapGenerator(
            overallMapRadius, centerFlatRadius, effectiveLandingPadHeight,
            noiseScale, noiseAmplitude, noiseOffsetX, noiseOffsetY, // Pass noise parameters
            mapSeed
        );

        _mapGenerator.GenerateMap(
            numberOfPlatforms,
            minPlatformWidth, maxPlatformWidth,
            minPlatformLength, maxPlatformLength,
            minPlatformHeight, maxPlatformHeight,
            stairProbability
        );

        DisplayMap();
    }
    
    void DisplayMap()
    {
        if (_mapGenerator == null || _mapGenerator.Tiles == null) { Debug.LogError("Map data not generated!"); return; }
        if (groundTilePrefab == null) { Debug.LogError("Ground Tile Prefab not assigned!"); return; }
        if (stairTilePrefab == null) { Debug.LogError("Stair Tile Prefab not assigned!"); return; }

        float mapHalfGridWidth = _mapGenerator.GridWidth / 2.0f;
        float mapHalfGridDepth = _mapGenerator.GridHeight / 2.0f;

        for (int y = 0; y < _mapGenerator.GridHeight; y++)
        {
            for (int x = 0; x < _mapGenerator.GridWidth; x++)
            {
                Tile currentTileData = _mapGenerator.Tiles[x, y];

                if (currentTileData.Type == TileType.Empty)
                {
                    continue;
                }

                GameObject tilePrefabToUse = null;
                Quaternion tileRotation = Quaternion.identity;
                
                if (currentTileData.Type == TileType.Ground) 
                { 
                    tilePrefabToUse = groundTilePrefab; 
                }
                else if (currentTileData.Type == TileType.Stair) 
                { 
                    tilePrefabToUse = stairTilePrefab;
                    switch (currentTileData.Direction)
                    {
                        case StairDirection.North: tileRotation = Quaternion.Euler(0, 180f, 0); break;
                        case StairDirection.East:  tileRotation = Quaternion.Euler(0, 90f, 0);  break;
                        case StairDirection.South: tileRotation = Quaternion.Euler(0, 0, 0); break;
                        case StairDirection.West:  tileRotation = Quaternion.Euler(0, -90f, 0); break;
                    }
                }
                 
                if (tilePrefabToUse == null) continue;

                Vector3 tileBasePosition = new Vector3(
                    (x - mapHalfGridWidth + 0.5f) * tileSpacing,
                    currentTileData.Height * heightStep,          
                    (y - mapHalfGridDepth + 0.5f) * tileSpacing  
                );

                GameObject tileInstance = Instantiate(tilePrefabToUse, tileBasePosition, tileRotation);
                tileInstance.transform.SetParent(_mapContainer.transform);
                tileInstance.name = $"Tile_{x}_{y} (H:{currentTileData.Height}, T:{currentTileData.Type}, Dir:{currentTileData.Direction})";
                
                Renderer tileRenderer = tileInstance.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material.color = tileColor; // Use the uniform color
                }

                // --- Generate Support/Extension Blocks (Handles positive and negative heights relative to 0) ---
                int tileH = currentTileData.Height;

                if (tileH > 0) // Tile surface is above level 0, add supports from 0 up to tileH-1
                {
                    for (int h_s = 0; h_s < tileH; h_s++)
                    {
                        Vector3 supportPosition = new Vector3(
                            tileBasePosition.x,    
                            h_s * heightStep, 
                            tileBasePosition.z     
                        );
                        GameObject supportInstance = Instantiate(groundTilePrefab, supportPosition, Quaternion.identity);
                        supportInstance.transform.SetParent(_mapContainer.transform);
                        supportInstance.name = $"Tile_{x}_{y}_Support_PosH{h_s}";
                        Renderer sr = supportInstance.GetComponent<Renderer>();
                        if(sr != null) sr.material.color = tileColor;
                    }
                }
                else if (tileH < 0) // Tile surface is below level 0, add "walls" from -1 down to tileH
                {
                    // These are essentially columns filling the space from just below the "0 plane" down to the tile.
                    // The main tileInstance is already at tileH.
                    // So, we fill levels from -1 down to tileH (inclusive, as these are full blocks).
                    for (int h_s = -1; h_s >= tileH; h_s--) 
                    {
                        // We already placed the main tile at tileH, so skip if h_s is that exact level.
                        // No, the main tile IS the surface. The loop means "place blocks at these levels".
                        // If tileH = -1, loop places block at -1. This is correct.
                        // If tileH = -2, loop places blocks at -1, -2. Correct.
                        Vector3 extensionPosition = new Vector3(
                            tileBasePosition.x,
                            h_s * heightStep,
                            tileBasePosition.z
                        );
                        GameObject extensionInstance = Instantiate(groundTilePrefab, extensionPosition, Quaternion.identity);
                        extensionInstance.transform.SetParent(_mapContainer.transform);
                        extensionInstance.name = $"Tile_{x}_{y}_Extension_NegH{h_s}";
                        Renderer er = extensionInstance.GetComponent<Renderer>();
                        if(er != null) er.material.color = tileColor;
                    }
                }
            }
        }
        Debug.Log("Circular map with noise, flat center, and supports displayed!");
    }
}