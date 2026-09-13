using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class FlowClimbTrackGenerator : MonoBehaviour
{
    [Header("Track Size")]
    [SerializeField] private float _height = 40f;
    [SerializeField] private float _length = 60f;
    [SerializeField] private float _width = 30f;

    [Header("Width Shape")]
    [SerializeField] private float _bottomEndWidth = 10f;
    [SerializeField] private float _topEndWidth = 10f;

    [Range(0f, 0.5f)]
    [SerializeField] private float _bottomWidenRatio = 0.2f;

    [Range(0.5f, 1f)]
    [SerializeField] private float _topNarrowStartRatio = 0.8f;

    [Range(0f, 0.4f)]
    [SerializeField] private float _flatStartRatio = 0.2f;

    [Range(0f, 0.4f)]
    [SerializeField] private float _flatEndRatio = 0.2f;

    [Range(0f, 1f)]
    [SerializeField] private float _curvature = 0.85f;


    [Header("Generation")]
    [SerializeField] private int _segmentCount = 4;
    [SerializeField] private int _samplesPerSegment = 12;

    [Header("Mesh")]
    [SerializeField] private float _trackThickness = 2f;
    [SerializeField] private int _meshResolution = 100;
    private SplineContainer _splineContainer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    [ContextMenu("Generate Flow Climb Track")]
    private void Generate()
    {
        CacheComponents();

        List<Vector3> positions = CreateTrackPositions();

        Spline spline = CreateSpline(positions);

        _splineContainer.Spline = spline;

        GenerateRoadMesh(spline);
    }

    private void CacheComponents()
    {
        _splineContainer = GetComponent<SplineContainer>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
    }

    private List<Vector3> CreateTrackPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        for (int segment = 0; segment < _segmentCount; segment++)
        {
            float segmentStartX =
                segment * _length;

            float segmentStartY =
                segment * _height;

            for (int i = 0; i <= _samplesPerSegment; i++)
            {
                if (segment > 0 && i == 0)
                    continue;

                float t =
                    i / (float)_samplesPerSegment;

                float x =
                    segmentStartX
                    + t * _length;

                float height01 =
                    EvaluateTrackHeight(t);

                float y =
                    segmentStartY
                    + height01 * _height;

                positions.Add(
                    new Vector3(
                        x,
                        y,
                        0f
                    )
                );
            }
        }

        return positions;
    }

    private float EvaluateTrackHeight(float t)
    {
        float rampStart =
            _flatStartRatio;

        float rampEnd =
            1f - _flatEndRatio;

        // 시작 직선
        if (t <= rampStart)
        {
            return 0f;
        }

        // 마지막 직선
        if (t >= rampEnd)
        {
            return 1f;
        }

        // 오르막 부분만 0~1로 다시 정규화
        float rampT =
            Mathf.InverseLerp(
                rampStart,
                rampEnd,
                t
            );

        // 직선 경사
        float linearRamp =
            rampT;

        // 부드러운 S자 경사
        float smoothRamp =
            SmootherStep(rampT);

        // Curvature로 두 형태를 혼합
        return Mathf.Lerp(
            linearRamp,
            smoothRamp,
            _curvature
        );
    }

    private float SmootherStep(float t)
    {
        return t * t * t
            * (t * (t * 6f - 15f) + 10f);
    }




    private Spline CreateSpline(
        List<Vector3> positions)
    {
        Spline spline = new Spline();

        float handleScale =
            Mathf.Lerp(
                0.2f,
                0.42f,
                _curvature
            );

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 tangent =
                CalculateTangent(
                    positions,
                    i
                );

            Vector3 roadUp =
                CalculateRoadUp(tangent);

            Quaternion rotation =
                Quaternion.LookRotation(
                    tangent,
                    roadUp
                );

            float tangentInLength =
                CalculateIncomingLength(
                    positions,
                    i,
                    handleScale
                );

            float tangentOutLength =
                CalculateOutgoingLength(
                    positions,
                    i,
                    handleScale
                );

            float3 tangentIn =
                new float3(
                    0f,
                    0f,
                    -tangentInLength
                );

            float3 tangentOut =
                new float3(
                    0f,
                    0f,
                    tangentOutLength
                );

            BezierKnot knot =
                new BezierKnot(
                    ToFloat3(positions[i]),
                    tangentIn,
                    tangentOut,
                    ToQuaternion(rotation)
                );

            spline.Add(
                knot,
                TangentMode.Broken
            );
        }

        spline.Closed = false;

        return spline;
    }

    private Vector3 CalculateTangent(
        List<Vector3> positions,
        int index)
    {
        if (index == 0)
        {
            return (
                positions[1]
                - positions[0]
            ).normalized;
        }

        if (index == positions.Count - 1)
        {
            return (
                positions[index]
                - positions[index - 1]
            ).normalized;
        }

        return (
            positions[index + 1]
            - positions[index - 1]
        ).normalized;
    }

    private Vector3 CalculateRoadUp(
        Vector3 tangent)
    {
        // 코스는 XY 평면을 따라가고
        // 폭은 Z축 방향이므로,
        // Tangent에 수직인 Surface Normal 계산
        Vector3 roadUp =
            Vector3.Cross(
                Vector3.forward,
                tangent
            );

        if (roadUp.sqrMagnitude < 0.001f)
            return Vector3.up;

        return roadUp.normalized;
    }

    private float CalculateIncomingLength(
        List<Vector3> positions,
        int index,
        float handleScale)
    {
        if (index == 0)
            return 0f;

        float distance =
            Vector3.Distance(
                positions[index],
                positions[index - 1]
            );

        return distance * handleScale;
    }

    private float CalculateOutgoingLength(
        List<Vector3> positions,
        int index,
        float handleScale)
    {
        if (index == positions.Count - 1)
            return 0f;

        float distance =
            Vector3.Distance(
                positions[index],
                positions[index + 1]
            );

        return distance * handleScale;
    }

    private void GenerateRoadMesh(Spline spline)
    {
        Mesh mesh = new Mesh
        {
            name = "Flow Climb Track Mesh"
        };

        int sampleCount = Mathf.Max(2, _meshResolution);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);

            bool success = _splineContainer.Evaluate(
                0,
                t,
                out float3 worldPosition,
                out float3 worldTangent,
                out float3 worldUp
            );

            if (!success)
                continue;

            Vector3 position =
                transform.InverseTransformPoint(
                    ToVector3(worldPosition)
                );

            Vector3 tangent =
                transform.InverseTransformDirection(
                    ToVector3(worldTangent)
                ).normalized;

            Vector3 up =
                transform.InverseTransformDirection(
                    ToVector3(worldUp)
                ).normalized;

            Vector3 right =
                Vector3.Cross(
                    up,
                    tangent
                ).normalized;

            // 현재 위치의 폭 계산
            float currentWidth = CalculateWidth(t);

            float halfWidth =
                currentWidth * 0.5f;

            float halfThickness =
                _trackThickness * 0.5f;

            Vector3 topCenter =
                position + up * halfThickness;

            Vector3 bottomCenter =
                position - up * halfThickness;

            Vector3 topLeft =
                topCenter - right * halfWidth;

            Vector3 topRight =
                topCenter + right * halfWidth;

            Vector3 bottomLeft =
                bottomCenter - right * halfWidth;

            Vector3 bottomRight =
                bottomCenter + right * halfWidth;

            vertices.Add(topLeft);
            vertices.Add(topRight);
            vertices.Add(bottomLeft);
            vertices.Add(bottomRight);

            uvs.Add(new Vector2(0f, t));
            uvs.Add(new Vector2(1f, t));
            uvs.Add(new Vector2(0f, t));
            uvs.Add(new Vector2(1f, t));
        }

        for (int i = 0; i < sampleCount - 1; i++)
        {
            int current = i * 4;
            int next = (i + 1) * 4;

            // Top
            AddQuad(
                triangles,
                current,
                next,
                next + 1,
                current + 1
            );

            // Bottom
            AddQuad(
                triangles,
                current + 3,
                next + 3,
                next + 2,
                current + 2
            );

            // Left
            AddQuad(
                triangles,
                current + 2,
                next + 2,
                next,
                current
            );

            // Right
            AddQuad(
                triangles,
                current + 1,
                next + 1,
                next + 3,
                current + 3
            );
        }

        AddStartCap(triangles);
        AddEndCap(triangles, sampleCount);

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        _meshFilter.sharedMesh = mesh;

        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = mesh;
    }

    private float CalculateWidth(float t)
    {
        // -------------------------
        // 아래쪽 시작 끝
        // 좁은 폭 → 기본 폭
        // -------------------------

        if (t < _bottomWidenRatio)
        {
            float widenT = Mathf.InverseLerp(
                0f,
                _bottomWidenRatio,
                t
            );

            widenT = SmootherStep(widenT);

            return Mathf.Lerp(
                _bottomEndWidth,
                _width,
                widenT
            );
        }

        // -------------------------
        // 위쪽 마지막 끝
        // 기본 폭 → 좁은 폭
        // -------------------------

        if (t > _topNarrowStartRatio)
        {
            float narrowT = Mathf.InverseLerp(
                _topNarrowStartRatio,
                1f,
                t
            );

            narrowT = SmootherStep(narrowT);

            return Mathf.Lerp(
                _width,
                _topEndWidth,
                narrowT
            );
        }

        // 가운데는 기본 폭 유지
        return _width;
    }

    private void AddQuad(
    List<int> triangles,
    int a,
    int b,
    int c,
    int d)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);

        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
    }

    private void AddStartCap(
        List<int> triangles)
    {
        // 시작 단면
        AddQuad(
            triangles,
            2,
            0,
            1,
            3
        );
    }

    private void AddEndCap(
        List<int> triangles,
        int sampleCount)
    {
        int start =
            (sampleCount - 1) * 4;

        AddQuad(
            triangles,
            start,
            start + 2,
            start + 3,
            start + 1
        );
    }

    private Vector3 ToVector3(
        float3 value)
    {
        return new Vector3(
            value.x,
            value.y,
            value.z
        );
    }

    private float3 ToFloat3(
        Vector3 value)
    {
        return new float3(
            value.x,
            value.y,
            value.z
        );
    }

    private quaternion ToQuaternion(
        Quaternion value)
    {
        return new quaternion(
            value.x,
            value.y,
            value.z,
            value.w
        );
    }

#if UNITY_EDITOR

    [ContextMenu("Save Generated Mesh")]
    private void SaveGeneratedMesh()
    {
        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        Mesh currentMesh = _meshFilter.sharedMesh;

        if (currentMesh == null)
        {
            Debug.LogWarning("저장할 Mesh가 없습니다. 먼저 Generate를 실행하세요.");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Generated Mesh",
            $"{gameObject.name}_Mesh",
            "asset",
            "생성된 Mesh를 저장할 위치를 선택하세요."
        );

        if (string.IsNullOrEmpty(path))
            return;

        // 현재 임시 Mesh를 복제해서 Asset으로 저장
        Mesh savedMesh = Instantiate(currentMesh);
        savedMesh.name = $"{gameObject.name}_Mesh";

        AssetDatabase.CreateAsset(savedMesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 저장된 Asset을 다시 참조
        _meshFilter.sharedMesh = savedMesh;

        if (_meshCollider != null)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = savedMesh;
        }

        EditorUtility.SetDirty(_meshFilter);

        if (_meshCollider != null)
        {
            EditorUtility.SetDirty(_meshCollider);
        }

        Debug.Log($"Mesh 저장 완료: {path}");
    }

#endif
}

