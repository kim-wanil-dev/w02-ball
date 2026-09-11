using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{

    [Header("Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.1f;

    [SerializeField, Range(0.5f, 1f)]
    private float _groundCheckRadiusRatio = 0.9f;

    [Header("Ball Stat")]
    [SerializeField] private List<BallStat> _ownedBalls;
    [SerializeField] private int _currentBallNum;

    [Header("Gravity"), Range(-100f, 0f)]
    [SerializeField] private float _diveAcceleration = -20f;

    [Header("Haptic")]
    [SerializeField] private HapticManager _hapticManager;

    [SerializeField] private float _groundHapticInterval = 0.1f;
    [SerializeField] private float _fallHapticInterval = 0.12f;

    [SerializeField] private float _minHapticSpeed = 10f;
    [SerializeField] private float _minFallSpeed = 40f;

    private Text _velocityText;
    private Text _heightText;

    private Transform _cameraTransform;
    private Vector2 _moveInput;
    private float _previousSizeChangeInput;
    private float _diveInput;

    private Vector3 _gravityDir = Vector3.down;
    private Vector3 _groundNormal = Vector3.up;
    private bool _hasGroundContact;
    private Vector3 _contactGroundNormal = Vector3.up;

    private Rigidbody _rb;

    private SphereCollider _collider;
    private PhysicsMaterial _physicsMaterial;

    private float _moveSpeed;
    private float _moveAcceleration;
    private float _moveResponseTime;
    private float _jumpForce;
    private float _maxGravityVelocity;

    private bool _jumpRequested;

    private float _hapticTimer = 0f;

    public bool IsGrounded { get; private set; }
    public List<BallStat> OwnedBalls { get { return _ownedBalls; } }
    public int CurrentBallNum { get { return _currentBallNum; } }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;

        _collider = GetComponent<SphereCollider>();
        _physicsMaterial = _collider.material;

        _hapticManager = GetComponent<HapticManager>();

        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        InitSizeBall(_ownedBalls[_currentBallNum]);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Instantiate(Resources.Load<GameObject>($"Prefabs/EventSystem"));
        GameObject DebugCanvas = Instantiate(Resources.Load<GameObject>($"Prefabs/DebugCanvas"));
        _velocityText = DebugCanvas.transform.Find("VelocityText").GetComponent<Text>();
        _heightText = DebugCanvas.transform.Find("HeightText").GetComponent<Text>();
    }

    private void Update()
    {
        ProcessMoveInput();
        ProcessResizeInput();
        ProcessJumpInput();
        ProcessDiveInput();
    }

    private void ProcessMoveInput()
    {
        _moveInput = GameInputController.Instance.MoveInput;
    }

    private void ProcessResizeInput()
    {
        int currentSizeChangeInput = (int)GameInputController.Instance.ResizeInput;

        if (_previousSizeChangeInput == currentSizeChangeInput)
            return;

        _previousSizeChangeInput = currentSizeChangeInput;

        if (currentSizeChangeInput == 0)
            return;

        int nextBallNum = Mathf.Clamp(currentSizeChangeInput + _currentBallNum, 0, _ownedBalls.Count - 1);

        if (nextBallNum == _currentBallNum)
            return;

        ResizeBall(_currentBallNum, nextBallNum);
        _currentBallNum = nextBallNum;

    }

    private void ProcessJumpInput()
    {
        if (GameInputController.Instance.JumpPressed && IsGrounded)
            _jumpRequested = true;
    }

    private void ProcessDiveInput()
    {
        _diveInput = GameInputController.Instance.DiveInput;
    }

    private void FixedUpdate()
    {
        CheckGround();
        ProcessJump();
        ApplyMovement();

        HapticControl();

        ClampGravityVelocity();

        _velocityText.text = $"{_rb.linearVelocity.magnitude:F2} m/s";
        _heightText.text = $"{_rb.transform.position.y:F2} m";
        Debug.Log($"Ground: {IsGrounded}\n Ground Normal: {_groundNormal}");
    }

    private void CheckGround()
    {
        Vector3 gravityDirection = _gravityDir;

        float sphereRadius =
            _ownedBalls[_currentBallNum].SphereRadius;

        float checkRadius =
            sphereRadius * _groundCheckRadiusRatio;

        float castDistance = sphereRadius - checkRadius +
            _groundCheckDistance;

        if (Physics.SphereCast(
            transform.position,
            checkRadius,
            gravityDirection,
            out RaycastHit hit,
            castDistance,
            _groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            IsGrounded = true;
            _groundNormal = hit.normal;
        }
        else if (_hasGroundContact)
        {
            IsGrounded = true;
            _groundNormal = _contactGroundNormal;
        }
        else
        {
            IsGrounded = false;
            _groundNormal = -gravityDirection;
        }

        _hasGroundContact = false;
    }

    private void ProcessJump()
    {
        if (!_jumpRequested)
            return;

        _jumpRequested = false;

        if (!IsGrounded)
            return;

        Vector3 jumpDirection =
            -_gravityDir;

        _rb.AddForce(
            jumpDirection *
            _jumpForce,
            ForceMode.Impulse
        );
    }

    private void ApplyMovement()
    {
        Vector3 worldMoveInput = GetWorldMoveInput(_moveInput);

        if (IsGrounded)
            Roll(worldMoveInput);
        else
            AirMove(worldMoveInput);
    }

    private Vector3 GetWorldMoveInput(Vector2 input)
    {
        Vector3 upDirection = -_gravityDir;

        Vector3 cameraForward =
            Vector3.ProjectOnPlane(_cameraTransform.forward, upDirection);

        if (cameraForward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        cameraForward.Normalize();

        Vector3 cameraRight =
            Vector3.Cross(upDirection, cameraForward).normalized;

        Vector3 worldMoveInput =
            cameraForward * input.y +
            cameraRight * input.x;

        return Vector3.ClampMagnitude(worldMoveInput, 1f);
    }

    private void Roll(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection =
            GetGroundMoveDirection(worldMoveInput);

        UpdateGroundMoveVelocity(
            groundMoveDirection,
            worldMoveInput.magnitude
        );
    }

    private Vector3 GetGroundMoveDirection(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection =
            Vector3.ProjectOnPlane(worldMoveInput, _groundNormal);

        if (groundMoveDirection.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return groundMoveDirection.normalized;
    }

    private void UpdateGroundMoveVelocity(
        Vector3 groundMoveDirection,
        float inputMagnitude)
    {
        inputMagnitude = Mathf.Clamp01(inputMagnitude);

        if (inputMagnitude <= 0.001f)
            return;

        Vector3 targetVelocity =
            groundMoveDirection *
            _moveSpeed *
            inputMagnitude;

        Vector3 currentVelocity =
            Vector3.ProjectOnPlane(
                _rb.linearVelocity,
                _groundNormal
            );

        Vector3 velocityDelta =
            targetVelocity - currentVelocity;

        Vector3 responseAcceleration =
            velocityDelta / _moveResponseTime;

        Vector3 velocityChange =
            responseAcceleration * Time.fixedDeltaTime;

        velocityChange =
            Vector3.ClampMagnitude(
                velocityChange,
                velocityDelta.magnitude
            );

        Vector3 acceleration =
            velocityChange / Time.fixedDeltaTime;

        _rb.AddForce(
            acceleration,
            ForceMode.Acceleration
        );
    }

    private void AirMove(Vector3 worldMoveInput)
    {
        UpdateAirMoveVelocity(worldMoveInput);
        ApplyDiveGravity();
    }

    private void UpdateAirMoveVelocity(Vector3 worldMoveInput)
    {
        float inputMagnitude =
            Mathf.Clamp01(worldMoveInput.magnitude);

        if (inputMagnitude <= 0.001f)
            return;

        Vector3 moveDirection =
            worldMoveInput.normalized;

        float targetSpeed =
            _moveSpeed *
            inputMagnitude;

        Vector3 planarVelocity =
            Vector3.ProjectOnPlane(
                _rb.linearVelocity,
                _gravityDir
            );

        float currentSpeed =
            Vector3.Dot(
                planarVelocity,
                moveDirection
            );

        if (currentSpeed >= targetSpeed)
            return;

        float remainingSpeed =
            targetSpeed - currentSpeed;

        float acceleration =
           _moveAcceleration *
            inputMagnitude;

        float velocityChange = Mathf.Min(
            acceleration * Time.fixedDeltaTime,
            remainingSpeed
        );

        acceleration =
            velocityChange / Time.fixedDeltaTime;

        _rb.AddForce(
            moveDirection * acceleration,
            ForceMode.Acceleration
        );
    }

    private void ApplyDiveGravity()
    {
        if (_diveInput <= 0f)
            return;

        Vector3 diveAcceleration =
            _gravityDir * _diveAcceleration * _diveInput;

        _rb.AddForce(
            diveAcceleration,
            ForceMode.Acceleration
        );
    }

    private void ClampGravityVelocity()
    {
        float gravitySpeed =
            Vector3.Dot(
                _rb.linearVelocity,
                _gravityDir
            );
        if (gravitySpeed <= _maxGravityVelocity)
            return;
        Vector3 excessVelocity = _gravityDir *
            (gravitySpeed - _maxGravityVelocity);
        _rb.linearVelocity -= excessVelocity;
    }

    public void InitSizeBall(BallStat inputBallStat)
    {
        transform.localScale = 2 * Vector3.one *
            inputBallStat.SphereRadius;

        _moveSpeed = inputBallStat.MoveSpeed;
        _moveAcceleration = inputBallStat.MoveAcceleration;
        _moveResponseTime = inputBallStat.MoveResponseTime;
        _jumpForce = inputBallStat.JumpForce;
        _maxGravityVelocity = inputBallStat.MaxGravityVelocity;

        _physicsMaterial.bounciness =
            inputBallStat.Bounciness;

        _rb.mass = inputBallStat.Mass;
    }

    public void ResizeBall(int currentBallNum, int nextBallNum)
    {
        BallStat currentBallStat = _ownedBalls[currentBallNum];
        BallStat nextBallStat = _ownedBalls[nextBallNum];

        Vector3 currentVelocity = _rb.linearVelocity;
        float velocityRatio = Mathf.Sqrt(
            currentBallStat.Mass / nextBallStat.Mass
        );

        InitSizeBall(nextBallStat);

        _rb.linearVelocity = currentVelocity * velocityRatio;
    }

    private void HapticControl()
    {
        _hapticTimer -= Time.fixedDeltaTime;

        if (_hapticTimer > 0f)
            return;

        if (IsGrounded)
        {
            GroundHaptic();
        }
        else
        {
            AirHaptic();
        }
    }

    private void GroundHaptic()
    {
        float speed = _rb.linearVelocity.magnitude;

        if (speed < _minHapticSpeed)
            return;

        float speed01 = Mathf.InverseLerp(
            _minHapticSpeed,
            _ownedBalls[_currentBallNum].MoveSpeed,
            speed
        );

        float mass = _ownedBalls[CurrentBallNum].Mass;

        float lowFrequency =
            Mathf.Clamp01(speed01 * 0.4f * mass);

        float highFrequency =
            Mathf.Clamp01(speed01 * 0.25f / mass);

        _hapticManager.Vibrate(
            lowFrequency,
            highFrequency,
            0.05f
        );

        _hapticTimer = _groundHapticInterval;
    }

    private void AirHaptic()
    {
        float fallSpeed = -_rb.linearVelocity.y;

        if (fallSpeed < _minFallSpeed)
            return;

        float fall01 = Mathf.InverseLerp(
            _minFallSpeed,
            _ownedBalls[_currentBallNum].MaxGravityVelocity,
            fallSpeed
        );

        _hapticManager.Vibrate(
            fall01 * 0.15f,
            fall01 * 0.35f,
            0.04f
        );

        _hapticTimer = _fallHapticInterval;
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((_groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        Vector3 upDirection = -_gravityDir;

        float bestDot = -1f;
        Vector3 bestNormal = upDirection;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            float dot = Vector3.Dot(normal, upDirection);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestNormal = normal;
            }
        }

        if (bestDot <= 0f)
            return;

        _hasGroundContact = true;
        _contactGroundNormal = bestNormal;
    }
}
