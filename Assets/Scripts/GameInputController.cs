using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode
{
    Player,
    UI
}

public class GameInputController : MonoBehaviour
{
    public static GameInputController Instance;
    private InputSystem_Actions inputActions;

    private InputMode _inputMode = InputMode.Player;
    private InputActionMap _playerMap;
    private InputActionMap _uiMap;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool DiveInput { get; private set; }
    public float ResizeInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool GamePadConnected { get; private set; }
    private void Awake()
    {
        Instance = this;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        inputActions = new InputSystem_Actions();

        _playerMap = inputActions.Player;
        _uiMap = inputActions.UI;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Look.performed += CheckDeviceType;
        inputActions.Player.Look.canceled += CheckDeviceType;

        SetInputMode(InputMode.Player);
    }

    private void OnDisable()
    {
        inputActions.Player.Look.performed -= CheckDeviceType;
        inputActions.Player.Look.canceled -= CheckDeviceType;

        inputActions.Disable();
    }

    private void CheckDeviceType(InputAction.CallbackContext ctx)
    {
        GamePadConnected = ctx.control.device is Gamepad;
    }

    private void Update()
    {
        MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
        LookInput = inputActions.Player.Look.ReadValue<Vector2>();
        DiveInput = inputActions.Player.Dive.IsPressed();
        ResizeInput = inputActions.Player.Resize.ReadValue<float>();
        JumpPressed = inputActions.Player.Jump.WasPressedThisFrame();
        JumpHeld = inputActions.Player.Jump.IsPressed();
    }

    public void SetInputMode(InputMode mode)
    {
        _inputMode = mode;

        JumpPressed = false;
        DiveInput = false;
        ResizeInput = 0f;
        MoveInput = Vector2.zero;

        if (mode == InputMode.Player)
        {
            _uiMap.Disable();
            _playerMap.Enable();
        }
        else
        {
            _playerMap.Disable();
            _uiMap.Enable();
        }
    }
}