using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Terrain))]
public sealed class TerrainMeshGenerator : MonoBehaviour
{
    public Material material;

    [Range(33, 513)]
    public int resolution = 257;

    public bool hideOriginal = false;

    [ContextMenu("Generate Terrain Mesh")]
    public void GenerateMesh()
    {
        Terrain terrain = GetComponent<Terrain>();
        TerrainData data = terrain.terrainData;

        if (data == null || material == null)
        {
            Debug.LogError(
                "Assign TerrainData and Material first.",
                this
            );
            return;
        }

        int n = Mathf.Clamp(resolution, 33, 513);

        Vector3[] vertices = new Vector3[n * n];
        Vector2[] uv = new Vector2[n * n];

        int[] triangles =
            new int[(n - 1) * (n - 1) * 6];

        // -----------------------------
        // Vertices
        // Terrain 그대로 생성
        // -----------------------------

        for (int z = 0; z < n; z++)
        {
            for (int x = 0; x < n; x++)
            {
                float u = (float)x / (n - 1);
                float v = (float)z / (n - 1);

                int i = z * n + x;

                float height =
                    data.GetInterpolatedHeight(u, v);

                vertices[i] = new Vector3(
                    u * data.size.x,
                    height,
                    v * data.size.z
                );

                uv[i] = new Vector2(u, v);
            }
        }

        // -----------------------------
        // Triangles
        // 위쪽을 향하는 정방향
        // -----------------------------

        int index = 0;

        for (int z = 0; z < n - 1; z++)
        {
            for (int x = 0; x < n - 1; x++)
            {
                int a = z * n + x;

                triangles[index++] = a;
                triangles[index++] = a + n;
                triangles[index++] = a + 1;

                triangles[index++] = a + 1;
                triangles[index++] = a + n;
                triangles[index++] = a + n + 1;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "Terrain Mesh",
            indexFormat = IndexFormat.UInt32
        };

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            string path =
                UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                    "Assets/TerrainMesh.asset"
                );

            UnityEditor.AssetDatabase.CreateAsset(
                mesh,
                path
            );
        }
#endif

        // -----------------------------
        // Mesh Object 생성
        // -----------------------------

        GameObject meshObject =
            new GameObject("Terrain Mesh");

        UnityEngine.SceneManagement.SceneManager
            .MoveGameObjectToScene(
                meshObject,
                gameObject.scene
            );

        meshObject.layer = gameObject.layer;

        meshObject.transform.position =
            transform.position;

        meshObject.AddComponent<MeshFilter>()
            .sharedMesh = mesh;

        meshObject.AddComponent<MeshRenderer>()
            .sharedMaterial = material;

        meshObject.AddComponent<MeshCollider>()
            .sharedMesh = mesh;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(
                meshObject,
                "Generate Terrain Mesh"
            );
        }
#endif

        if (hideOriginal)
        {
            terrain.enabled = false;

            TerrainCollider sourceCollider =
                GetComponent<TerrainCollider>();

            if (sourceCollider != null)
                sourceCollider.enabled = false;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(terrain);

            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(gameObject.scene);

            UnityEditor.AssetDatabase.SaveAssets();

            UnityEditor.Selection.activeGameObject =
                meshObject;
        }
#endif

        Debug.Log(
            "Created forward-facing Terrain mesh and saved it as a Mesh asset.",
            meshObject
        );
    }
}
