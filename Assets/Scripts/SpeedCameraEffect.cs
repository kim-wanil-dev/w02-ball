using Unity.Cinemachine;
using UnityEngine;

public class SpeedCameraEffect : MonoBehaviour
{
    [Header("References")]
    private Rigidbody _playerRb;
    private PlayerController _playerController;
    private Transform _mainCamTransform;

    [Header("Speed")]
    [SerializeField] private float _maxEffectSpeed = 80f;
    [SerializeField] private float _effectStartSpeed = 20f;

    [Header("Damping")]
    [SerializeField]
    private Vector3 _slowDamping =
        new Vector3(0.05f, 0.05f, 0.1f);

    [SerializeField]
    private Vector3 _fastDamping =
        new Vector3(0.1f, 0.1f, 0.7f);

    [Header("Distance")]
    //[SerializeField] private float slowDistance = 4f;
    //[SerializeField] private float fastDistance = 6f;

    [Header("FOV")]
    [SerializeField] private float _slowFOV = 60f;
    [SerializeField] private float _fastFOV = 75f;

    [Header("Smooth")]
    [SerializeField] private float _effectChangeSpeed = 3f;

    private CinemachineCamera _cinemachineCamera;
    private CinemachineThirdPersonFollow _thirdPersonFollow;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        _thirdPersonFollow = GetComponent<CinemachineThirdPersonFollow>();
    }

    private void Start()
    {
        if (GameObject.Find("Player") != null)
        {
            _playerController = GameObject.Find("Player").GetComponent<PlayerController>();
            _playerRb = GameObject.Find("Player").GetComponent<Rigidbody>();
        }

        _mainCamTransform = Camera.main.transform;
    }
    private void Update()
    {
        UpdateSpeedCameraEffect();


    }


    private void UpdateSpeedCameraEffect()
    {

        float speed =
            _playerRb.linearVelocity.magnitude;

        float speedRatio =
            Mathf.InverseLerp(
                _effectStartSpeed,
                _maxEffectSpeed,
                speed
            );


        // -------------------------
        // Damping
        // -------------------------

        Vector3 targetDamping =
            Vector3.Lerp(
                _slowDamping,
                _fastDamping,
                speedRatio
            );

        //카메라 보는 방향과 플레이어의 이동 방향이 반대인 경우 z댐핑 없애기
        if (Vector3.Dot(_mainCamTransform.forward, _playerRb.linearVelocity) <= 0)
        {
            targetDamping.z = 0;
        }

        _thirdPersonFollow.Damping =
            Vector3.Lerp(
                _thirdPersonFollow.Damping,
                targetDamping,
                _effectChangeSpeed *
                Time.deltaTime
            );


        // -------------------------
        // Camera Distance
        // -------------------------

        float targetDistance =
            Mathf.Lerp(
                _playerController.OwnedBalls[_playerController.CurrentBallNum].SlowCameraDistance,
                _playerController.OwnedBalls[_playerController.CurrentBallNum].FastCameraDistance,
                speedRatio
            );

        _thirdPersonFollow.CameraDistance =
            Mathf.Lerp(
                _thirdPersonFollow.CameraDistance,
                targetDistance,
                _effectChangeSpeed *
                Time.deltaTime
            );


        // -------------------------
        // FOV
        // -------------------------

        LensSettings lens =
            _cinemachineCamera.Lens;

        float targetFOV;
        if (Vector3.Dot(_mainCamTransform.forward, _playerRb.linearVelocity) <= 0)
        {
            targetFOV = _slowFOV;
        }

        else
        {
            targetFOV = Mathf.Lerp(_slowFOV, _fastFOV, speedRatio);

        }

        lens.FieldOfView =
            Mathf.Lerp(
                lens.FieldOfView,
                targetFOV,
                _effectChangeSpeed *
                Time.deltaTime
            );

        _cinemachineCamera.Lens = lens;
    }
}