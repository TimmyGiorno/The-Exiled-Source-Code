using UnityEngine;
// 如果 Tile.cs 和 MapGenerator.cs 在 namespace 中，确保 using
using RandomMapGenerator; // 假设 Tile.cs 和 MapGenerator.cs 都在这个 namespace

public class MapVisualizer : MonoBehaviour
{
    [Header("Map Dimensions")]
    public int mapWidth = 30;
    public int mapDepth = 20; // 使用 Depth 对应我们逻辑中的 HeightMap (地图的Y轴长度)

    [Header("Platform Generation")]
    public int numberOfPlatforms = 10;
    public int minPlatformWidth = 3;
    public int maxPlatformWidth = 8;
    public int minPlatformLength = 3; // 对应地图的 depth/Y方向
    public int maxPlatformLength = 8;
    public int minPlatformHeight = 1;
    public int maxPlatformHeight = 3;

    [Header("Generation Settings")]
    public double slopeProbability = 0.75;
    public int? mapSeed = null; // int? 表示可为空的整数。如果为null，则随机种子

    [Header("Visuals")]
    public GameObject groundTilePrefab; // 用于普通地面和平台顶部的预制件
    public GameObject slopeTilePrefab;  // 用于斜坡的预制件 (可选，可以先用groundTilePrefab代替)
    public float tileSpacing = 1.0f; // 地块之间的间隔 (通常是地块的大小)
    public float heightStep = 0.5f;  // 每一个高度单位在Y轴上的实际距离

    private MapGenerator _mapGenerator;
    private GameObject _mapContainer; // 用于存放所有生成的地块，方便管理

    void Start()
    {
        GenerateAndDisplayMap();
    }

    // 也可以做一个按钮，在编辑器模式下点击生成
    [ContextMenu("Generate and Display Map")] // 这会在 Inspector 中添加一个右键菜单项
    public void GenerateAndDisplayMap()
    {
        // 清理旧地图 (如果存在)
        if (_mapContainer != null)
        {
            DestroyImmediate(_mapContainer); // 使用 DestroyImmediate 如果在编辑器模式下调用
        }
        _mapContainer = new GameObject("GeneratedMapContainer");
        _mapContainer.transform.SetParent(this.transform); // 将容器作为此对象的子对象

        // 初始化生成器
        _mapGenerator = new MapGenerator(mapWidth, mapDepth, mapSeed); // 使用 mapDepth

        // 生成地图数据
        _mapGenerator.GenerateMap(
            numberOfPlatforms,
            minPlatformWidth, maxPlatformWidth,
            minPlatformLength, maxPlatformLength,
            minPlatformHeight, maxPlatformHeight,
            slopeProbability
        );

        // 可视化地图
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

        float mapHalfWidth = _mapGenerator.Width / 2.0f;
        float mapHalfDepth = _mapGenerator.HeightMap / 2.0f;

        for (int y = 0; y < _mapGenerator.HeightMap; y++)
        {
            for (int x = 0; x < _mapGenerator.Width; x++)
            {
                Tile currentTileData = _mapGenerator.Tiles[x, y];

                // 确定顶层地块使用的预制件
                GameObject topTilePrefabToUse = groundTilePrefab;
                if (currentTileData.Type == TileType.Slope && slopeTilePrefab != null)
                {
                    topTilePrefabToUse = slopeTilePrefab;
                }
                else if (currentTileData.Type == TileType.Empty)
                {
                    continue; // 如果是 Empty 类型，不创建任何对象
                }

                // 计算顶层地块的世界坐标
                Vector3 topPosition = new Vector3(
                    (x - mapHalfWidth + 0.5f) * tileSpacing,
                    currentTileData.Height * heightStep,
                    (y - mapHalfDepth + 0.5f) * tileSpacing
                );

                // 实例化顶层地块
                GameObject topTileInstance = Instantiate(topTilePrefabToUse, topPosition, Quaternion.identity);
                topTileInstance.transform.SetParent(_mapContainer.transform);
                topTileInstance.name = $"Tile_{x}_{y}_Top (H:{currentTileData.Height}, T:{currentTileData.Type})";

                // --- 外观调整 (顶层地块) ---
                Renderer topTileRenderer = topTileInstance.GetComponent<Renderer>();
                if (topTileRenderer != null)
                {
                    if (currentTileData.Type == TileType.Ground)
                    {
                        float hue = (currentTileData.Height % maxPlatformHeight) / (float)maxPlatformHeight;
                        topTileRenderer.material.color = Color.HSVToRGB(0.3f + hue * 0.2f, 0.7f, 0.8f);
                    }
                    else if (currentTileData.Type == TileType.Slope)
                    {
                        if (slopeTilePrefab == null)
                        {
                            topTileRenderer.material.color = Color.yellow;
                            // 旋转以匹配斜坡方向
                            Quaternion slopeRotation = Quaternion.identity;
                            switch (currentTileData.SlopeDir)
                            {
                                case SlopeDirection.North:
                                    slopeRotation = Quaternion.Euler(20, 0, 0);
                                    break;
                                case SlopeDirection.East:
                                    slopeRotation = Quaternion.Euler(0, 0, -20);
                                    break;
                                case SlopeDirection.South:
                                    slopeRotation = Quaternion.Euler(-20, 0, 0);
                                    break;
                                case SlopeDirection.West:
                                    slopeRotation = Quaternion.Euler(0, 0, 20);
                                    break;
                            }
                            topTileInstance.transform.rotation = slopeRotation;
                        }
                        else
                        {
                            // 旋转斜坡预制件以匹配方向
                            Quaternion slopeRotation = Quaternion.identity;
                            switch (currentTileData.SlopeDir)
                            {
                                case SlopeDirection.North:
                                    slopeRotation = Quaternion.Euler(0, 180, 0);
                                    break;
                                case SlopeDirection.East:
                                    slopeRotation = Quaternion.Euler(0, -90, 0);
                                    break;
                                case SlopeDirection.South:
                                    break;
                                case SlopeDirection.West:
                                    slopeRotation = Quaternion.Euler(0, 90, 0);
                                    break;
                            }
                            topTileInstance.transform.rotation = slopeRotation;
                        }
                    }
                }

                // 向下生成支撑地块
                if (currentTileData.Height > 0) // 如果高度大于 0，说明不是最底层
                {
                    for (int h = 0; h < currentTileData.Height; h++)
                    {
                        Vector3 supportPosition = new Vector3(
                            (x - mapHalfWidth + 0.5f) * tileSpacing,
                            h * heightStep, // 支撑地块的高度从 0 开始，直到顶层地块的高度 - 1
                            (y - mapHalfDepth + 0.5f) * tileSpacing
                        );

                        GameObject supportTileInstance = Instantiate(groundTilePrefab, supportPosition, Quaternion.identity);
                        supportTileInstance.transform.SetParent(_mapContainer.transform);
                        supportTileInstance.name = $"Tile_{x}_{y}_Support_H{h}";

                        // --- 外观调整 (支撑地块，可以与顶层不同) ---
                        Renderer supportTileRenderer = supportTileInstance.GetComponent<Renderer>();
                        if (supportTileRenderer != null)
                        {
                            supportTileRenderer.material.color = new Color(0.5f, 0.5f, 0.5f); // 例如，灰色表示支撑
                        }
                    }
                }
            }
        }
        Debug.Log("Map displayed with support!");
    }
}