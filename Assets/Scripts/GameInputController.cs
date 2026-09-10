using UnityEngine;

public class GameInputController : MonoBehaviour
{

    private PlayerInput _playerInput;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public float DiveInput { get; private set; }
    public float ResizeInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    private void Awake()
    {
        _playerInput = new PlayerInput();
    }

    private void OnEnable()
    {
        _playerInput.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInput.Player.Disable();
    }

    private void Update()
    {
        MoveInput = _playerInput.Player.Move.ReadValue<Vector2>();
        LookInput = _playerInput.Player.Look.ReadValue<Vector2>();
        DiveInput = _playerInput.Player.Dive.ReadValue<float>();
        ResizeInput = _playerInput.Player.Resize.ReadValue<float>();
        JumpPressed = _playerInput.Player.Jump.WasPressedThisFrame();
        JumpHeld = _playerInput.Player.Jump.IsPressed();
    }


}