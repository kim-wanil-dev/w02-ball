using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class RollerCoasterSplineGenerator : MonoBehaviour
{
    [Header("Start")]
    [SerializeField]
    private Vector3 _startPosition =
        new Vector3(0f, 165f, 0f);

    [Header("Descent")]
    [SerializeField]
    private Vector3 _descentMiddlePosition =
        new Vector3(40f, 120f, 0f);

    [SerializeField]
    private Vector3 _descentApproachPosition =
        new Vector3(90f, 20f, 0f);

    [Header("Loop")]
    [SerializeField]
    private Vector3 _loopBottomPosition =
        new Vector3(120f, 0f, 0f);

    // 루프 전체 높이
    [SerializeField] private float _loopHeight = 100f;

    // 아래쪽 루프 너비
    [SerializeField] private float _loopBottomHalfWidth = 55f;

    // 위쪽으로 갈수록 좁아지는 정도
    [SerializeField] private float _loopTopHalfWidth = 32f;

    // 루프를 한 바퀴 도는 동안 Z축으로 벌어지는 거리
    [SerializeField] private float _loopSideOffset = 20f;

    [SerializeField] private int _loopSegments = 20;

    [Header("Exit")]
    [SerializeField] private float _exitLength = 80f;

    [Header("Spline")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float _handleScale = 0.33f;

    private SplineContainer _splineContainer;

    private void Awake()
    {
        _splineContainer = GetComponent<SplineContainer>();
    }

    [ContextMenu("Generate Roller Coaster Spline")]
    private void GenerateSpline()
    {
        _splineContainer = GetComponent<SplineContainer>();

        List<Vector3> positions = new List<Vector3>();

        AddDescentPositions(positions);
        AddLoopPositions(positions);
        AddExitPositions(positions);

        Spline spline = CreateSpline(positions);

        spline.Closed = false;

        _splineContainer.Spline = spline;
    }

    private void AddDescentPositions(List<Vector3> positions)
    {
        positions.Add(_startPosition);
        positions.Add(_descentMiddlePosition);
        positions.Add(_descentApproachPosition);
    }

    private void AddLoopPositions(List<Vector3> positions)
    {
        for (int i = 0; i <= _loopSegments; i++)
        {
            float progress =
                i / (float)_loopSegments;

            float angle =
                Mathf.Lerp(-90f, 270f, progress);

            float angleRad =
                angle * Mathf.Deg2Rad;

            // 0 = 루프 가장 아래
            // 1 = 루프 가장 위
            float height01 =
                (Mathf.Sin(angleRad) + 1f) * 0.5f;

            // 위로 갈수록 루프 폭이 좁아진다.
            float taper =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    height01
                );

            float halfWidth =
                Mathf.Lerp(
                    _loopBottomHalfWidth,
                    _loopTopHalfWidth,
                    taper
                );

            float x =
                _loopBottomPosition.x
                + Mathf.Cos(angleRad) * halfWidth;

            float y =
                _loopBottomPosition.y
                + height01 * _loopHeight;

            float z =
                _loopBottomPosition.z
                + _loopSideOffset * progress;

            Vector3 position =
                new Vector3(x, y, z);

            positions.Add(position);
        }
    }

    private void AddExitPositions(List<Vector3> positions)
    {
        Vector3 loopExitPosition =
            _loopBottomPosition
            + Vector3.forward * _loopSideOffset;

        Vector3 exitMiddlePosition =
            loopExitPosition
            + Vector3.right * (_exitLength * 0.5f);

        Vector3 exitPosition =
            loopExitPosition
            + Vector3.right * _exitLength;

        positions.Add(exitMiddlePosition);
        positions.Add(exitPosition);
    }

    private Spline CreateSpline(List<Vector3> positions)
    {
        Spline spline = new Spline();

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 tangent =
                CalculateTangent(positions, i);

            Vector3 up =
                CalculateUpVector(tangent);

            Quaternion rotation =
                Quaternion.LookRotation(
                    tangent,
                    up
                );

            float tangentInLength =
                CalculateTangentInLength(
                    positions,
                    i
                );

            float tangentOutLength =
                CalculateTangentOutLength(
                    positions,
                    i
                );

            // Tangent은 Knot Rotation 기준 Local Z축
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

        Vector3 previousDirection =
            (
                positions[index]
                - positions[index - 1]
            ).normalized;

        Vector3 nextDirection =
            (
                positions[index + 1]
                - positions[index]
            ).normalized;

        Vector3 tangent =
            (
                previousDirection
                + nextDirection
            ).normalized;

        return tangent;
    }

    private Vector3 CalculateUpVector(
        Vector3 tangent)
    {
        // 진행 방향의 왼쪽 Normal.
        //
        // 루프 아래:
        // Tangent = →
        // Up      = ↑
        //
        // 루프 위:
        // Tangent = ←
        // Up      = ↓

        Vector3 up =
            new Vector3(
                -tangent.y,
                tangent.x,
                0f
            );

        if (up.sqrMagnitude < 0.001f)
        {
            return Vector3.up;
        }

        return up.normalized;
    }

    private float CalculateTangentInLength(
        List<Vector3> positions,
        int index)
    {
        if (index == 0)
            return 0f;

        float distance =
            Vector3.Distance(
                positions[index],
                positions[index - 1]
            );

        return distance * _handleScale;
    }

    private float CalculateTangentOutLength(
        List<Vector3> positions,
        int index)
    {
        if (index == positions.Count - 1)
            return 0f;

        float distance =
            Vector3.Distance(
                positions[index],
                positions[index + 1]
            );

        return distance * _handleScale;
    }

    private float3 ToFloat3(Vector3 vector)
    {
        return new float3(
            vector.x,
            vector.y,
            vector.z
        );
    }

    private quaternion ToQuaternion(
        Quaternion rotation)
    {
        return new quaternion(
            rotation.x,
            rotation.y,
            rotation.z,
            rotation.w
        );
    }
}