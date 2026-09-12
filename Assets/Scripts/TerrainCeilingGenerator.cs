using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Terrain))]
public sealed class TerrainCeilingGenerator : MonoBehaviour
{
    [Tooltip("World Y of the ceiling where the source Terrain height is zero.")]
    public float ceilingY = 60f;
    [Tooltip("Use a regular mesh material (e.g. URP/Lit or Standard), not a Terrain shader.")]
    public Material ceilingMaterial;
    [Range(33, 513)] public int resolution = 257;
    public bool hideOriginal = true;

    [ContextMenu("Generate Ceiling")]
    public void GenerateCeiling()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainData data = terrain.terrainData;
        if (data == null || ceilingMaterial == null)
        {
            Debug.LogError("Assign TerrainData and a regular mesh Ceiling Material first.", this);
            return;
        }

        int n = Mathf.Clamp(resolution, 33, 513);
        Vector3[] vertices = new Vector3[n * n];
        Vector2[] uv = new Vector2[n * n];
        int[] triangles = new int[(n - 1) * (n - 1) * 6];
        for (int z = 0; z < n; z++)
        for (int x = 0; x < n; x++)
        {
            float u = (float)x / (n - 1);
            float v = (float)z / (n - 1);
            int i = z * n + x;
            // Reflect the terrain profile vertically, preserving X/Z direction.
            vertices[i] = new Vector3(u * data.size.x,
                -data.GetInterpolatedHeight(u, v), v * data.size.z);
            uv[i] = new Vector2(u, v);
        }

        int index = 0;
        for (int z = 0; z < n - 1; z++)
        for (int x = 0; x < n - 1; x++)
        {
            int a = z * n + x;
            // Downward-facing triangles: visible and collidable from below.
            triangles[index++] = a;
            triangles[index++] = a + 1;
            triangles[index++] = a + n;
            triangles[index++] = a + 1;
            triangles[index++] = a + n + 1;
            triangles[index++] = a + n;
        }

        Mesh mesh = new Mesh { name = "Terrain Ceiling Mesh",
            indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                "Assets/TerrainCeilingMesh.asset");
            UnityEditor.AssetDatabase.CreateAsset(mesh, path);
        }
#endif

        // Independent object so disabling or editing the source cannot move it.
        GameObject ceiling = new GameObject("Terrain Ceiling");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ceiling, gameObject.scene);
        ceiling.layer = gameObject.layer;
        ceiling.transform.position = new Vector3(transform.position.x, ceilingY, transform.position.z);
        ceiling.AddComponent<MeshFilter>().sharedMesh = mesh;
        ceiling.AddComponent<MeshRenderer>().sharedMaterial = ceilingMaterial;
        ceiling.AddComponent<MeshCollider>().sharedMesh = mesh;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(ceiling, "Generate Ceiling");
            UnityEditor.Undo.RecordObject(terrain, "Hide Source Terrain");
            TerrainCollider sourceCollider = GetComponent<TerrainCollider>();
            if (sourceCollider != null)
                UnityEditor.Undo.RecordObject(sourceCollider, "Hide Source Terrain");
        }
#endif

        if (hideOriginal)
        {
            terrain.enabled = false;
            TerrainCollider sourceCollider = GetComponent<TerrainCollider>();
            if (sourceCollider != null) sourceCollider.enabled = false;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(terrain);
            TerrainCollider sourceCollider = GetComponent<TerrainCollider>();
            if (sourceCollider != null) UnityEditor.EditorUtility.SetDirty(sourceCollider);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.Selection.activeGameObject = ceiling;
        }
#endif
        Debug.Log("Created a downward-facing ceiling with MeshCollider. Each generation creates a new object.", ceiling);
    }
}
