using UnityEngine;

[RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
[DisallowMultipleComponent]
public sealed class RepeatedRampTerrainGenerator : MonoBehaviour
{
    [Header("Component menu > Generate Ramps")]
    public bool generateOnStart = false;

    [Header("Terrain size")]
    [Min(1f)] public float terrainWidth = 300f;
    [Min(1f)] public float terrainLength = 300f;
    [Min(20f)] public float terrainHeight = 600f;

    [Header("Ramp dimensions (metres)")]
    [Range(0f, 20f)] public float rampHeight = 20f;
    [Min(2f)] public float rampLength = 30f;
    [Min(0f)] public float flatGap = 15f;
    [Min(0f)] public float startFlat = 15f;
    [Tooltip("Included in rampLength. The remaining length is the uphill ramp.")]
    [Min(0.5f)] public float descentLength = 3f;
    [Tooltip("Rounds only the entrance; the rest of the uphill stays straight.")]
    [Range(0f, 0.3f)] public float entryRounding = 0.1f;

    private const int Resolution = 1025;

    private void Start()
    {
        if (generateOnStart) GenerateRamps();
    }

    [ContextMenu("Generate Ramps")]
    public void GenerateRamps()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
        if (terrain.terrainData == null)
        {
            Debug.LogError("Assign TerrainData before generating ramps.", this);
            return;
        }

        float width = Mathf.Max(1f, terrainWidth);
        float length = Mathf.Max(1f, terrainLength);
        float verticalSize = Mathf.Max(20f, terrainHeight);
        float height = Mathf.Clamp(rampHeight, 0f, 20f);
        float ramp = Mathf.Max(2f, rampLength);
        float descent = Mathf.Clamp(descentLength, 0.5f, ramp - 0.5f);
        float ascent = ramp - descent;
        float gap = Mathf.Max(0f, flatGap);
        float start = Mathf.Max(0f, startFlat);
        float period = ramp + gap;
        // Generate complete ramps only, leaving any remainder flat.
        int count = Mathf.Max(0, Mathf.FloorToInt((length - start + gap) / period));
        float[,] heights = new float[Resolution, Resolution];

        for (int z = 0; z < Resolution; z++)
        {
            float distance = z * length / (Resolution - 1) - start;
            float y = 0f;
            if (distance >= 0f)
            {
                int index = Mathf.FloorToInt(distance / period);
                float local = distance - index * period;
                if (index < count && local < ramp)
                {
                    y = local <= ascent
                        ? height * Uphill(local / ascent)
                        : height * (1f - (local - ascent) / descent);
                }
            }

            float normalized = Mathf.Clamp(y, 0f, height) / verticalSize;
            // Every X sample in this row has exactly the same height.
            for (int x = 0; x < Resolution; x++) heights[z, x] = normalized;
        }

        TerrainData data = Instantiate(terrain.terrainData);
        data.name = "Generated Repeated Ramps";
        data.heightmapResolution = Resolution;
        data.size = new Vector3(width, verticalSize, length);
        data.SetHeights(0, 0, heights);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                "Assets/GeneratedRepeatedRamps.asset");
            UnityEditor.AssetDatabase.CreateAsset(data, path);
            UnityEditor.Undo.RecordObjects(
                new Object[] { terrain, terrainCollider, transform }, "Generate Ramps");
        }
#endif

        terrain.terrainData = data;
        terrainCollider.terrainData = data;
        // Reset the previous canyon's -10 offset. Floor is now world Y = 0.
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
        terrain.Flush();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(terrain);
            UnityEditor.EditorUtility.SetDirty(terrainCollider);
            UnityEditor.EditorUtility.SetDirty(transform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
        Debug.Log($"Generated {count} ramps along +Z. Floor: 0, height limit: {height}.", this);
    }

    private float Uphill(float t)
    {
        float rounding = Mathf.Clamp(entryRounding, 0f, 0.3f);
        if (rounding <= 0f) return t;
        float value = t < rounding
            ? t * t / (2f * rounding)
            : t - rounding * 0.5f;
        return value / (1f - rounding * 0.5f);
    }
}
