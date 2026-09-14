using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One generator for an entire connected group of 300 x 300 Terrains.</summary>
[DisallowMultipleComponent]
public sealed class NaturalTerrainGroupGen : MonoBehaviour
{
    public const string Version = "DIRECT V4 - preserves TerrainData names and asset paths";
    [Header("Assign all tiles, or collect Terrain children")]
    public Terrain[] terrains = new Terrain[0];
    public int seed = 12345;
    [Header("Smooth natural riding terrain (metres)")]
    [Range(15f, 70f)] public float relief = 45f;
    [Range(70f, 300f)] public float landformScale = 140f;
    [Range(0f, 100f)] public float warpStrength = 45f;
    [Range(0f, 0.15f)] public float detailStrength = 0.035f;
    [Range(0f, 40f)] public float floorHeight = 0f;
    [Tooltip("Fixed world X/Z sampling offset. Changing it moves the landscape pattern.")]
    public Vector2 noiseOffset;
    [Min(100f)] public float terrainHeight = 600f;
    private const float TileSize = 300f;

    private void SortTiles()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Sort Terrain Array");
#endif
        Array.Sort(terrains, (a, b) =>
        {
            if (a == b) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int row = a.transform.position.z.CompareTo(b.transform.position.z);
            return row != 0 ? row : a.transform.position.x.CompareTo(b.transform.position.x);
        });
    }

    [ContextMenu("Sort And Rename Only")]
    public void SortAndRenameOnly()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        if (terrains == null || terrains.Length == 0) { Fail("Assign Terrain tiles first."); return; }
        SortTiles();
        var unique = new HashSet<TerrainData>();
        foreach (Terrain tile in terrains)
        {
            if (tile == null || tile.terrainData == null) { Fail("A tile or TerrainData is missing."); return; }
            if (!unique.Add(tile.terrainData)) { Fail("Several tiles share one TerrainData. Assign distinct existing data before naming."); return; }
        }
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain tile = terrains[i];
            UnityEditor.Undo.RecordObject(tile.gameObject, "Rename Terrain");
            tile.name = "Terrain_" + AlphabeticName(i);
            UnityEditor.EditorUtility.SetDirty(tile.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tile.gameObject.scene);
        }
        SortHierarchy();
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("[DIRECT V4] Terrain GameObject names organized; TerrainData names, files and heightmaps unchanged.", this);
#endif
    }

#if UNITY_EDITOR
    private void SortHierarchy()
    {
        // Group the listed tiles in alphabetic order when they share a parent/scene.
        Transform parent = terrains[0].transform.parent;
        int start = int.MaxValue;
        foreach (Terrain tile in terrains)
        {
            if (tile.transform.parent != parent || tile.gameObject.scene != terrains[0].gameObject.scene) return;
            start = Math.Min(start, tile.transform.GetSiblingIndex());
        }
        for (int i = 0; i < terrains.Length; i++)
        {
            UnityEditor.Undo.RecordObject(terrains[i].transform, "Sort Terrain Hierarchy");
            terrains[i].transform.SetSiblingIndex(start + i);
        }
    }
#endif

    [ContextMenu("Collect Child Terrains")]
    public void CollectChildTerrains()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Collect Terrains");
#endif
        terrains = GetComponentsInChildren<Terrain>(true);
        SortTiles();
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
        if (terrains == null || terrains.Length == 0) { Fail("Assign Terrain tiles first."); return; }
        SortTiles();
        Dictionary<Vector2Int, Terrain> grid;
        if (!ValidateTiles(out grid)) return;
        var originalData = new Dictionary<Terrain, TerrainData>();
        var originalGuids = new Dictionary<Terrain, string>();
        var originalNames = new Dictionary<Terrain, string>();
        var originalPaths = new Dictionary<Terrain, string>();
        foreach (Terrain tile in terrains)
        {
            originalData.Add(tile, tile.terrainData);
            originalNames.Add(tile, tile.terrainData.name);
            originalPaths.Add(tile, UnityEditor.AssetDatabase.GetAssetPath(tile.terrainData));
            originalGuids.Add(tile, UnityEditor.AssetDatabase.AssetPathToGUID(
                UnityEditor.AssetDatabase.GetAssetPath(tile.terrainData)));
        }
        var field = new NaturalDirectHeightField(seed,
            Mathf.Clamp(relief, 15f, 70f), Mathf.Clamp(landformScale, 70f, 300f),
            Mathf.Clamp(warpStrength, 0f, 100f), Mathf.Clamp(detailStrength, 0f, 0.15f),
            Mathf.Clamp(floorHeight, 0f, 20f));
        float vertical = Mathf.Max(100f, terrainHeight);
        int resolution = terrains[0].terrainData.heightmapResolution;
        var generated = new Dictionary<Terrain, float[,]>();
        try
        {
            int done = 0;
            foreach (Terrain tile in terrains)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Natural terrain",
                    "Building " + tile.name, (float)done++ / grid.Count);
                float[,] heights = new float[resolution, resolution];
                // No per-tile normalization, smoothing, random seed or edge flattening.
                // Identical world coordinates always produce identical heights.
                double originX = tile.transform.position.x + (double)noiseOffset.x;
                double originZ = tile.transform.position.z + (double)noiseOffset.y;
                for (int z = 0; z < resolution; z++)
                    for (int x = 0; x < resolution; x++)
                        heights[z, x] = (float)(field.Sample(
                            originX + x * 300.0 / (resolution - 1),
                            originZ + z * 300.0 / (resolution - 1)) / vertical);
                generated.Add(tile, heights);
            }

            UnityEditor.Undo.IncrementCurrentGroup();
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName("Generate Natural Terrain Group");
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain tile = terrains[i];
                TerrainData data = originalData[tile];
                if (tile.terrainData != data)
                    throw new InvalidOperationException("Another component replaced TerrainData during generation: " + tile.name);
                string suffix = AlphabeticName(i);
                TerrainCollider collider = tile.GetComponent<TerrainCollider>();
                UnityEditor.Undo.RegisterCompleteObjectUndo(data, "Modify TerrainData");
                UnityEditor.Undo.RecordObject(tile.gameObject, "Rename Terrain");
                UnityEditor.Undo.RecordObject(tile, "Update Terrain");
                if (collider == null) collider = UnityEditor.Undo.AddComponent<TerrainCollider>(tile.gameObject);
                else UnityEditor.Undo.RecordObject(collider, "Update Terrain Collider");
                tile.gameObject.name = "Terrain_" + suffix;
                data.size = new Vector3(TileSize, vertical, TileSize);
                data.SetHeights(0, 0, generated[tile]);
                collider.terrainData = data;
                tile.enabled = true;
                collider.enabled = true;
                // This component sets every neighbor explicitly.
                tile.allowAutoConnect = false;
                UnityEditor.EditorUtility.SetDirty(tile);
                UnityEditor.EditorUtility.SetDirty(tile.gameObject);
                UnityEditor.EditorUtility.SetDirty(data);
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
            SortHierarchy();
            foreach (Terrain tile in terrains)
            {
                string guid = UnityEditor.AssetDatabase.AssetPathToGUID(
                    UnityEditor.AssetDatabase.GetAssetPath(tile.terrainData));
                if (tile.terrainData != originalData[tile] || guid != originalGuids[tile] ||
                    tile.terrainData.name != originalNames[tile] ||
                    UnityEditor.AssetDatabase.GetAssetPath(tile.terrainData) != originalPaths[tile])
                    throw new InvalidOperationException("TerrainData identity, name or asset path changed: " + tile.name);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[DIRECT V4] Updated " + grid.Count + " existing TerrainData objects. " +
                "References, names, paths and GUIDs verified unchanged. Seed: " + seed, this);
        }
        finally
        {
            UnityEditor.EditorUtility.ClearProgressBar();
        }
#else
        Debug.LogWarning("NaturalTerrainGroupDirect is an editor generation tool. Generated terrain works in builds.", this);
#endif
    }

    private bool ValidateTiles(out Dictionary<Vector2Int, Terrain> grid)
    {
        grid = new Dictionary<Vector2Int, Terrain>();
        if (terrains == null || terrains.Length == 0) return Fail("Assign the Terrain tiles first.");
        Terrain first = terrains[0];
        if (first == null) return Fail("The Terrain list contains an empty entry.");
        Vector3 origin = first.transform.position;
        var usedData = new HashSet<TerrainData>();
        foreach (Terrain tile in terrains)
        {
            if (tile == null || tile.terrainData == null) return Fail("A tile or its TerrainData is missing.");
            if (!usedData.Add(tile.terrainData))
                return Fail("Each tile must reference a different TerrainData. Shared data cannot store different tile heights.");
            if (tile.terrainData.heightmapResolution != first.terrainData.heightmapResolution)
                return Fail("All tiles must have the same heightmap resolution.");
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
    public static string AlphabeticName(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        string name = "";
        long value = (long)index + 1;
        while (value > 0)
        {
            value--;
            name = (char)('A' + value % 26) + name;
            value /= 26;
        }
        return name;
    }


    private static Terrain At(Dictionary<Vector2Int, Terrain> grid, Vector2Int key)
    {
        Terrain value;
        return grid.TryGetValue(key, out value) ? value : null;
    }
}

// Continuous world-space height field; no dependence on tile order, count or boundaries.
public sealed class NaturalDirectHeightField
{
    private readonly int seed;
    private readonly double relief, scale, warp, detail, floor;
    public NaturalDirectHeightField(int seed, double relief, double scale, double warp, double detail, double floor)
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
[UnityEditor.CustomEditor(typeof(NaturalTerrainGroupGen))]
public sealed class NaturalTerrainGroupDirectEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var generator = (NaturalTerrainGroupGen)target;
        UnityEditor.EditorGUILayout.HelpBox(
            NaturalTerrainGroupGen.Version + "\n" +
            "Remove previous terrain generator components. Tile order: Z ascending, then X ascending. " +
            "Edits the assigned TerrainData directly. Names tiles Terrain_A...Z, AA... " +
            "TerrainData names and asset file paths stay unchanged. Existing paint, trees, grass and holes are retained. " +
            "Each tile needs unique TerrainData.",
            UnityEditor.MessageType.Info);
        if (GUILayout.Button("Collect Child Terrains")) generator.CollectChildTerrains();
        using (new UnityEditor.EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("DIRECT V4 - Sort And Rename Only")) generator.SortAndRenameOnly();
            if (GUILayout.Button("DIRECT V4 - Generate New Random Landscape")) generator.GenerateRandom();
            if (GUILayout.Button("DIRECT V4 - Generate Using Current Seed")) generator.Generate();
        }
    }
}
#endif


