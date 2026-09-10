using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private GameInputController _inputHandler;

    [SerializeField] private float _height = 1.5f;
    [SerializeField] private float _sensitivity = 0.12f;

    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 70f;

    private float _yaw;
    private float _pitch;

    private void Update()
    {
        Vector2 lookInput =
            _inputHandler.LookInput;

        _yaw +=
            lookInput.x *
            _sensitivity;

        _pitch -=
            lookInput.y *
            _sensitivity;

        _pitch =
            Mathf.Clamp(
                _pitch,
                _minPitch,
                _maxPitch
            );

        transform.rotation =
            Quaternion.Euler(
                _pitch,
                _yaw,
                0f
            );
    }

    private void FixedUpdate()
    {
        transform.position =
            _player.position +
            Vector3.up * _height;
    }
}