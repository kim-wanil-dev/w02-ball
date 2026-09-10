using UnityEngine;

[RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
[DisallowMultipleComponent]
public sealed class CanyonTerrainGenerator : MonoBehaviour
{
    [Header("Generate using the component menu: Generate Canyon")]
    public bool generateOnStart = false;

    [Header("Shape (metres)")]
    [Range(10f, 55f)] public float bendAmplitude = 38f;
    [Range(25f, 65f)] public float valleyHalfWidth = 48f;
    [Range(10f, 50f)] public float ridgeHeight = 45f;
    [Range(-10f, 2f)] public float floorHeight = 0f;

    [Header("Broad rollers along the valley")]
    [Range(0f, 12f)] public float rollerHeight = 7f;
    [Range(12f, 30f)] public float rollerHalfLength = 20f;

    private const float Width = 300f;
    private const float Length = 300f;
    private const float Height = 600f;
    private const float BaseY = -10f;
    private const int Resolution = 513;

    private void Start()
    {
        if (generateOnStart) GenerateCanyon();
    }

    [ContextMenu("Generate Canyon")]
    public void GenerateCanyon()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
        if (terrain.terrainData == null)
        {
            Debug.LogError("Assign TerrainData before generating.", this);
            return;
        }

        // Clone so other Terrain objects sharing the source are unaffected.
        TerrainData data = Instantiate(terrain.terrainData);
        data.name = "Generated Canyon";
        data.heightmapResolution = Resolution;
        data.size = new Vector3(Width, Height, Length);

        float[,] heights = new float[Resolution, Resolution];
        float amplitude = Mathf.Clamp(bendAmplitude, 10f, 55f);
        float halfWidth = Mathf.Clamp(valleyHalfWidth, 25f, 65f);
        float floor = Mathf.Clamp(floorHeight, -10f, 2f);
        float ridge = Mathf.Clamp(ridgeHeight, 10f, 50f);

        for (int z = 0; z < Resolution; z++)
        {
            float worldZ = z * Length / (Resolution - 1);
            float phase = 3f * Mathf.PI * worldZ / Length;
            // Exactly three alternating lobes: right, left, right.
            float centerX = Width * 0.5f + amplitude * Mathf.Sin(phase);
            float slope = amplitude * 3f * Mathf.PI / Length * Mathf.Cos(phase);
            // Approximate perpendicular distance keeps width more consistent.
            float distanceScale = Mathf.Sqrt(1f + slope * slope);

            float rollers = Roller(worldZ, 72f) + Roller(worldZ, 168f)
                          + Roller(worldZ, 260f);
            float crest = Mathf.Min(50f, ridge + 3f * Mathf.Sin(phase - 0.6f));

            for (int x = 0; x < Resolution; x++)
            {
                float worldX = x * Width / (Resolution - 1);
                float distance = Mathf.Abs(worldX - centerX) / distanceScale;
                float t = Mathf.Clamp01(distance / halfWidth);
                // Rounded U cross-section: smooth floor, banks and ridge tops.
                float bank = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t);
                float y = Mathf.Lerp(floor, crest, bank);
                y += rollers * (1f - bank);
                heights[z, x] = (Mathf.Clamp(y, -10f, 50f) - BaseY) / Height;
            }
        }

        data.SetHeights(0, 0, heights);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Persist the generated TerrainData so it survives reopening the scene.
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                "Assets/GeneratedCanyon.asset");
            UnityEditor.AssetDatabase.CreateAsset(data, path);
            UnityEditor.Undo.RecordObjects(
                new Object[] { terrain, terrainCollider, transform }, "Generate Canyon");
        }
#endif

        terrain.terrainData = data;
        terrainCollider.terrainData = data;
        Vector3 position = transform.position;
        position.y = BaseY;
        transform.position = position;
        terrain.Flush();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(terrain);
            UnityEditor.EditorUtility.SetDirty(terrainCollider);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
        Debug.Log("Canyon generated: 300 x 300, three bends, world Y -10 to 50.", this);
    }

    private float Roller(float z, float center)
    {
        float t = Mathf.Abs(z - center) / Mathf.Clamp(rollerHalfLength, 12f, 30f);
        if (t >= 1f) return 0f;
        // Smooth compact hill. A moving ball can leave its convex crest.
        float shape = 0.5f + 0.5f * Mathf.Cos(Mathf.PI * t);
        return Mathf.Clamp(rollerHeight, 0f, 12f) * shape;
    }
}
