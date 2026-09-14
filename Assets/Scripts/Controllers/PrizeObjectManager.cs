using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PrizeObjectManager : MonoBehaviour
{
    private string _greeting = "잘 하셨습니다!\n이제부터 {Jump}키를 눌러 점프가 가능합니다.\n구슬을 모아 힘을 회복한 뒤 성지로 돌아가세요.\n\n";
    private string _guide = "점프: {Jump}\n강하: {Dive}\n크기 조절: {Resize}\n언제든지 이 구슬로 다시 돌아와 사용법을 확인하실 수 있습니다.";
    private GameObject _prizeUI;
    private Button _prizeButton;
    private bool _isTouched;
    private InputAction _confirmAction;

    private void Awake()
    {
        if (gameObject.CompareTag("Prize"))
            return;

        _prizeUI = Instantiate(Managers.Game.PrizeUI);

        _prizeButton = _prizeUI.GetComponentInChildren<Button>();
        _prizeButton.onClick.AddListener(OnPrizeUIButtonClicked);

        _confirmAction = InputSystem.actions.FindAction("Confirm");
        _confirmAction.performed += OnJumpPerformed;

        string jump = GetBindingName("Jump");
        string dive = GetBindingName("Dive");
        string resize = GetBindingName("Resize");

        string message = (_greeting + _guide)
            .Replace("{Jump}", jump)
            .Replace("{Dive}", dive)
            .Replace("{Resize}", resize);

        Text text = _prizeUI.transform.Find("Panel/Text").GetComponent<Text>();
        text.text = message;

        _prizeUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (gameObject.CompareTag("Prize"))
        {
            Destroy(gameObject);
            return;
        }

        GameInputController.Instance.SetInputMode(InputMode.UI);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        _prizeUI.SetActive(true);
    }
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!_prizeUI.activeSelf)
            return;

        OnPrizeUIButtonClicked();
    }

    private void OnPrizeUIButtonClicked()
    {
        _prizeUI.SetActive(false);

        if (!_isTouched)
        {
            string jump = GetBindingName("Jump");
            string dive = GetBindingName("Dive");
            string resize = GetBindingName("Resize");

            string message = _guide
                .Replace("{Jump}", jump)
                .Replace("{Dive}", dive)
                .Replace("{Resize}", resize);

            Text text = _prizeUI.transform.Find("Panel/Text").GetComponent<Text>();
            text.text = message;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.material.SetColor(
                "_EmissionColor",
                new Color(0, 171, 184)
            );
        }
        _isTouched = true;

        GameInputController.Instance.SetInputMode(InputMode.Player);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
    }

    private string GetBindingName(string actionName)
    {
        InputAction action = InputSystem.actions.FindAction(actionName);

        if (action == null)
            return actionName;

        return action.GetBindingDisplayString();
    }
}
