// MapVisualizer.cs
using UnityEngine;
using RandomMapGenerator; // 确保引用

public class MapVisualizer : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int mapWidth = 30;
    public int mapDepth = 20;

    [Header("Platform Generation")]
    public int numberOfPlatforms = 10;
    public int minPlatformWidth = 3;
    public int maxPlatformWidth = 8;
    public int minPlatformLength = 3;
    public int maxPlatformLength = 8;
    public int minPlatformHeight = 1; // 平台高度至少为1
    public int maxPlatformHeight = 3;

    [Header("Generation Settings")]
    public double stairProbability = 0.75; // 重命名 slopeProbability
    public int? mapSeed = null;

    [Header("Visuals")]
    public GameObject groundTilePrefab;
    public GameObject stairTilePrefab;  // << 新增：楼梯预制件
    public float tileSpacing = 1.0f;
    public float heightStep = 0.5f;

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
            DestroyImmediate(_mapContainer);
        }
        _mapContainer = new GameObject("GeneratedMapContainer");
        _mapContainer.transform.SetParent(this.transform);

        _mapGenerator = new MapGenerator(mapWidth, mapDepth, mapSeed);

        _mapGenerator.GenerateMap(
            numberOfPlatforms,
            minPlatformWidth, maxPlatformWidth,
            minPlatformLength, maxPlatformLength,
            minPlatformHeight, maxPlatformHeight,
            stairProbability // 使用 stairProbability
        );

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
            Debug.LogError("Ground Tile Prefab not assigned!");
            return;
        }
        if (stairTilePrefab == null) 
        {
            Debug.LogError("Stair Tile Prefab not assigned!");
            return;
        }

        float mapHalfWidth = _mapGenerator.Width / 2.0f;
        float mapHalfDepth = _mapGenerator.HeightMap / 2.0f;

        // 定义你想要的灰色
        Color desiredGrayColor = Color.gray; // 或者 new Color(0.6f, 0.6f, 0.6f) 等自定义灰色

        for (int y = 0; y < _mapGenerator.HeightMap; y++) 
        {
            for (int x = 0; x < _mapGenerator.Width; x++)
            {
                Tile currentTileData = _mapGenerator.Tiles[x, y];
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
                        case StairDirection.North: 
                            tileRotation = Quaternion.Euler(0, 180f, 0);
                            break;
                        case StairDirection.East:  
                            tileRotation = Quaternion.Euler(0, 90f, 0);
                            break;
                        case StairDirection.South: 
                            tileRotation = Quaternion.Euler(0, 0, 0); 
                            break;
                        case StairDirection.West:  
                            tileRotation = Quaternion.Euler(0, -90f, 0);
                            break;
                    }
                }
                else if (currentTileData.Type == TileType.Empty) 
                {
                    continue;
                }
                
                if (tilePrefabToUse == null) continue;

                Vector3 tileBasePosition = new Vector3(
                    (x - mapHalfWidth + 0.5f) * tileSpacing,
                    currentTileData.Height * heightStep, 
                    (y - mapHalfDepth + 0.5f) * tileSpacing  
                );

                GameObject tileInstance = Instantiate(tilePrefabToUse, tileBasePosition, tileRotation);
                tileInstance.transform.SetParent(_mapContainer.transform);
                tileInstance.name = $"Tile_{x}_{y} (H:{currentTileData.Height}, T:{currentTileData.Type}, Dir:{currentTileData.Direction})";
                
                // --- 外观调整: 所有顶层地块统一为灰色 ---
                Renderer tileRenderer = tileInstance.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material.color = desiredGrayColor; // 设置为统一的灰色
                }

                // --- 生成支撑方块 ---
                if (currentTileData.Height > 0)
                {
                    for (int h_support = 0; h_support < currentTileData.Height; h_support++)
                    {
                        Vector3 supportPosition = new Vector3(
                            tileBasePosition.x,     
                            h_support * heightStep, 
                            tileBasePosition.z      
                        );

                        GameObject supportTileInstance = Instantiate(groundTilePrefab, supportPosition, Quaternion.identity);
                        supportTileInstance.transform.SetParent(_mapContainer.transform);
                        supportTileInstance.name = $"Tile_{x}_{y}_Support_H{h_support}";
                        
                        Renderer supportRenderer = supportTileInstance.GetComponent<Renderer>();
                        if (supportRenderer != null)
                        {
                             // --- 外观调整: 所有支撑地块统一为灰色 ---
                            supportRenderer.material.color = desiredGrayColor; // 设置为统一的灰色
                        }
                    }
                }
            }
        }
        Debug.Log("Map displayed with all tiles in gray!");
    }
}