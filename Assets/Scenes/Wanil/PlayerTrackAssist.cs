using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class PlayerTrackAssist : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private SplineContainer _trackSpline;

    [Header("Rail Detection")]
    [SerializeField] private LayerMask _railLayerMask;
    [SerializeField] private float _railCheckOffset = 1f;
    [SerializeField] private float _groundCheckRadius = 0.3f;
    [SerializeField] private float _groundCheckDistance = 1.5f;

    [Header("Ground Grace")]
    [SerializeField] private float _groundGraceTime = 0.2f;

    [Header("Spline Search")]
    [SerializeField] private int _nearestPointResolution = 4;
    [SerializeField] private int _nearestPointIterations = 2;

    [Header("Track Assist")]
    [SerializeField] private float _adhesionForce = 15f;
    [SerializeField] private float _lateralCorrection = 4f;
    [SerializeField] private float _forwardAcceleration = 8f;
    [SerializeField] private float _maxTrackSpeed = 40f;

    private Vector3 _lastGroundNormal;
    private float _timeSinceRailContact;
    private bool _hasRailContact;
    private bool _isOnTrack;

    private void Awake()
    {
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody>();
        }
    }

    private void FixedUpdate()
    {
        if (!TryGetTrackInfo(
                out Vector3 trackPosition,
                out Vector3 trackForward,
                out Vector3 trackUp,
                out Vector3 groundNormal))
        {
            _isOnTrack = false;
            return;
        }

        _isOnTrack = true;

        ApplyAdhesion(groundNormal);
        ApplyLateralCorrection(trackForward, trackUp);
        ApplyForwardForce(trackForward);
    }

    private bool TryGetTrackInfo(
        out Vector3 trackPosition,
        out Vector3 trackForward,
        out Vector3 trackUp,
        out Vector3 groundNormal)
    {
        trackPosition = Vector3.zero;
        trackForward = Vector3.forward;
        trackUp = Vector3.up;
        groundNormal = Vector3.up;

        if (_trackSpline == null || _rb == null)
            return false;

        // 플레이어 위치를 Spline의 Local Space로 변환
        Vector3 localPlayerPosition =
            _trackSpline.transform.InverseTransformPoint(
                _rb.worldCenterOfMass
            );

        // 가장 가까운 Spline 위치 탐색
        SplineUtility.GetNearestPoint(
            _trackSpline.Spline,
            ToFloat3(localPlayerPosition),
            out _,
            out float splineT,
            _nearestPointResolution,
            _nearestPointIterations
        );

        // 해당 위치의 Position / Tangent / Up 가져오기
        bool evaluated = _trackSpline.Evaluate(
            0,
            splineT,
            out float3 position,
            out float3 tangent,
            out float3 upVector
        );

        if (!evaluated)
            return false;

        trackPosition = ToVector3(position);
        trackForward = ToVector3(tangent).normalized;
        trackUp = ToVector3(upVector).normalized;

        if (trackForward.sqrMagnitude < 0.001f)
            return false;

        if (trackUp.sqrMagnitude < 0.001f)
            return false;

        // Forward와 Up을 정확히 직각으로 보정
        trackUp = Vector3.ProjectOnPlane(
            trackUp,
            trackForward
        ).normalized;

        Vector3 trackRight = Vector3.Cross(
            trackUp,
            trackForward
        ).normalized;

        // 두 레일 위쪽에서 각각 SphereCast
        Vector3 center = _rb.worldCenterOfMass;

        Vector3 leftOrigin =
            center - trackRight * _railCheckOffset;

        Vector3 rightOrigin =
            center + trackRight * _railCheckOffset;

        // 루프 어느 위치에 있더라도 레일 쪽으로 검사
        Vector3 castDirection = -trackUp;

        bool leftHit = Physics.SphereCast(
            leftOrigin,
            _groundCheckRadius,
            castDirection,
            out RaycastHit leftHitInfo,
            _groundCheckDistance,
            _railLayerMask,
            QueryTriggerInteraction.Ignore
        );

        bool rightHit = Physics.SphereCast(
            rightOrigin,
            _groundCheckRadius,
            castDirection,
            out RaycastHit rightHitInfo,
            _groundCheckDistance,
            _railLayerMask,
            QueryTriggerInteraction.Ignore
        );

        // 실제 레일 접촉
        if (leftHit || rightHit)
        {
            if (leftHit && rightHit)
            {
                groundNormal =
                    (leftHitInfo.normal + rightHitInfo.normal).normalized;
            }
            else if (leftHit)
            {
                groundNormal = leftHitInfo.normal;
            }
            else
            {
                groundNormal = rightHitInfo.normal;
            }

            _lastGroundNormal = groundNormal;

            _timeSinceRailContact = 0f;
            _hasRailContact = true;

            return true;
        }

        // 레일 감지가 끊긴 경우
        _timeSinceRailContact += Time.fixedDeltaTime;

        // 이전에 접촉한 적이 있고,
        // 0.2초 이내라면 마지막 Ground Normal을 계속 사용
        if (_hasRailContact &&
            _timeSinceRailContact <= _groundGraceTime)
        {
            groundNormal = _lastGroundNormal;

            return true;
        }

        _hasRailContact = false;

        return false;
    }

    private void ApplyAdhesion(Vector3 groundNormal)
    {
        float speed = _rb.linearVelocity.magnitude;

        float speedMultiplier = Mathf.InverseLerp(
            0f,
            30f,
            speed
        );

        float adhesionForce =
            _adhesionForce * speedMultiplier;

        _rb.AddForce(
            -groundNormal * adhesionForce,
            ForceMode.Acceleration
        );
    }

    private void ApplyLateralCorrection(
        Vector3 trackForward,
        Vector3 trackUp)
    {
        Vector3 trackRight = Vector3.Cross(
            trackUp,
            trackForward
        ).normalized;

        float lateralSpeed = Vector3.Dot(
            _rb.linearVelocity,
            trackRight
        );

        Vector3 correctionForce =
            -trackRight
            * lateralSpeed
            * _lateralCorrection;

        _rb.AddForce(
            correctionForce,
            ForceMode.Acceleration
        );
    }

    private void ApplyForwardForce(Vector3 trackForward)
    {
        float forwardSpeed = Vector3.Dot(
            _rb.linearVelocity,
            trackForward
        );

        if (forwardSpeed >= _maxTrackSpeed)
            return;

        _rb.AddForce(
            trackForward * _forwardAcceleration,
            ForceMode.Acceleration
        );
    }

    private float3 ToFloat3(Vector3 value)
    {
        return new float3(
            value.x,
            value.y,
            value.z
        );
    }

    private Vector3 ToVector3(float3 value)
    {
        return new Vector3(
            value.x,
            value.y,
            value.z
        );
    }
}