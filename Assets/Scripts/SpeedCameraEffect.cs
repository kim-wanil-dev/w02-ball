using Unity.Cinemachine;
using UnityEngine;

public class SpeedCameraEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform mainCamTransform;

    [Header("Speed")]
    [SerializeField] private float maxEffectSpeed = 80f;
    [SerializeField] private float effectStartSpeed = 20f;

    [Header("Damping")]
    [SerializeField]
    private Vector3 slowDamping =
        new Vector3(0.05f, 0.05f, 0.1f);

    [SerializeField]
    private Vector3 fastDamping =
        new Vector3(0.1f, 0.1f, 0.7f);

    [Header("Distance")]
    //[SerializeField] private float slowDistance = 4f;
    //[SerializeField] private float fastDistance = 6f;

    [Header("FOV")]
    [SerializeField] private float slowFOV = 60f;
    [SerializeField] private float fastFOV = 75f;

    [Header("Smooth")]
    [SerializeField] private float effectChangeSpeed = 3f;

    private CinemachineCamera _cinemachineCamera;
    private CinemachineThirdPersonFollow _thirdPersonFollow;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        _thirdPersonFollow = GetComponent<CinemachineThirdPersonFollow>();
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = GameObject.Find("Player").GetComponent<PlayerController>();
        }
        if (mainCamTransform == null)
        {
            mainCamTransform = Camera.main.transform;
        }


    }
    private void Update()
    {
        UpdateSpeedCameraEffect();


    }


    private void UpdateSpeedCameraEffect()
    {

        float speed =
            playerRb.linearVelocity.magnitude;

        float speedRatio =
            Mathf.InverseLerp(
                effectStartSpeed,
                maxEffectSpeed,
                speed
            );


        // -------------------------
        // Damping
        // -------------------------

        Vector3 targetDamping =
            Vector3.Lerp(
                slowDamping,
                fastDamping,
                speedRatio
            );

        //카메라 보는 방향과 플레이어의 이동 방향이 반대인 경우 z댐핑 없애기
        if (Vector3.Dot(mainCamTransform.forward, playerRb.linearVelocity) <= 0)
        {
            targetDamping.z = 0;
        }

        _thirdPersonFollow.Damping =
            Vector3.Lerp(
                _thirdPersonFollow.Damping,
                targetDamping,
                effectChangeSpeed *
                Time.deltaTime
            );


        // -------------------------
        // Camera Distance
        // -------------------------

        float targetDistance =
            Mathf.Lerp(
                playerController.OwnedBalls[playerController.CurrentBallNum].SlowCameraDistance,
                playerController.OwnedBalls[playerController.CurrentBallNum].FastCameraDistance,
                speedRatio
            );

        _thirdPersonFollow.CameraDistance =
            Mathf.Lerp(
                _thirdPersonFollow.CameraDistance,
                targetDistance,
                effectChangeSpeed *
                Time.deltaTime
            );


        // -------------------------
        // FOV
        // -------------------------

        LensSettings lens =
            _cinemachineCamera.Lens;

        float targetFOV =
            Mathf.Lerp(
                slowFOV,
                fastFOV,
                speedRatio
            );

        lens.FieldOfView =
            Mathf.Lerp(
                lens.FieldOfView,
                targetFOV,
                effectChangeSpeed *
                Time.deltaTime
            );

        _cinemachineCamera.Lens = lens;
    }
}