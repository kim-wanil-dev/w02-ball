using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class RaiseTerrainHeight : MonoBehaviour
{
    [SerializeField] private float _heightOffset = 10f;

    private Terrain _terrain;

    private void Awake()
    {
    }

    [ContextMenu("Apply Height Offset")]
    private void ApplyHeightOffset()
    {
        _terrain = GetComponent<Terrain>();
        TerrainData terrainData = _terrain.terrainData;

        int resolution = terrainData.heightmapResolution;

        float[,] heights = terrainData.GetHeights(
            0,
            0,
            resolution,
            resolution
        );

        // TerrainData의 실제 높이 범위(월드 단위)
        float terrainHeight = terrainData.size.y;

        // 입력한 월드 높이를 Heightmap 0~1 값으로 변환
        float normalizedOffset = _heightOffset / terrainHeight;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[y, x] = Mathf.Clamp01(
                    heights[y, x] + normalizedOffset
                );
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }
}
