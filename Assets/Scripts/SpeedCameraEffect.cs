using Unity.Cinemachine;
using UnityEngine;

public class SpeedCameraEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;

    [Header("Speed")]
    [SerializeField] private float maxEffectSpeed = 20f;
    [SerializeField] private float effectStartSpeed = 8f;

    [Header("Damping")]
    [SerializeField]
    private Vector3 slowDamping =
        new Vector3(0.05f, 0.05f, 0.1f);

    [SerializeField]
    private Vector3 fastDamping =
        new Vector3(0.1f, 0.1f, 0.7f);

    [Header("Distance")]
    [SerializeField] private float slowDistance = 4f;
    [SerializeField] private float fastDistance = 6f;

    [Header("FOV")]
    [SerializeField] private float slowFOV = 60f;
    [SerializeField] private float fastFOV = 75f;

    [Header("Smooth")]
    [SerializeField] private float effectChangeSpeed = 3f;


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

        thirdPersonFollow.Damping =
            Vector3.Lerp(
                thirdPersonFollow.Damping,
                targetDamping,
                effectChangeSpeed *
                Time.deltaTime
            );


        // -------------------------
        // Camera Distance
        // -------------------------

        float targetDistance =
            Mathf.Lerp(
                slowDistance,
                fastDistance,
                speedRatio
            );

        thirdPersonFollow.CameraDistance =
            Mathf.Lerp(
                thirdPersonFollow.CameraDistance,
                targetDistance,
                effectChangeSpeed *
                Time.deltaTime
            );


        // -------------------------
        // FOV
        // -------------------------

        LensSettings lens =
            cinemachineCamera.Lens;

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

        cinemachineCamera.Lens = lens;
    }
}