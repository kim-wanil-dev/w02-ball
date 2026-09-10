using UnityEngine;

public class GameInputController : MonoBehaviour
{
    //[SerializeField] private InputActionReference moveAction;
    //[SerializeField] private InputActionReference jumpAction;
    //[SerializeField] private InputActionReference lookAction;
    //[SerializeField] private InputActionReference changeSizeAction;
    [SerializeField] private PlayerCameraController playerCameraController;

    private PlayerInput playerInput;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public float SizeChangeInput { get; private set; }
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
        SizeChangeInput = playerInput.Player.SizeChange.ReadValue<float>();
        JumpHeld = playerInput.Player.Jump.IsPressed();
        JumpPressed = playerInput.Player.Jump.WasPressedThisFrame();


    }


}