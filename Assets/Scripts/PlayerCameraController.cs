using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameInputController inputHandler;

    [SerializeField] private float height = 1.5f;
    [SerializeField] private float sensitivity = 0.12f;

    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    private float yaw;
    private float pitch;

    private void Update()
    {
        Vector2 lookInput =
            inputHandler.LookInput;

        yaw +=
            lookInput.x *
            sensitivity;

        pitch -=
            lookInput.y *
            sensitivity;

        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );

        transform.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );
    }

    private void LateUpdate()
    {
        transform.position =
            player.position +
            Vector3.up * height;
    }
}