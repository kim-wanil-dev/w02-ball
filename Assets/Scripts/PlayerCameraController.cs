using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private float _height = 1.5f;
    [SerializeField] private float _mouseSensitivity = 0.12f;
    [SerializeField] private float _gamepadSensitivity = 0.6f;

    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 70f;

    private Transform _player;

    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        _player = GameObject.Find("Player").transform;
    }

    private void Update()
    {
        Vector2 lookInput =
            GameInputController.Instance.LookInput;

        if (GameInputController.Instance.GamePadConnected)
        {
            _yaw +=
                lookInput.x *
                _gamepadSensitivity;

            _pitch -=
                lookInput.y *
                _gamepadSensitivity;
        }
        else
        {
            _yaw +=
                lookInput.x *
                _mouseSensitivity;

            _pitch -=
                lookInput.y *
                _mouseSensitivity;
        }

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