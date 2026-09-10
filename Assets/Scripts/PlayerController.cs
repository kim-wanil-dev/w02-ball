using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private GameInputController inputController;

    private Vector2 moveInput;
    private float previousSizeChangeInput;

    // =========================================================
    // Ground
    // =========================================================

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;
    public bool IsGrounded { get; set; }

    private Vector3 groundCheckOffset;

    // =========================================================
    // Gravity
    // =========================================================

    [Header("Gravity")]
    [SerializeField] private float gravityStrength = 20f;

    private Vector3 gravityDirection = Vector3.down;


    // =========================================================
    // Movement
    // =========================================================

    [Header("Movement")]
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private Vector3 targetVelocity;


    // =========================================================
    // Sphere
    // =========================================================

    private SphereCollider _collider;
    private PhysicsMaterial physicsMaterial;

    // =========================================================
    // Ball Stat
    // =========================================================

    [Header("Ball Stat")]
    [SerializeField] private List<BallStat> ownedBalls;
    [SerializeField] private int currentBallNum;

    private bool canChange = true;


    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    [SerializeField] private Text velocityText;


    // =========================================================
    // Unity Lifecycle
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;

        _collider =
            gameObject
                .GetComponent<SphereCollider>();
        physicsMaterial = _collider.material;
        PlaySizeChange(ownedBalls[currentBallNum]);
    }

    private void Start()
    {
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
        ApplyMovement();

        velocityText.text =
            $"{rb.linearVelocity.magnitude:F2}";
    }


    // =========================================================
    // Input
    // =========================================================

    private void ProcessMoveInput()
    {
        moveInput =
            inputController.MoveInput;
    }

    private void ProcessSizeChangeInput()
    {
        float currentSizeChangeInput =
            inputController.SizeChangeInput;

        if (
            previousSizeChangeInput != currentSizeChangeInput &&
            currentSizeChangeInput != 0f
        )
        {
            if (!canChange)
                return;

            int diff =
                (int)currentSizeChangeInput;

            int nextBallNum;

            if (diff == 1)
            {
                nextBallNum =
                    (currentBallNum + 1)
                    % ownedBalls.Count;
            }
            else if (diff == -1)
            {
                nextBallNum =
                    (
                        currentBallNum
                        - 1
                        + ownedBalls.Count
                    )
                    % ownedBalls.Count;
            }
            else
            {
                Debug.LogError(
                    $"currentSizeChangeInput: " +
                    $"{currentSizeChangeInput} error"
                );

                return;
            }

            currentBallNum =
                nextBallNum;

            PlaySizeChange(
                ownedBalls[currentBallNum]
            );
        }

        previousSizeChangeInput =
            currentSizeChangeInput;
    }

    private void ProcessJumpInput()
    {
        if (
            inputController.JumpPressed &&
            IsGrounded
        )
        {
            ProcessJump();
        }
    }


    // =========================================================
    // Ground
    // =========================================================

    private void CheckGround()
    {
        Vector3 origin = groundCheckOffset;

        IsGrounded =
            Physics.Raycast(
                origin,
                gravityDirection,
                groundCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
    }


    // =========================================================
    // Movement
    // =========================================================

    private void ApplyMovement()
    {
        Vector3 upDirection =
            -gravityDirection.normalized;


        // 카메라 Forward를
        // 중력에 수직인 이동 평면에 투영
        Vector3 cameraForward =
            Vector3.ProjectOnPlane(
                cameraTransform.forward,
                upDirection
            );

        if (cameraForward.sqrMagnitude < 0.001f)
            return;

        cameraForward.Normalize();


        Vector3 cameraRight =
            Vector3.Cross(
                upDirection,
                cameraForward
            ).normalized;


        Vector3 moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;


        // 대각선 속도 증가 방지
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }


        // 중력 방향 성분을 제외한 현재 속도
        Vector3 currentPlanarVelocity =
            Vector3.ProjectOnPlane(
                rb.linearVelocity,
                gravityDirection
            );


        Vector3 targetPlanarVelocity =
            moveDirection *
            ownedBalls[currentBallNum].MoveSpeed;


        // 목표 속도까지 필요한 가속도
        Vector3 requiredAcceleration =
            (
                targetPlanarVelocity -
                currentPlanarVelocity
            )
            / Time.fixedDeltaTime;


        requiredAcceleration =
            Vector3.ClampMagnitude(
                requiredAcceleration,
                ownedBalls[currentBallNum].MoveAcceleration
            );


        if (canChange)
        {
            rb.AddForce(
                requiredAcceleration,
                ForceMode.Force
            );
        }
        else
        {
            rb.linearVelocity =
                targetVelocity;
        }


        // 커스텀 중력
        rb.AddForce(
            gravityDirection *
            gravityStrength,
            ForceMode.Acceleration
        );
    }


    // =========================================================
    // Jump
    // =========================================================

    private void ProcessJump()
    {
        rb.AddForce(
            -gravityDirection *
            ownedBalls[currentBallNum].JumpForce,
            ForceMode.Impulse
        );
    }


    // =========================================================
    // Ball
    // =========================================================

    public void PlaySizeChange(BallStat inputBallStat)
    {
        if (!canChange)
            return;

        groundCheckOffset =
            new Vector3(
                0f,
                -inputBallStat.SphereRadius + 0.5f,
                0f
            );

        transform.localScale =
            Vector3.one *
            inputBallStat.SphereRadius *
            2f;

        ownedBalls[currentBallNum] =
            inputBallStat;

        physicsMaterial.bounciness =
            ownedBalls[currentBallNum].Bounciness;

        rb.mass =
            ownedBalls[currentBallNum].Mass;
    }

    public IEnumerator ChangeBallStat()
    {
        canChange = false;

        targetVelocity =
            rb.linearVelocity;

        Time.timeScale = 0.3f;

        yield return
            new WaitForSecondsRealtime(1f);

        Time.timeScale = 1f;

        canChange = true;
    }

    // =========================================================
    // Gizmos
    // =========================================================

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + groundCheckOffset;

        // Gravity
        Gizmos.color =
            Color.red;

        Gizmos.DrawRay(
            transform.position,
            gravityDirection.normalized *
            2f
        );


        // Ground Check
        Gizmos.color =
            Color.green;

        Gizmos.DrawRay(
            origin,
            gravityDirection.normalized *
            groundCheckDistance
        );


        if (
            !Application.isPlaying ||
            rb == null
        )
        {
            return;
        }


        // Velocity
        Gizmos.color =
            Color.blue;

        Gizmos.DrawRay(
            transform.position,
            rb.linearVelocity
        );
    }
}
