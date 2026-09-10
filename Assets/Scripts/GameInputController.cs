using UnityEngine;

public class GameInputController : MonoBehaviour
{

    private PlayerInput playerInput;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public float DiveInput { get; private set; }
    public float ResizeInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    private void Awake()
    {
        playerInput = new PlayerInput();
    }

    private void OnEnable()
    {
        playerInput.Player.Enable();
    }

    private void OnDisable()
    {
        playerInput.Player.Disable();
    }

    private void Update()
    {
        MoveInput = playerInput.Player.Move.ReadValue<Vector2>();
        LookInput = playerInput.Player.Look.ReadValue<Vector2>();
        DiveInput = playerInput.Player.Dive.ReadValue<float>();
        ResizeInput = playerInput.Player.Resize.ReadValue<float>();
        JumpPressed = playerInput.Player.Jump.WasPressedThisFrame();
        JumpHeld = playerInput.Player.Jump.IsPressed();
    }


}