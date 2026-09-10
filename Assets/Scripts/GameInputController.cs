using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputController : MonoBehaviour
{
    public static GameInputController Instance;
    // private PlayerInput playerInput;
    private InputSystem_Actions inputActions;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public float DiveInput { get; private set; }
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
        // playerInput = new PlayerInput();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Look.performed += CheckDeviceType;
        inputActions.Player.Look.canceled += CheckDeviceType;
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
        DiveInput = inputActions.Player.Dive.ReadValue<float>();
        ResizeInput = inputActions.Player.Resize.ReadValue<float>();
        JumpPressed = inputActions.Player.Jump.WasPressedThisFrame();
        JumpHeld = inputActions.Player.Jump.IsPressed();
    }


}