using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One generator for an entire connected group of 300 x 300 Terrains.</summary>
[DisallowMultipleComponent]
public sealed class NaturalTerrainGroup : MonoBehaviour
{
    [Header("Assign all tiles, or collect Terrain children")]
    public Terrain[] terrains = new Terrain[0];
    public int seed = 12345;
    [Header("Smooth natural riding terrain (metres)")]
    [Range(15f, 70f)] public float relief = 45f;
    [Range(70f, 300f)] public float landformScale = 140f;
    [Range(0f, 100f)] public float warpStrength = 45f;
    [Range(0f, 0.15f)] public float detailStrength = 0.035f;
    [Range(0f, 20f)] public float floorHeight = 0f;
    [Tooltip("Fixed world X/Z sampling offset. Changing it moves the landscape pattern.")]
    public Vector2 noiseOffset;
    [Min(100f)] public float terrainHeight = 600f;
    private const int Resolution = 513;
    private const float TileSize = 300f;

    [ContextMenu("Collect Child Terrains")]
    public void CollectChildTerrains()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Collect Terrains");
#endif
        terrains = GetComponentsInChildren<Terrain>(true);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Generate New Random Landscape")]
    public void GenerateRandom()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Change Landscape Seed");
#endif
        seed = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
        Generate();
    }

    [ContextMenu("Generate Using Current Seed")]
    public void Generate()
    {
        // Editor authoring tool: avoids unintentionally modifying scene assets in Play mode.
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Debug.LogWarning("Generate this landscape outside Play mode.", this);
            return;
        }
        Dictionary<Vector2Int, Terrain> grid;
        if (!ValidateTiles(out grid)) return;
        var field = new NaturalRidingHeightField(seed,
            Mathf.Clamp(relief, 15f, 70f), Mathf.Clamp(landformScale, 70f, 300f),
            Mathf.Clamp(warpStrength, 0f, 100f), Mathf.Clamp(detailStrength, 0f, 0.15f),
            Mathf.Clamp(floorHeight, 0f, 20f));
        float vertical = Mathf.Max(100f, terrainHeight);
        var generated = new Dictionary<Terrain, TerrainData>();
        try
        {
            int done = 0;
            foreach (Terrain tile in grid.Values)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Natural terrain",
                    "Building " + tile.name, (float)done++ / grid.Count);
                float[,] heights = new float[Resolution, Resolution];
                // No per-tile normalization, smoothing, random seed or edge flattening.
                // Identical world coordinates always produce identical heights.
                double originX = tile.transform.position.x + (double)noiseOffset.x;
                double originZ = tile.transform.position.z + (double)noiseOffset.y;
                for (int z = 0; z < Resolution; z++)
                for (int x = 0; x < Resolution; x++)
                    heights[z, x] = (float)(field.Sample(
                        originX + x * 300.0 / (Resolution - 1),
                        originZ + z * 300.0 / (Resolution - 1)) / vertical);

                TerrainData source = tile.terrainData;
                var data = new TerrainData { name = "Natural_" + seed + "_" + done };
                generated.Add(tile, data);
                data.heightmapResolution = Resolution;
                data.size = new Vector3(TileSize, vertical, TileSize);
                data.terrainLayers = source.terrainLayers;
                if (source.alphamapLayers > 0)
                {
                    data.alphamapResolution = source.alphamapResolution;
                    data.SetAlphamaps(0, 0, source.GetAlphamaps(
                        0, 0, source.alphamapWidth, source.alphamapHeight));
                }
                data.SetHeights(0, 0, heights);
            }

            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/GeneratedNaturalTerrain"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "GeneratedNaturalTerrain");
            // Finish every heightmap before replacing any of the scene's TerrainData.
            foreach (TerrainData data in generated.Values)
                UnityEditor.AssetDatabase.CreateAsset(data,
                    UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                        "Assets/GeneratedNaturalTerrain/" + data.name + ".asset"));

            UnityEditor.Undo.IncrementCurrentGroup();
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName("Generate Natural Terrain Group");
            foreach (var pair in generated)
            {
                Terrain tile = pair.Key;
                TerrainCollider collider = tile.GetComponent<TerrainCollider>();
                UnityEditor.Undo.RecordObject(tile, "Update Terrain");
                if (collider == null) collider = UnityEditor.Undo.AddComponent<TerrainCollider>(tile.gameObject);
                else UnityEditor.Undo.RecordObject(collider, "Update Terrain Collider");
                tile.terrainData = pair.Value;
                collider.terrainData = pair.Value;
                tile.enabled = true;
                collider.enabled = true;
                // This component sets every neighbor explicitly.
                tile.allowAutoConnect = false;
                UnityEditor.EditorUtility.SetDirty(tile);
                UnityEditor.EditorUtility.SetDirty(collider);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tile.gameObject.scene);
            }
            foreach (var pair in grid)
            {
                Vector2Int p = pair.Key;
                pair.Value.SetNeighbors(At(grid, p + Vector2Int.left), At(grid, p + Vector2Int.up),
                    At(grid, p + Vector2Int.right), At(grid, p + Vector2Int.down));
                pair.Value.Flush();
            }
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("Generated " + grid.Count + " connected natural tiles. Seed: " + seed, this);
        }
        finally
        {
            UnityEditor.EditorUtility.ClearProgressBar();
            foreach (TerrainData data in generated.Values)
                if (data != null && !UnityEditor.AssetDatabase.Contains(data)) DestroyImmediate(data);
        }
#else
        Debug.LogWarning("NaturalTerrainGroup is an editor generation tool. Generated terrain works in builds.", this);
#endif
    }

    private bool ValidateTiles(out Dictionary<Vector2Int, Terrain> grid)
    {
        grid = new Dictionary<Vector2Int, Terrain>();
        if (terrains == null || terrains.Length == 0) return Fail("Assign the Terrain tiles first.");
        Terrain first = terrains[0];
        if (first == null) return Fail("The Terrain list contains an empty entry.");
        Vector3 origin = first.transform.position;
        foreach (Terrain tile in terrains)
        {
            if (tile == null || tile.terrainData == null) return Fail("A tile or its TerrainData is missing.");
            if (!tile.gameObject.scene.IsValid()) return Fail("Assign scene Terrain objects, not prefab assets.");
            if (Quaternion.Angle(tile.transform.rotation, Quaternion.identity) > 0.001f ||
                (tile.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
                return Fail(tile.name + ": Terrain rotation must be zero and world scale must be one.");
            Vector3 size = tile.terrainData.size;
            if (Mathf.Abs(size.x - TileSize) > 0.0001f || Mathf.Abs(size.z - TileSize) > 0.0001f)
                return Fail(tile.name + ": Width and Length must both be 300.");
            Vector3 p = tile.transform.position;
            // Require exact common Y and edge positions rather than silently creating cracks.
            int gx = Mathf.RoundToInt((p.x - origin.x) / TileSize);
            int gz = Mathf.RoundToInt((p.z - origin.z) / TileSize);
            if (p.y != origin.y || (double)p.x != (double)origin.x + gx * 300.0 ||
                (double)p.z != (double)origin.z + gz * 300.0)
                return Fail(tile.name + ": use the same Y and exact 300-unit X/Z spacing for all tiles.");
            Vector2Int key = new Vector2Int(gx, gz);
            if (grid.ContainsKey(key)) return Fail("Duplicate or overlapping Terrain entries.");
            grid.Add(key, tile);
        }
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(Vector2Int.zero);
        visited.Add(Vector2Int.zero);
        Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            foreach (Vector2Int d in directions)
                if (grid.ContainsKey(p + d) && visited.Add(p + d)) queue.Enqueue(p + d);
        }
        return visited.Count == grid.Count || Fail("All assigned tiles must connect along an edge.");
    }

    private bool Fail(string message) { Debug.LogError(message, this); return false; }
    private static Terrain At(Dictionary<Vector2Int, Terrain> grid, Vector2Int key)
    {
        Terrain value;
        return grid.TryGetValue(key, out value) ? value : null;
    }
}

// Continuous world-space height field; no dependence on tile order, count or boundaries.
public sealed class NaturalRidingHeightField
{
    private readonly int seed;
    private readonly double relief, scale, warp, detail, floor;
    public NaturalRidingHeightField(int seed, double relief, double scale, double warp, double detail, double floor)
    {
        this.seed = seed; this.relief = relief; this.scale = scale;
        this.warp = warp; this.detail = detail; this.floor = floor;
    }
    public double Sample(double x, double z)
    {
        // Domain warping breaks straight, grid-aligned valley patterns.
        double wx = x + warp * (Noise(x / (scale * 1.7), z / (scale * 1.7), 17) * 2 - 1);
        double wz = z + warp * (Noise(x / (scale * 1.7), z / (scale * 1.7), 73) * 2 - 1);
        double broad = Noise(wx / scale, wz / scale, 131);
        double banks = Noise(wx / (scale * 0.53), wz / (scale * 0.53), 269);
        double small = Noise(wx / (scale * 0.23), wz / (scale * 0.23), 419);
        // Low detail weight preserves smooth rolling and convex launch crests.
        return floor + relief * ((0.7 - detail) * broad + 0.3 * banks + detail * small);
    }
    private double Noise(double x, double z, int salt)
    {
        int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z);
        double u = Smooth(x - ix), v = Smooth(z - iz);
        double a = Hash(ix, iz, salt), b = Hash(ix + 1, iz, salt);
        double c = Hash(ix, iz + 1, salt), d = Hash(ix + 1, iz + 1, salt);
        return (a + (b - a) * u) * (1 - v) + (c + (d - c) * u) * v;
    }
    private double Hash(int x, int z, int salt)
    {
        unchecked
        {
            uint h = (uint)seed ^ ((uint)x * 374761393u) ^ ((uint)z * 668265263u) ^ ((uint)salt * 2246822519u);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return h / 4294967295.0;
        }
    }
    private static double Smooth(double t) { return t * t * t * (t * (t * 6 - 15) + 10); }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(NaturalTerrainGroup))]
public sealed class NaturalTerrainGroupEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var generator = (NaturalTerrainGroup)target;
        UnityEditor.EditorGUILayout.HelpBox(
            "One generator controls all assigned 300 x 300 tiles. Use equal Y and exact 300-unit spacing. " +
            "Remove old per-tile generators. Existing paint is copied; trees, grass and holes are not copied.",
            UnityEditor.MessageType.Info);
        if (GUILayout.Button("Collect Child Terrains")) generator.CollectChildTerrains();
        using (new UnityEditor.EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Generate New Random Landscape")) generator.GenerateRandom();
            if (GUILayout.Button("Generate Using Current Seed")) generator.Generate();
        }
    }
}
#endif
