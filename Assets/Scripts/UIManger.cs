using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum TutorialSequence
{
    Greeting = 0,
    Guide,
    SecondPirzeGuide,
    FirstSaveGuide

}
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private string _greeting = "잘 하셨습니다!\n이제부터 {Jump}키를 눌러 점프가 가능합니다.\n구슬을 모아 힘을 회복한 뒤 성지로 돌아가세요.\n\n";
    private string _guide = "점프: {Jump}\n강하: {Dive}\n크기 조절: {Resize}\n언제든지 이 구슬로 다시 돌아와 사용법을 확인하실 수 있습니다.";
    private string _secondPirzeGuide = "축하합니다!\n 첫 구슬을 획득하셨습니다.\n 구슬을 얻을 때 마다 점프 횟수가 1회 늘어납니다.\n";
    private string _firstSaveGuide = "첫 세이브 포인트에 도달했습니다!\n[{Restart}]을 눌러 언제든지 이곳에서 다시 시작할 수 있습니다.";
    [SerializeField] private GameObject _prizeUI;
    [SerializeField] private Button _prizeButton;
    [SerializeField] private Text _text;
    //private bool _isTouched;
    [SerializeField] private InputAction _confirmAction;

    public bool isGetFirstSave = true;
    public bool isGetFirstPrize = true;

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

        _confirmAction = InputSystem.actions.FindAction("Confirm");
        _confirmAction.performed += OnJumpPerformed;
        _prizeUI.SetActive(false);

        _prizeButton.onClick.AddListener(OnPrizeUIButtonClicked);
    }
    private void Update()
    {

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



        GameInputController.Instance.SetInputMode(InputMode.Player);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
    }


    public void ShowTutorial(TutorialSequence seq)
    {
        GameInputController.Instance.SetInputMode(InputMode.UI);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        _prizeUI.SetActive(true);

        string jump = GetBindingName("Jump");
        string dive = GetBindingName("Dive");
        string resize = GetBindingName("Resize");
        string restrart = GetBindingName("Restart");

        string message;
        switch (seq)
        {
            case TutorialSequence.Greeting:
                message = _greeting + _guide;
                break;
            case TutorialSequence.Guide:
                message = _guide;
                break;
            case TutorialSequence.SecondPirzeGuide:
                message = _secondPirzeGuide;
                break;
            case TutorialSequence.FirstSaveGuide:
                message = _firstSaveGuide;
                break;
            default:
                Debug.LogError($"존재하지 않는 TutorialSequence : {seq}");
                return;

        }


        //string message = _isTouched ? _guide : _greeting + _guide;
        message = message
               .Replace("{Jump}", jump)
               .Replace("{Dive}", dive)
               .Replace("{Resize}", resize)
               .Replace("{Restart}", restrart);

        // _text = _prizeUI.transform.Find("Panel/Text").GetComponent<Text>();
        _text.text = message;
    }





    private string GetBindingName(string actionName)
    {
        InputAction action = InputSystem.actions.FindAction(actionName);

        if (action == null)
            return actionName;

        bool isGamepad = GameInputController.Instance.GamePadConnected;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite)
            {
                bool compositeIsGamepad = false;

                for (int j = i + 1; j < action.bindings.Count; j++)
                {
                    InputBinding part = action.bindings[j];

                    if (!part.isPartOfComposite)
                        break;

                    string path = part.effectivePath;

                    if (!string.IsNullOrEmpty(path) &&
                        path.StartsWith("<Gamepad>/"))
                    {
                        compositeIsGamepad = true;
                        break;
                    }

                    if (part.groups != null &&
                        part.groups.Contains("Gamepad"))
                    {
                        compositeIsGamepad = true;
                        break;
                    }
                }

                if (isGamepad == compositeIsGamepad)
                    return action.GetBindingDisplayString(i);
            }
            else if (!binding.isPartOfComposite)
            {
                string path = binding.effectivePath;

                bool isBindingGamepad =
                    (binding.groups != null &&
                     binding.groups.Contains("Gamepad")) ||
                    (!string.IsNullOrEmpty(path) &&
                     path.StartsWith("<Gamepad>/"));

                if (isGamepad == isBindingGamepad)
                    return action.GetBindingDisplayString(i);
            }
        }

        return action.GetBindingDisplayString();
    }

}