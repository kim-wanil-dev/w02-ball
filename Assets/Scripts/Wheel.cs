using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Wheel : MonoBehaviour
{
    private List<Rigidbody> _wings;
    public bool _isTouched;
    private string _tutorialMessage = "친구들을 따라가지 못하고 떨어져 버렸습니다.\n성지로 돌아갈 방법을 찾아보세요.\n";
    private string _guide = "크기 조절 : {Resize}";
    private GameObject _tutorialUIPrefab;
    private GameObject _tutorialUIObject;
    private Button _tutorialButton;
    private InputAction _confirmAction;

    void Awake()
    {
        _wings = new List<Rigidbody>();
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject.GetComponent<Wheel>() == null)
            {
                _wings.Add(rb);
            }
        }
        _tutorialUIPrefab = Resources.Load<GameObject>("prefabs/UIs/TutorialCanvas");
        _tutorialUIObject = Instantiate(_tutorialUIPrefab);
        _tutorialButton = _tutorialUIObject.GetComponentInChildren<Button>();
        _tutorialButton.onClick.AddListener(OnTutorialButtonClicked);
        _confirmAction = InputSystem.actions.FindAction("Confirm");
        _confirmAction.performed += OnJumpPerformed;
        _tutorialUIObject.SetActive(false);
    }

    public void SetRigidbodyConstraintsAllWings(bool bigScaleCollisionStay)
    {
        if (bigScaleCollisionStay)
        {
            foreach (Rigidbody rb in _wings)
            {
                rb.constraints = RigidbodyConstraints.None;
            }
        }
        else
        {
            foreach (Rigidbody rb in _wings)
            {
                rb.constraints = RigidbodyConstraints.FreezePosition;
            }
        }
    }

    public void ActiveTutorialCanvas()
    {
        string resize = GetBindingName("Resize");

        string message = _tutorialMessage + _guide;
        message = message.Replace("{Resize}", resize);

        Text text = _tutorialUIObject.transform.Find("Panel/Text").GetComponent<Text>();
        text.text = message;

        GameInputController.Instance.SetInputMode(InputMode.UI);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        _tutorialUIObject.SetActive(true);
    }

    private void OnTutorialButtonClicked()
    {
        _tutorialUIObject.SetActive(false);
        GameInputController.Instance.SetInputMode(InputMode.Player);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
        _isTouched = true;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!_tutorialUIObject.activeSelf)
            return;

        OnTutorialButtonClicked();
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
