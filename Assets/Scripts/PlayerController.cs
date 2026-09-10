using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private GameInputController inputController;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Movement")]
    [SerializeField] private float moveResponseTime = 0.2f;

    [Header("Ball Stat")]
    [SerializeField] private List<BallStat> ownedBalls;
    [SerializeField] private int currentBallNum;

    [Header("UI")]
    [SerializeField] private Text velocityText;

    private Transform cameraTransform;
    private Vector2 moveInput;
    private float previousSizeChangeInput;


    private Vector3 groundCheckOffset;
    private Vector3 groundNormal = Vector3.up;

    private Rigidbody rb;

    private SphereCollider _collider;
    private PhysicsMaterial physicsMaterial;

    private bool canChange = true;

    private Vector3 targetVelocity;

    private bool jumpRequested;

    public bool IsGrounded { get; private set; }
    public List<BallStat> OwnedBalls { get { return ownedBalls; } }
    public int CurrentBallNum { get { return currentBallNum; } }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;

        _collider = GetComponent<SphereCollider>();
        physicsMaterial = _collider.material;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        PlaySizeChange(ownedBalls[currentBallNum]);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        ProcessMoveInput();
        ProcessSizeChangeInput();
        ProcessJumpInput();

        Debug.Log($"Ground: {IsGrounded}\n Ground Normal: {groundNormal}");
    }

    private void FixedUpdate()
    {
        CheckGround();
        ProcessJump();
        ApplyMovement();

        if (velocityText != null)
            velocityText.text = $"{rb.linearVelocity.magnitude:F2}";
    }

    private void ProcessMoveInput()
    {
        moveInput = inputController.MoveInput;
    }

    private void ProcessSizeChangeInput()
    {
        float currentSizeChangeInput = inputController.SizeChangeInput;

        if (previousSizeChangeInput != currentSizeChangeInput && currentSizeChangeInput != 0f)
        {
            if (!canChange)
                return;

            int diff = (int)currentSizeChangeInput;
            int nextBallNum;

            if (diff == 1)
            {
                nextBallNum = (currentBallNum + 1) % ownedBalls.Count;
            }
            else if (diff == -1)
            {
                nextBallNum = (currentBallNum - 1 + ownedBalls.Count) % ownedBalls.Count;
            }
            else
            {
                Debug.LogError($"currentSizeChangeInput: {currentSizeChangeInput} error");
                return;
            }

            currentBallNum = nextBallNum;
            PlaySizeChange(ownedBalls[currentBallNum]);
        }

        previousSizeChangeInput = currentSizeChangeInput;
    }

    private void ProcessJumpInput()
    {
        if (inputController.JumpPressed && IsGrounded)
            jumpRequested = true;
    }

    private void CheckGround()
    {
        Vector3 origin = transform.position + groundCheckOffset;
        Vector3 gravityDirection = GetGravityDirection();

        if (Physics.Raycast(
            origin,
            gravityDirection,
            out RaycastHit hit,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            IsGrounded = true;
            groundNormal = hit.normal;
        }
        else
        {
            IsGrounded = false;
        }
    }

    private void ApplyMovement()
    {
        if (!canChange)
        {
            rb.linearVelocity = targetVelocity;
            return;
        }

        Vector3 worldMoveInput = GetWorldMoveInput(moveInput);

        if (IsGrounded)
            Roll(worldMoveInput);
        else
            AirMove(worldMoveInput);
    }

    private Vector3 GetWorldMoveInput(Vector2 input)
    {
        Vector3 upDirection = -GetGravityDirection();

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, upDirection);

        if (cameraForward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        cameraForward.Normalize();

        Vector3 cameraRight = Vector3.Cross(upDirection, cameraForward).normalized;

        Vector3 worldMoveInput =
            cameraForward * input.y +
            cameraRight * input.x;

        return Vector3.ClampMagnitude(worldMoveInput, 1f);
    }

    private void Roll(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection = GetGroundMoveDirection(worldMoveInput);

        UpdateGroundMoveVelocity(
            groundMoveDirection,
            worldMoveInput.magnitude
        );
    }

    private Vector3 GetGroundMoveDirection(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection = Vector3.ProjectOnPlane(worldMoveInput, groundNormal);

        if (groundMoveDirection.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return groundMoveDirection.normalized;
    }

    private void UpdateGroundMoveVelocity(Vector3 groundMoveDirection, float inputMagnitude)
    {
        inputMagnitude = Mathf.Clamp01(inputMagnitude);

        if (inputMagnitude <= 0.001f)
            return;

        float targetSpeed = ownedBalls[currentBallNum].MoveSpeed * inputMagnitude;

        Vector3 currentGroundVelocity =
            Vector3.ProjectOnPlane(rb.linearVelocity, groundNormal);

        float currentSpeed =
            Vector3.Dot(currentGroundVelocity, groundMoveDirection);

        if (currentSpeed >= targetSpeed)
            return;

        float responseAcceleration =
            ownedBalls[currentBallNum].MoveSpeed / moveResponseTime;

        float remainingSpeed = targetSpeed - currentSpeed;

        float velocityChange = Mathf.Min(
            responseAcceleration * Time.fixedDeltaTime,
            remainingSpeed
        );

        float acceleration = velocityChange / Time.fixedDeltaTime;

        rb.AddForce(
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
        float inputMagnitude = Mathf.Clamp01(worldMoveInput.magnitude);

        if (inputMagnitude <= 0.001f)
            return;

        Vector3 moveDirection = worldMoveInput.normalized;
        float targetSpeed = ownedBalls[currentBallNum].MoveSpeed * inputMagnitude;

        Vector3 planarVelocity =
            Vector3.ProjectOnPlane(rb.linearVelocity, GetGravityDirection());

        float currentSpeed =
            Vector3.Dot(planarVelocity, moveDirection);

        if (currentSpeed >= targetSpeed)
            return;

        float remainingSpeed = targetSpeed - currentSpeed;

        float acceleration =
            ownedBalls[currentBallNum].MoveAcceleration * inputMagnitude;

        float velocityChange = Mathf.Min(
            acceleration * Time.fixedDeltaTime,
            remainingSpeed
        );

        acceleration = velocityChange / Time.fixedDeltaTime;

        rb.AddForce(
            moveDirection * acceleration,
            ForceMode.Acceleration
        );
    }

    private void ProcessJump()
    {
        if (!jumpRequested)
            return;

        jumpRequested = false;

        if (!IsGrounded)
            return;

        Vector3 jumpDirection = -GetGravityDirection();

        rb.AddForce(
            jumpDirection * ownedBalls[currentBallNum].JumpForce,
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
        if (!canChange)
            return;

        groundCheckOffset = new Vector3(
            0f,
            -inputBallStat.SphereRadius + 0.5f,
            0f
        );

        transform.localScale =
            Vector3.one * inputBallStat.SphereRadius * 2f;

        ownedBalls[currentBallNum] = inputBallStat;

        physicsMaterial.bounciness =
            ownedBalls[currentBallNum].Bounciness;

        rb.mass =
            ownedBalls[currentBallNum].Mass;
    }
}
