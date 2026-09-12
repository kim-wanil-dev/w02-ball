using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.1f;
    [SerializeField, Range(0.5f, 1f)] private float _groundCheckRadiusRatio = 0.9f;
    [SerializeField, Range(0f, 90f)] private float _maxGroundAngle = 50f;

    [Header("Ball Stat")]
    [SerializeField] private BallStat _smallBall;
    [SerializeField] private BallStat _largeBall;

    [Header("Resize")]
    [SerializeField] private float _resizeSpeed = 1f;
    [SerializeField] private float _shrinkUpwardVelocityBoost = 10f;

    [Header("Gravity")]
    [SerializeField, Range(0f, 100f)] private float _diveAcceleration = 20f;

    private Rigidbody _rb;
    private SphereCollider _collider;
    private PhysicsMaterial _physicsMaterial;
    private Transform _cameraTransform;
    private HapticManager _hapticManager;
    private Text _velocityText;
    private Text _heightText;

    private Vector2 _moveInput;
    private float _resizeInput;
    private bool _jumpRequested;
    private bool _diveInput;

    private float _currentSizeRatio;

    private Vector3 _gravityDir = Vector3.down;
    private Vector3 _groundNormal = Vector3.up;
    private bool _hasGroundContact;
    private Vector3 _contactGroundNormal = Vector3.up;

    public bool IsGrounded { get; private set; }
    public float CurrentSizeRatio => _currentSizeRatio;
    public BallStat SmallBall => _smallBall;
    public BallStat LargeBall => _largeBall;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;

        _collider = GetComponent<SphereCollider>();
        _physicsMaterial = _collider.material;
        _hapticManager = GetComponent<HapticManager>();

        _cameraTransform = Camera.main.transform;

        _currentSizeRatio = Mathf.Clamp01(_smallBall.SizeRatio);
        ApplyCurrentSizeStat();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Instantiate(Resources.Load<GameObject>("Prefabs/EventSystem"));

        GameObject debugCanvas = Instantiate(Resources.Load<GameObject>("Prefabs/DebugCanvas"));
        _velocityText = debugCanvas.transform.Find("VelocityText").GetComponent<Text>();
        _heightText = debugCanvas.transform.Find("HeightText").GetComponent<Text>();
    }

    private void Update()
    {
        ProcessMoveInput();
        ProcessResizeInput();
        ProcessJumpInput();
        ProcessDiveInput();
    }

    private void FixedUpdate()
    {
        ProcessResize();
        CheckGround();
        ProcessJump();
        ApplyMovement();
        ClampGravityVelocity();

        _velocityText.text = $"{_rb.linearVelocity.magnitude:F2} m/s";
        _heightText.text = $"{transform.position.y:F2} m";

        Debug.Log($"Ground: {IsGrounded}\n Ground Normal: {_groundNormal}");
    }

    private void OnDisable()
    {
        _hapticManager.StopHaptic();
    }

    private void ProcessMoveInput()
    {
        _moveInput = GameInputController.Instance.MoveInput;
    }

    private void ProcessResizeInput()
    {
        _resizeInput = Mathf.Clamp(GameInputController.Instance.ResizeInput, -1f, 1f);
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

    private void ProcessResize()
    {
        float curvedInput = GetEaseOutSineInput(_resizeInput);

        if (Mathf.Abs(curvedInput) <= Mathf.Epsilon)
        {
            _hapticManager.StopHaptic();
            return;
        }

        float previousRatio = _currentSizeRatio;
        float nextRatio = Mathf.Clamp01(previousRatio + curvedInput * _resizeSpeed * Time.fixedDeltaTime);

        if (Mathf.Approximately(previousRatio, nextRatio))
        {
            _hapticManager.StopHaptic();
            return;
        }

        float previousMass = GetStat(previousRatio, stat => stat.Mass);
        Vector3 previousVelocity = _rb.linearVelocity;
        float previousRadius = _collider.radius * transform.lossyScale.x;

        _currentSizeRatio = nextRatio;
        ApplyCurrentSizeStat();

        float currentRadius = _collider.radius * transform.lossyScale.x;
        float radiusDelta = currentRadius - previousRadius;
        float currentMass = GetStat(_currentSizeRatio, stat => stat.Mass);

        _rb.linearVelocity = previousVelocity * Mathf.Sqrt(previousMass / currentMass);

        if (_currentSizeRatio > previousRatio)
        {
            _rb.position += -_gravityDir * radiusDelta * 2f;

            float downwardSpeed = Vector3.Dot(_rb.linearVelocity, _gravityDir);

            if (downwardSpeed > 0f)
                _rb.linearVelocity -= _gravityDir * downwardSpeed;
        }
        else
        {
            if (IsGrounded)
                _rb.position += _groundNormal * radiusDelta;

            float upwardSpeed = Vector3.Dot(previousVelocity, -_gravityDir);

            if (!IsGrounded && upwardSpeed > 0f)
            {
                float previousShrinkProgress = 1f - previousRatio;
                float currentShrinkProgress = 1f - _currentSizeRatio;

                float previousBoostRatio = previousShrinkProgress * previousShrinkProgress * previousShrinkProgress;
                float currentBoostRatio = currentShrinkProgress * currentShrinkProgress * currentShrinkProgress;

                float boostDelta = currentBoostRatio - previousBoostRatio;

                _rb.linearVelocity += -_gravityDir * (_shrinkUpwardVelocityBoost * boostDelta);
            }
        }

        float hapticIntensity = Mathf.Lerp(0.05f, 0.1f, _currentSizeRatio);
        _hapticManager.HapticControl(hapticIntensity);
    }

    private float GetEaseOutSineInput(float input)
    {
        float magnitude = Mathf.Abs(input);
        float curvedMagnitude = Mathf.Sin(magnitude * Mathf.PI * 0.5f);

        return Mathf.Sign(input) * curvedMagnitude;
    }

    private float GetStat(float ratio, Func<BallStat, float> selector)
    {
        return Mathf.Lerp(selector(_smallBall), selector(_largeBall), ratio);
    }

    private void ApplyCurrentSizeStat()
    {
        float scale = GetStat(_currentSizeRatio, stat => stat.Scale);
        float mass = GetStat(_currentSizeRatio, stat => stat.Mass);
        float bounciness = GetStat(_currentSizeRatio, stat => stat.Bounciness);

        transform.localScale = Vector3.one * scale;
        _rb.mass = mass;
        _physicsMaterial.bounciness = bounciness;
    }

    private void CheckGround()
    {
        float sphereRadius = _collider.radius * transform.lossyScale.x;
        float checkRadius = sphereRadius * _groundCheckRadiusRatio;
        float castDistance = sphereRadius - checkRadius + _groundCheckDistance;

        if (Physics.SphereCast(
            transform.position,
            checkRadius,
            _gravityDir,
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
            _groundNormal = -_gravityDir;
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

        float jumpForce = GetStat(_currentSizeRatio, stat => stat.JumpForce);
        _rb.AddForce(-_gravityDir * jumpForce, ForceMode.Impulse);
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
        Vector3 cameraForward = Vector3.ProjectOnPlane(_cameraTransform.forward, upDirection);

        if (cameraForward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        cameraForward.Normalize();

        Vector3 cameraRight = Vector3.Cross(upDirection, cameraForward).normalized;
        Vector3 worldMoveInput = cameraForward * input.y + cameraRight * input.x;

        return Vector3.ClampMagnitude(worldMoveInput, 1f);
    }

    private void Roll(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection = GetGroundMoveDirection(worldMoveInput);
        UpdateGroundMoveVelocity(groundMoveDirection, worldMoveInput.magnitude);
    }

    private Vector3 GetGroundMoveDirection(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection = Vector3.ProjectOnPlane(worldMoveInput, _groundNormal);

        if (groundMoveDirection.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return groundMoveDirection.normalized;
    }

    private void UpdateGroundMoveVelocity(Vector3 groundMoveDirection, float inputMagnitude)
    {
        inputMagnitude = Mathf.Clamp01(inputMagnitude);

        if (inputMagnitude <= 0.001f)
            return;

        float moveSpeed = GetStat(_currentSizeRatio, stat => stat.MoveSpeed);
        float moveResponseTime = GetStat(_currentSizeRatio, stat => stat.MoveResponseTime);

        Vector3 targetVelocity = groundMoveDirection * moveSpeed * inputMagnitude;
        Vector3 currentVelocity = Vector3.ProjectOnPlane(_rb.linearVelocity, _groundNormal);
        Vector3 velocityDelta = targetVelocity - currentVelocity;

        Vector3 responseAcceleration = velocityDelta / moveResponseTime;
        Vector3 velocityChange = responseAcceleration * Time.fixedDeltaTime;
        velocityChange = Vector3.ClampMagnitude(velocityChange, velocityDelta.magnitude);

        Vector3 acceleration = velocityChange / Time.fixedDeltaTime;
        _rb.AddForce(acceleration, ForceMode.Acceleration);
    }

    private void AirMove(Vector3 worldMoveInput)
    {
        UpdateAirMoveVelocity(worldMoveInput);
        ApplyDiveGravity();
    }

    private void UpdateAirMoveVelocity(Vector3 worldMoveInput)
    {
        float inputMagnitude = Mathf.Clamp01(worldMoveInput.magnitude);

        if (inputMagnitude <= 0.001f)
            return;

        float moveSpeed = GetStat(_currentSizeRatio, stat => stat.MoveSpeed);
        float moveAcceleration = GetStat(_currentSizeRatio, stat => stat.MoveAcceleration);

        Vector3 moveDirection = worldMoveInput.normalized;
        float targetSpeed = moveSpeed * inputMagnitude;

        Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.linearVelocity, _gravityDir);
        float currentSpeed = Vector3.Dot(planarVelocity, moveDirection);

        if (currentSpeed >= targetSpeed)
            return;

        float remainingSpeed = targetSpeed - currentSpeed;
        float acceleration = moveAcceleration * inputMagnitude;
        float velocityChange = Mathf.Min(acceleration * Time.fixedDeltaTime, remainingSpeed);

        acceleration = velocityChange / Time.fixedDeltaTime;
        _rb.AddForce(moveDirection * acceleration, ForceMode.Acceleration);
    }

    private void ApplyDiveGravity()
    {
        if (!_diveInput)
            return;

        _rb.AddForce(_gravityDir * _diveAcceleration, ForceMode.Acceleration);
    }

    private void ClampGravityVelocity()
    {
        float maxGravityVelocity = GetStat(_currentSizeRatio, stat => stat.MaxGravityVelocity);
        float gravitySpeed = Vector3.Dot(_rb.linearVelocity, _gravityDir);

        if (gravitySpeed <= maxGravityVelocity)
            return;

        Vector3 excessVelocity = _gravityDir * (gravitySpeed - maxGravityVelocity);
        _rb.linearVelocity -= excessVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float intensity = Mathf.InverseLerp(10f, 100f, collision.impulse.magnitude);
        _hapticManager.HapticControl(intensity, 0f, 0.15f);
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((_groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        float bestGroundDot = Mathf.Cos(_maxGroundAngle * Mathf.Deg2Rad);
        Vector3 bestGroundNormal = Vector3.zero;
        bool hasGroundContact = false;

        foreach (ContactPoint contact in collision.contacts)
        {
            float groundDot = Vector3.Dot(contact.normal, Vector3.up);

            if (groundDot < bestGroundDot)
                continue;

            bestGroundDot = groundDot;
            bestGroundNormal = contact.normal;
            hasGroundContact = true;
        }

        if (!hasGroundContact)
            return;

        _hasGroundContact = true;
        _contactGroundNormal = bestGroundNormal;
    }
}
