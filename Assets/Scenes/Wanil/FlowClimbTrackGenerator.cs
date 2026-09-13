using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Splines.ExtrusionShapes;

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

    [Header("Shape")]
    [Range(0f, 1f)]
    [SerializeField] private float _curvature = 0.85f;

    [Range(0f, 0.5f)]
    [SerializeField] private float _accelerationDipRatio = 0.18f;

    [Range(0.1f, 0.6f)]
    [SerializeField] private float _dipPortion = 0.35f;

    [Header("Generation")]
    [SerializeField] private int _segmentCount = 4;
    [SerializeField] private int _samplesPerSegment = 8;

    [SerializeField] private float _meshSegmentsPerUnit = 0.5f;

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
            float segmentStartX = segment * _length;
            float segmentStartY = segment * _height;

            for (int i = 0; i <= _samplesPerSegment; i++)
            {
                // 이전 Segment의 끝점과
                // 다음 Segment의 시작점 중복 방지
                if (segment > 0 && i == 0)
                    continue;

                float t =
                    i / (float)_samplesPerSegment;

                float x =
                    segmentStartX
                    + t * _length;

                float rise =
                    EvaluateRise(t);

                float dip =
                    EvaluateAccelerationDip(t);

                float y =
                    segmentStartY
                    + rise * _height
                    + dip;

                Vector3 position =
                    new Vector3(
                        x,
                        y,
                        0f
                    );

                positions.Add(position);
            }
        }

        return positions;
    }

    private float EvaluateRise(float t)
    {
        // Quintic SmootherStep
        // 시작과 끝의 기울기가 자연스럽게 0에 가까워짐
        float smoothT =
            t * t * t
            * (t * (t * 6f - 15f) + 10f);

        // Curvature 0
        // -> 직선에 가까움
        //
        // Curvature 1
        // -> 시작 / 끝이 매우 부드러운 S Curve
        return Mathf.Lerp(
            t,
            smoothT,
            _curvature
        );
    }

    private float EvaluateAccelerationDip(float t)
    {
        if (t >= _dipPortion)
            return 0f;

        float dipT =
            t / _dipPortion;

        float wave =
            Mathf.Sin(
                dipT * Mathf.PI
            );

        wave *= wave;

        float dipDepth =
            _height
            * _accelerationDipRatio;

        return -wave * dipDepth;
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

    private void GenerateRoadMesh(
        Spline spline)
    {
        Mesh mesh = _meshFilter.sharedMesh;

        if (mesh == null ||
            mesh.name != "Flow Climb Road Mesh")
        {
            mesh = new Mesh
            {
                name = "Flow Climb Road Mesh"
            };

            _meshFilter.sharedMesh = mesh;
        }
        else
        {
            mesh.Clear();
        }

        float approximateLength =
            _length * _segmentCount;

        int meshSegments =
            Mathf.Max(
                16,
                Mathf.CeilToInt(
                    approximateLength
                    * _meshSegmentsPerUnit
                )
            );

        Road roadShape = new Road();

        SplineMesh.Extrude(
            spline,
            mesh,
            _width * 0.5f,
            meshSegments,
            true,
            roadShape
        );

        mesh.RecalculateBounds();

        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = mesh;
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
}