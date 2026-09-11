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
    [SerializeField] private BallStat _smallBall;
    [SerializeField] private BallStat _largeBall;

    [Header("Gravity"), Range(-100f, 0f)]
    [SerializeField] private float _diveAcceleration = -20f;

    private Text _velocityText;
    private Text _heightText;

    private Transform _cameraTransform;
    private Vector2 _moveInput;
    private float _previousSizeChangeInput;
    private float _diveInput;
    private HapticManager _hapticManager;

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

    private bool _isExpanding = false;
    private bool _isShrinking = false;
    private float _elpasedTime = 0f;
    private float _fullTransitionDuration = 1f;
    private float _transitionDuration;

    private Vector3 _previousLenearVelocity;
    private Vector3 _originalLocalScale;
    private Vector3 _originalPosition;
    private float _originalMoveSpeed;
    private float _originalMoveAcceleration;
    private float _originalResponseTime;
    private float _originalJumpForce;
    private float _originalMaxGravityVelocity;
    private float _originalBounciness;
    private float _originalMass;

    private float _originalSizeRatio;
    private float _currentSizeRatio;
    private float _previousSizeRatio;
    private float _targetSizeRatio;
    private BallStat _targetBallStat;

    public bool IsGrounded { get; private set; }
    public BallStat SmallBall { get { return _smallBall; } }
    public BallStat LargeBall { get { return _largeBall; } }



    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;

        _collider = GetComponent<SphereCollider>();
        _physicsMaterial = _collider.material;

        _hapticManager = GetComponent<HapticManager>();

        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        //InitSizeBall(_ownedBalls[_currentBallNum]);
        InitSizeBall(_smallBall);

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
        ResizeBall(_targetBallStat);
        ProcessJumpInput();
        ProcessDiveInput();
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
        if (_elpasedTime >= _transitionDuration && _elpasedTime > 0f)
        {
            _isExpanding = false;
            _isShrinking = false;
            _elpasedTime = 0f;
            Time.timeScale = 1f;
            _targetBallStat = null;

            _hapticManager.StopHaptic();

            return;
        }

        int currentSizeChangeInput = (int)GameInputController.Instance.ResizeInput;

        if (_previousSizeChangeInput == currentSizeChangeInput)
            return;

        _previousSizeChangeInput = currentSizeChangeInput;
        _previousSizeRatio = _currentSizeRatio;
        _previousLenearVelocity = _rb.linearVelocity;


        if (currentSizeChangeInput == 0)
        {
            _isExpanding = false;
            _isShrinking = false;
            _elpasedTime = 0f;
            _targetBallStat = null;

            _hapticManager.StopHaptic();

            return;
        }

        SaveCurrentBallStat();

        _elpasedTime = 0f;
        if (currentSizeChangeInput == 1)
        {
            _isExpanding = true;
            _targetBallStat = _largeBall;
            _transitionDuration = 1 - _originalSizeRatio * _fullTransitionDuration;
            _targetSizeRatio = 1f;
            return;
        }

        if (currentSizeChangeInput == -1)
        {
            _isShrinking = true;
            _targetBallStat = _smallBall;
            _transitionDuration = _originalSizeRatio * _fullTransitionDuration;
            _targetSizeRatio = 0f;
            return;
        }

    }

    public void SaveCurrentBallStat()
    {
        _originalLocalScale = transform.localScale;
        _originalPosition = transform.position;
        _originalMoveSpeed = _moveSpeed;
        _originalMoveAcceleration = _moveAcceleration;
        _originalResponseTime = _moveResponseTime;
        _originalJumpForce = _jumpForce;
        _originalMaxGravityVelocity = _maxGravityVelocity;
        _originalBounciness = _physicsMaterial.bounciness;
        _originalMass = _rb.mass;
        _originalSizeRatio = _currentSizeRatio;
    }

    public void ResizeBall(BallStat inputBallStat)
    {
        if (_targetBallStat == null)
            return;
        if (!_isExpanding && !_isShrinking)
            return;
        if (_transitionDuration <= Mathf.Epsilon)
            return;
        _elpasedTime += Time.deltaTime;

        float x = _elpasedTime / _transitionDuration;
        float t = Mathf.Sin((x * Mathf.PI) / 2f);

        _currentSizeRatio = Mathf.Lerp(_originalSizeRatio, _targetSizeRatio, t);
        float previousRadius = transform.localScale.x;
        transform.localScale = Vector3.Lerp(_originalLocalScale, Vector3.one * inputBallStat.Scale, t);
        _moveSpeed = Mathf.Lerp(_originalMoveSpeed, inputBallStat.MoveSpeed, t);
        _moveAcceleration = Mathf.Lerp(_originalMoveAcceleration, inputBallStat.MoveAcceleration, t);
        _moveResponseTime = Mathf.Lerp(_originalResponseTime, inputBallStat.MoveResponseTime, t);
        _jumpForce = Mathf.Lerp(_originalJumpForce, inputBallStat.JumpForce, t);
        _maxGravityVelocity = Mathf.Lerp(_originalMaxGravityVelocity, inputBallStat.MaxGravityVelocity, t);
        _physicsMaterial.bounciness = Mathf.Lerp(_originalBounciness, inputBallStat.Bounciness, t);
        _rb.mass = Mathf.Lerp(_originalMass, inputBallStat.Mass, t);

        float radiusDelta = transform.localScale.x - previousRadius;
        transform.Translate(Vector3.up * (radiusDelta), Space.World);

        // 진동
        float intensity = Mathf.Lerp(
            0.05f,
            0.1f,
            _currentSizeRatio
        );

        _hapticManager.HapticControl(intensity);

        Debug.Log(radiusDelta);



    }
    private void ApplyMomentum()
    {
        float previousMass = Mathf.Lerp(_smallBall.Mass, _largeBall.Mass, _previousSizeRatio);


        Vector3 targetVelocity = _previousLenearVelocity * Mathf.Sqrt(previousMass / _rb.mass);

        _rb.linearVelocity = targetVelocity;
        _rb.angularVelocity = Vector3.zero;

        Vector3 velocity = _rb.linearVelocity;


        //반지름에 맞게 회전 계산
        Vector3 horizontalVelocity =
            new Vector3(velocity.x, 0f, velocity.z);

        if (horizontalVelocity.sqrMagnitude > 0.001f)
        {
            float angularSpeed =
                horizontalVelocity.magnitude / transform.localScale.x;

            Vector3 rotationAxis =
                Vector3.Cross(
                    Vector3.up,
                    horizontalVelocity.normalized
                );

            _rb.angularVelocity =
                rotationAxis * angularSpeed;
        }
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

        ClampGravityVelocity();

        _velocityText.text = $"{_rb.linearVelocity.magnitude:F2} m/s";
        _heightText.text = $"{_rb.transform.position.y:F2} m";
        Debug.Log($"Ground: {IsGrounded}\n Ground Normal: {_groundNormal}");
    }

    private void CheckGround()
    {
        Vector3 gravityDirection = _gravityDir;

        float sphereRadius =
            _collider.radius * transform.lossyScale.x;

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

        if (_isExpanding || _isShrinking)
            ApplyMomentum();
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
        transform.localScale = Vector3.one *
            inputBallStat.Scale;

        _moveSpeed = inputBallStat.MoveSpeed;
        _moveAcceleration = inputBallStat.MoveAcceleration;
        _moveResponseTime = inputBallStat.MoveResponseTime;
        _jumpForce = inputBallStat.JumpForce;
        _maxGravityVelocity = inputBallStat.MaxGravityVelocity;

        _physicsMaterial.bounciness =
            inputBallStat.Bounciness;

        _rb.mass = inputBallStat.Mass;
        _currentSizeRatio = inputBallStat.SizeRatio;

        SaveCurrentBallStat();
    }

    // 충돌
    private void OnCollisionEnter(Collision collision)
    {
        float impulse = collision.impulse.magnitude;

        float intensity = Mathf.InverseLerp(
            10f,
            100f,
            impulse
        );

        _hapticManager.HapticControl(
            intensity,
            0f,
            0.15f
        );
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
