using UnityEngine;

public class TerrainRotateTool : MonoBehaviour
{
    [ContextMenu("Rotate Terrain 90 Counter Clockwise")]
    private void RotateCounterClockwise()
    {
        Terrain terrain = GetComponent<Terrain>();

        if (terrain == null)
        {
            Debug.LogError("Terrain component not found.");
            return;
        }

        TerrainData data = terrain.terrainData;

        RotateHeightmap(data);
        RotateHoles(data);

        Debug.Log("Terrain rotated 90 degrees counter-clockwise.");
    }

    private void RotateHeightmap(TerrainData data)
    {
        int resolution = data.heightmapResolution;

        float[,] source =
            data.GetHeights(0, 0, resolution, resolution);

        float[,] rotated =
            new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                rotated[y, x] =
                    source[x, resolution - 1 - y];
            }
        }

        data.SetHeights(0, 0, rotated);
    }

    private void RotateHoles(TerrainData data)
    {
        int resolution = data.holesResolution;

        bool[,] source =
            data.GetHoles(0, 0, resolution, resolution);

        bool[,] rotated =
            new bool[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                rotated[y, x] =
                    source[x, resolution - 1 - y];
            }
        }

        data.SetHoles(0, 0, rotated);
    }
}