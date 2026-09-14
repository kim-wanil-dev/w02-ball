using System.Collections.Generic;
using UnityEngine;

public class CombineCubes : MonoBehaviour
{
    [ContextMenu("Combine Cubes")]
    private void Combine()
    {
        BoxCollider[] boxColliders = GetComponentsInChildren<BoxCollider>();

        if (boxColliders.Length == 0)
        {
            Debug.LogWarning("Child Cube에 BoxCollider가 없습니다.");
            return;
        }

        // --------------------------------------------------
        // 1. 모든 Cube의 Bounds를 Root 로컬 좌표계로 변환
        // --------------------------------------------------

        List<Bounds> boundsList = new();

        foreach (BoxCollider box in boxColliders)
        {
            Bounds worldBounds = box.bounds;

            Vector3 min = transform.InverseTransformPoint(worldBounds.min);
            Vector3 max = transform.InverseTransformPoint(worldBounds.max);

            Vector3 localMin = Vector3.Min(min, max);
            Vector3 localMax = Vector3.Max(min, max);

            boundsList.Add(new Bounds(
                (localMin + localMax) * 0.5f,
                localMax - localMin
            ));
        }

        // --------------------------------------------------
        // 2. 모든 X/Y/Z 경계 좌표 수집
        // --------------------------------------------------

        List<float> xs = new();
        List<float> ys = new();
        List<float> zs = new();

        foreach (Bounds bounds in boundsList)
        {
            xs.Add(bounds.min.x);
            xs.Add(bounds.max.x);

            ys.Add(bounds.min.y);
            ys.Add(bounds.max.y);

            zs.Add(bounds.min.z);
            zs.Add(bounds.max.z);
        }

        xs.Sort();
        ys.Sort();
        zs.Sort();

        // 중복 제거
        xs = RemoveDuplicates(xs);
        ys = RemoveDuplicates(ys);
        zs = RemoveDuplicates(zs);

        // --------------------------------------------------
        // 3. 각 공간 셀이 Cube 영역에 포함되는지 검사
        // --------------------------------------------------

        bool[,,] occupied = new bool[
            xs.Count - 1,
            ys.Count - 1,
            zs.Count - 1
        ];

        for (int x = 0; x < xs.Count - 1; x++)
        {
            for (int y = 0; y < ys.Count - 1; y++)
            {
                for (int z = 0; z < zs.Count - 1; z++)
                {
                    Vector3 center = new Vector3(
                        (xs[x] + xs[x + 1]) * 0.5f,
                        (ys[y] + ys[y + 1]) * 0.5f,
                        (zs[z] + zs[z + 1]) * 0.5f
                    );

                    foreach (Bounds bounds in boundsList)
                    {
                        if (bounds.Contains(center))
                        {
                            occupied[x, y, z] = true;
                            break;
                        }
                    }
                }
            }
        }

        // --------------------------------------------------
        // 4. 노출된 면만 Mesh로 생성
        // --------------------------------------------------

        List<Vector3> vertices = new();
        List<int> triangles = new();

        for (int x = 0; x < xs.Count - 1; x++)
        {
            for (int y = 0; y < ys.Count - 1; y++)
            {
                for (int z = 0; z < zs.Count - 1; z++)
                {
                    if (!occupied[x, y, z])
                        continue;

                    // -X
                    if (!IsOccupied(occupied, x - 1, y, z))
                    {
                        AddFace(
                            vertices,
                            triangles,
                            new Vector3(xs[x], ys[y], zs[z]),
                            new Vector3(xs[x], ys[y + 1], zs[z]),
                            new Vector3(xs[x], ys[y + 1], zs[z + 1]),
                            new Vector3(xs[x], ys[y], zs[z + 1])
                        );
                    }

                    // +X
                    if (!IsOccupied(occupied, x + 1, y, z))
                    {
                        AddFace(
                            vertices,
                            triangles,
                            new Vector3(xs[x + 1], ys[y], zs[z + 1]),
                            new Vector3(xs[x + 1], ys[y + 1], zs[z + 1]),
                            new Vector3(xs[x + 1], ys[y + 1], zs[z]),
                            new Vector3(xs[x + 1], ys[y], zs[z])
                        );
                    }

                    // -Y
                    if (!IsOccupied(occupied, x, y - 1, z))
                    {
                        AddFace(
                            vertices,
                            triangles,
                            new Vector3(xs[x], ys[y], zs[z + 1]),
                            new Vector3(xs[x + 1], ys[y], zs[z + 1]),
                            new Vector3(xs[x + 1], ys[y], zs[z]),
                            new Vector3(xs[x], ys[y], zs[z])
                        );
                    }

                    // +Y
                    if (!IsOccupied(occupied, x, y + 1, z))
                    {
                        AddFace(
                            vertices,
                            triangles,
                            new Vector3(xs[x], ys[y + 1], zs[z]),
                            new Vector3(xs[x + 1], ys[y + 1], zs[z]),
                            new Vector3(xs[x + 1], ys[y + 1], zs[z + 1]),
                            new Vector3(xs[x], ys[y + 1], zs[z + 1])
                        );
                    }

                    // -Z
                    if (!IsOccupied(occupied, x, y, z - 1))
                    {
                        AddFace(
                            vertices,
                            triangles,
                            new Vector3(xs[x + 1], ys[y], zs[z]),
                            new Vector3(xs[x + 1], ys[y + 1], zs[z]),
                            new Vector3(xs[x], ys[y + 1], zs[z]),
                            new Vector3(xs[x], ys[y], zs[z])
                        );
                    }

                    // +Z
                    if (!IsOccupied(occupied, x, y, z + 1))
                    {
                        AddFace(
                            vertices,
                            triangles,
                            new Vector3(xs[x], ys[y], zs[z + 1]),
                            new Vector3(xs[x], ys[y + 1], zs[z + 1]),
                            new Vector3(xs[x + 1], ys[y + 1], zs[z + 1]),
                            new Vector3(xs[x + 1], ys[y], zs[z + 1])
                        );
                    }
                }
            }
        }

        // --------------------------------------------------
        // 5. Mesh 생성
        // --------------------------------------------------

        Mesh combinedMesh = new Mesh
        {
            name = "CombinedCubeMesh"
        };

        combinedMesh.SetVertices(vertices);
        combinedMesh.SetTriangles(triangles, 0);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshFilter.sharedMesh = combinedMesh;

        // --------------------------------------------------
        // 6. Material 설정
        // --------------------------------------------------

        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (renderer == null)
            renderer = gameObject.AddComponent<MeshRenderer>();

        if (boxColliders.Length > 0)
        {
            MeshRenderer sourceRenderer =
                boxColliders[0].GetComponent<MeshRenderer>();

            if (sourceRenderer != null)
                renderer.sharedMaterials = sourceRenderer.sharedMaterials;
        }

        // --------------------------------------------------
        // 7. 기존 Cube 제거
        // --------------------------------------------------

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Debug.Log(
            $"Combined Mesh 생성 완료. " +
            $"Vertices: {vertices.Count}, " +
            $"Triangles: {triangles.Count / 3}"
        );
    }

    private static bool IsOccupied(
        bool[,,] occupied,
        int x,
        int y,
        int z)
    {
        if (x < 0 || x >= occupied.GetLength(0))
            return false;

        if (y < 0 || y >= occupied.GetLength(1))
            return false;

        if (z < 0 || z >= occupied.GetLength(2))
            return false;

        return occupied[x, y, z];
    }

    private static void AddFace(
        List<Vector3> vertices,
        List<int> triangles,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int start = vertices.Count;

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);

        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    private static List<float> RemoveDuplicates(List<float> values)
    {
        List<float> result = new();

        const float epsilon = 0.0001f;

        foreach (float value in values)
        {
            if (result.Count == 0 ||
                Mathf.Abs(result[^1] - value) > epsilon)
            {
                result.Add(value);
            }
        }

        return result;
    }
}
