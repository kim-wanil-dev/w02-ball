using UnityEngine;

public class TerrainHeightExpander : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private float newMaxHeight = 1000f;

    [ContextMenu("Expand Terrain Height")]
    private void ExpandTerrainHeight()
    {
        TerrainData data = terrain.terrainData;

        float oldMaxHeight = data.size.y;

        int resolution = data.heightmapResolution;

        float[,] heights = data.GetHeights(
            0,
            0,
            resolution,
            resolution
        );

        float ratio = oldMaxHeight / newMaxHeight;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[y, x] *= ratio;
            }
        }

        Vector3 size = data.size;
        size.y = newMaxHeight;
        data.size = size;

        data.SetHeights(0, 0, heights);
    }
}