using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private GameInputController _inputController;

    [Header("Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 1f;

    [Header("Movement")]
    [SerializeField] private float _moveResponseTime = 0.2f;

    [Header("Ball Stat")]
    [SerializeField] private List<BallStat> _ownedBalls;
    [SerializeField] private int _currentBallNum;

    private Text _velocityText;
    private Text _heightText;

    private Transform _cameraTransform;
    private Vector2 _moveInput;
    private float _previousSizeChangeInput;

    private Vector3 _groundCheckOffset;
    private Vector3 _groundNormal = Vector3.up;

    private Rigidbody _rb;

    private SphereCollider _collider;
    private PhysicsMaterial _physicsMaterial;

    private bool _canChange = true;

    private Vector3 _targetVelocity;

    private bool _jumpRequested;

    public bool IsGrounded { get; private set; }
    public List<BallStat> OwnedBalls { get { return _ownedBalls; } }
    public int CurrentBallNum { get { return _currentBallNum; } }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;

        _collider = GetComponent<SphereCollider>();
        _physicsMaterial = _collider.material;

        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        PlaySizeChange(_ownedBalls[_currentBallNum]);

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
        ProcessSizeChangeInput();
        ProcessJumpInput();
    }

    private void FixedUpdate()
    {
        CheckGround();
        ProcessJump();
        ApplyMovement();

        _velocityText.text = $"{_rb.linearVelocity.magnitude:F2} m/s";
        _heightText.text = $"{_rb.transform.position.y:F2} m";
        Debug.Log($"Ground: {IsGrounded}\n Ground Normal: {_groundNormal}");
    }

    private void ProcessMoveInput()
    {
        _moveInput = _inputController.MoveInput;
    }

    private void ProcessSizeChangeInput()
    {
        int currentSizeChangeInput = (int)_inputController.ResizeInput;

        if (_previousSizeChangeInput == currentSizeChangeInput || currentSizeChangeInput == 0)
            return;

        int nextBallNum = Mathf.Clamp(currentSizeChangeInput + _currentBallNum, 0, _ownedBalls.Count);

        _currentBallNum = nextBallNum;
        PlaySizeChange(_ownedBalls[_currentBallNum]);

        _previousSizeChangeInput = currentSizeChangeInput;
    }

    private void ProcessJumpInput()
    {
        if (_inputController.JumpPressed && IsGrounded)
            _jumpRequested = true;
    }

    private void CheckGround()
    {
        Vector3 origin = transform.position + _groundCheckOffset;
        Vector3 gravityDirection = GetGravityDirection();

        if (Physics.Raycast(
            origin,
            gravityDirection,
            out RaycastHit hit,
            _groundCheckDistance,
            _groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            IsGrounded = true;
            _groundNormal = hit.normal;
        }
        else
        {
            IsGrounded = false;
        }
    }

    private void ApplyMovement()
    {
        if (!_canChange)
        {
            _rb.linearVelocity = _targetVelocity;
            return;
        }

        Vector3 worldMoveInput = GetWorldMoveInput(_moveInput);

        if (IsGrounded)
            Roll(worldMoveInput);
        else
            AirMove(worldMoveInput);
    }

    private Vector3 GetWorldMoveInput(Vector2 input)
    {
        Vector3 upDirection = -GetGravityDirection();

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

        float targetSpeed =
            _ownedBalls[_currentBallNum].MoveSpeed * inputMagnitude;

        Vector3 currentGroundVelocity =
            Vector3.ProjectOnPlane(
                _rb.linearVelocity,
                _groundNormal
            );

        float currentSpeed =
            Vector3.Dot(
                currentGroundVelocity,
                groundMoveDirection
            );

        if (currentSpeed >= targetSpeed)
            return;

        float responseAcceleration =
            _ownedBalls[_currentBallNum].MoveSpeed /
            _moveResponseTime;

        float remainingSpeed =
            targetSpeed - currentSpeed;

        float velocityChange = Mathf.Min(
            responseAcceleration * Time.fixedDeltaTime,
            remainingSpeed
        );

        float acceleration =
            velocityChange / Time.fixedDeltaTime;

        _rb.AddForce(
            groundMoveDirection * acceleration,
            ForceMode.Acceleration
        );
    }

    private void AirMove(Vector3 worldMoveInput)
    {
        UpdateAirMoveVelocity(worldMoveInput);
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
            _ownedBalls[_currentBallNum].MoveSpeed *
            inputMagnitude;

        Vector3 planarVelocity =
            Vector3.ProjectOnPlane(
                _rb.linearVelocity,
                GetGravityDirection()
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
            _ownedBalls[_currentBallNum].MoveAcceleration *
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

    private void ProcessJump()
    {
        if (!_jumpRequested)
            return;

        _jumpRequested = false;

        if (!IsGrounded)
            return;

        Vector3 jumpDirection =
            -GetGravityDirection();

        _rb.AddForce(
            jumpDirection *
            _ownedBalls[_currentBallNum].JumpForce,
            ForceMode.Impulse
        );
    }

    private Vector3 GetGravityDirection()
    {
        if (Physics.gravity.sqrMagnitude < 0.001f)
            return Vector3.down;

        return Physics.gravity.normalized;
    }

    public void PlaySizeChange(BallStat inputBallStat)
    {
        if (!_canChange)
            return;

        _groundCheckOffset = new Vector3(
            0f,
            -inputBallStat.SphereRadius + 0.5f,
            0f
        );

        transform.localScale =
            Vector3.one *
            inputBallStat.SphereRadius *
            2f;

        _ownedBalls[_currentBallNum] = inputBallStat;

        _physicsMaterial.bounciness =
            _ownedBalls[_currentBallNum].Bounciness;

        _rb.mass =
            _ownedBalls[_currentBallNum].Mass;
    }
}
