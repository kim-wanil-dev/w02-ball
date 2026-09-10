using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCharacter : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckOrigin;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private bool isGrounded;
    public bool IsGrounded => isGrounded;


    [Header("Gravity")]
    [SerializeField] private float gravityStrength = 20f;


    [Header("Movement")]
    private Rigidbody rb;
    [SerializeField] private Vector2 moveInput;
    private Vector3 gravityDirection = Vector3.down;
    //private bool jumpRequested;
    private Vector3 targetVelocity;


    [Header("Sphere")]
    [SerializeField] private Transform sphereVisual;
    private PhysicsMaterial physicsMaterial;
    //[SerializeField] private float sphereRadius = 0.5f;


    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;


    [Header("Stat")]
    [SerializeField] private BallStat currentBallStat;
    [SerializeField] private BallStat pendingBallStat;
    [SerializeField] private bool canChange = true;

    [Header("UI")]
    [SerializeField] private Text velocityText;




    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;


    }

    private void Start()
    {
        physicsMaterial = sphereVisual.GetComponent<SphereCollider>().material;

        PlaySizeChange(currentBallStat);
        physicsMaterial.bounciness = currentBallStat.Bounciness;
        rb.mass = currentBallStat.Mass;
    }

    private void FixedUpdate()
    {
        CheckGround();
        //ProcessJump();
        ApplyMovement();
        RotateSphere();

        velocityText.text = $"{rb.linearVelocity.magnitude:F2}";
    }




    private void LateUpdate()
    {
    }


    public void PlaySizeChange(BallStat inputBallStat)
    {
        if (!canChange) return;
        groundCheckOrigin.localPosition = new Vector3(0f, -inputBallStat.SphereRadius + 0.5f, 0f);
        sphereVisual.localScale = Vector3.one * inputBallStat.SphereRadius * 2f;

        currentBallStat = inputBallStat;

        physicsMaterial.bounciness = currentBallStat.Bounciness;
        rb.mass = currentBallStat.Mass;

    }
    public void SetTargetVelocity()
    {
        targetVelocity = rb.linearVelocity;
    }


    public void SetMoveInput(Vector2 moveInput)

    {
        this.moveInput = moveInput;
    }


    //public void Jump()
    //{
    //    jumpRequested = true;
    //}



    private void CheckGround()
    {
        Vector3 origin =
            groundCheckOrigin != null
            ? groundCheckOrigin.position
            : transform.position;

        isGrounded =
            Physics.Raycast(
                origin,
                gravityDirection,
                groundCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );
    }


    private void ApplyMovement()
    {
        Vector3 upDirection =
            -gravityDirection.normalized;


        // 카메라가 바라보는 방향을
        // 현재 중력 기준의 이동 평면에 투영
        Vector3 cameraForward =
            Vector3.ProjectOnPlane(
                cameraTransform.forward,
                upDirection
            );

        if (cameraForward.sqrMagnitude < 0.001f)
            return;

        cameraForward.Normalize();


        // 현재 중력 기준 오른쪽 방향
        Vector3 cameraRight =
            Vector3.Cross(
                upDirection,
                cameraForward
            ).normalized;


        // W/S + A/D 조합
        Vector3 moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;


        // 대각선 이동이 빨라지는 것 방지
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }


        // 현재 중력 방향 속도를 제외한 이동 속도
        Vector3 currentPlanarVelocity =
            Vector3.ProjectOnPlane(
                rb.linearVelocity,
                gravityDirection
            );


        Vector3 targetPlanarVelocity =
            moveDirection * currentBallStat.MoveSpeed;


        // 목표 속도에 도달하기 위해 필요한 가속도
        Vector3 requiredAcceleration =
            (
                targetPlanarVelocity -
                currentPlanarVelocity
            )
            / Time.fixedDeltaTime;


        // 최대 가속도 제한
        requiredAcceleration =
            Vector3.ClampMagnitude(
                requiredAcceleration,
                currentBallStat.MoveAcceleration
            );


        //rb.AddForce(
        //    requiredAcceleration,
        //    ForceMode.Acceleration
        //);
        if (canChange)
        {
            rb.AddForce(
            requiredAcceleration,
            ForceMode.Force);
        }
        else
        {
            rb.linearVelocity = targetVelocity;
        }
        //else
        //{

        //    rb.AddForce(
        //    requiredAcceleration,
        //    ForceMode.Acceleration);
        //}


        rb.AddForce(
            gravityDirection * gravityStrength,
            ForceMode.Acceleration
        );
    }

    private void RotateSphere()
    {
        Vector3 velocity = rb.linearVelocity;

        // 수직 속도 제거
        Vector3 planarVelocity =
            Vector3.ProjectOnPlane(
                velocity,
                Vector3.up
            );

        float speed = planarVelocity.magnitude;

        if (speed < 0.01f)
            return;

        Vector3 moveDirection =
            planarVelocity.normalized;

        // 이동 방향에 수직인 회전축
        Vector3 rotationAxis =
            Vector3.Cross(
                Vector3.up,
                moveDirection
            ).normalized;

        // 이번 프레임에 이동한 거리
        float distance =
            speed * Time.deltaTime;

        // 굴러간 거리 = 반지름 × 회전각(rad)
        float angleRadians =
            distance / currentBallStat.SphereRadius;

        float angleDegrees =
            angleRadians * Mathf.Rad2Deg;

        sphereVisual.Rotate(
            rotationAxis,
            angleDegrees,
            Space.World
        );
    }


    // =========================================================
    // Jump
    // =========================================================

    public void ProcessJump()
    {
        rb.AddForce(
            -gravityDirection *
            currentBallStat.JumpForce,
            ForceMode.Impulse
        );
    }




    // =========================================================
    // Gizmos
    // =========================================================

    private void OnDrawGizmos()
    {
        Vector3 origin =
            groundCheckOrigin != null
            ? groundCheckOrigin.position
            : transform.position;


        // Gravity
        Gizmos.color = Color.red;

        Gizmos.DrawRay(
            transform.position,
            gravityDirection.normalized *
            2f
        );


        // Ground Check
        Gizmos.color = Color.green;

        Gizmos.DrawRay(
            origin,
            gravityDirection.normalized *
            groundCheckDistance
        );


        if (!Application.isPlaying ||
            rb == null)
        {
            return;
        }


        // Velocity
        Gizmos.color = Color.blue;

        Gizmos.DrawRay(
            transform.position,
            rb.linearVelocity
        );



    }
}
