using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    [SerializeField] private GameObject[] _cineCams;
    [SerializeField] private SphereFriend[] _friends;
    [SerializeField] private GameObject _savePoint;
    [SerializeField] private Vector3 _startPos;
    [SerializeField] private float _fadeTime = 2f;
    [SerializeField] private PlayerController _player;
    private bool _cutsceneStarted = false;
    private bool _timerOn = false;
    private float _timeElapsed = 0;
    private float _endTime = 3;
    private int _flag = 0;

    private string _endingMessage = "Thanks For Play!\n친구들을 따라 여행을 시작하세요!\n세이브 포인트로 돌아와 맵을 구경할 수도 있습니다.";
    private Button _endingButton;
    private GameObject _endingUIPrefab;
    private GameObject _endingUIObject;
    private GameObject _fadeOutUIPrefab;
    private GameObject _fadeOutUiObject;
    private InputAction _confirmAction;


    void Awake()
    {
        _endingUIPrefab = Resources.Load<GameObject>("Prefabs/UIs/EndingCanvas");
        _fadeOutUIPrefab = Resources.Load<GameObject>("Prefabs/UIs/FadeOutCanvas");
        _endingUIObject = Instantiate(_endingUIPrefab);
        _fadeOutUiObject = Instantiate(_fadeOutUIPrefab);

        _endingButton = _endingUIObject.GetComponentInChildren<Button>();
        _endingButton.onClick.AddListener(OnEndingUIButtonClicked);
        _confirmAction = InputSystem.actions.FindAction("Confirm");
        _confirmAction.performed += OnJumpPerformed;
        _endingUIObject.SetActive(false);
        _fadeOutUiObject.SetActive(false);
        _savePoint.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (GameObject cam in _cineCams)
        {
            cam.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_flag < 1 && _player.gameObject.transform.position.y < 500)
        {
            FastenStartPoint();
        }
        if (_timerOn && _timeElapsed < _endTime)
        {
            _timeElapsed += Time.deltaTime;
        }
        else if (_timerOn && _timeElapsed > _endTime && _flag != 4)
        {
            SetNextFlag();
        }
        else if (_timerOn && _timeElapsed > _endTime && _flag == 4 && !_player.IsGrounded)
        {
            SetNextFlag();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !_cutsceneStarted)
        {
            _player = other.gameObject.GetComponent<PlayerController>();
            _cutsceneStarted = true;
            _player.SetCutSceneState(true);
            SetNextFlag();
        }

        if (other.gameObject.CompareTag("Player") && Managers.Game.CheckPlaying() == GameState.Playing)
        {
            Managers.Game.MoveToNextState();
            Text text = _endingUIObject.transform.Find("Panel/Text").GetComponent<Text>();
            text.text = _endingMessage;

            GameInputController.Instance.SetInputMode(InputMode.UI);
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _endingUIObject.SetActive(true);

        }
    }

    private void SetNextFlag()
    {
        Debug.Log(_flag);
        _flag++;
        switch (_flag)
        {
            case 1: //컷신 시작. _endTime동안 친구들이 도움닫기 하는 것을 바라봄 슬로프를 비춤. 
                Managers.Game.MoveToNextState();
                foreach (SphereFriend friend in _friends)
                {
                    friend.SetRunUp();
                }
                _player.SetLinearVelocity(Vector3.zero);
                _player.transform.LookAt(_cineCams[0].transform);

                // _cineCams[1].SetActive(true);
                _cineCams[0].SetActive(true);
                // _cineCams[1].GetComponent<CinemachineCamera>().Target.LookAtTarget = _cineCams[0].transform;
                _timerOn = true;
                _endTime = 2;
                break;
            case 2: //친구 팔로우 캠으로 전환. 친구가 속도를 얻기 위해 마저 뒤로 가다가 다시 앞으로 가는 것을 봄.
                _timeElapsed = 0;
                // _cineCams[1].SetActive(false);
                _endTime = 8;
                break;
            case 3: //플레이어 카메라 원복. 
                _timeElapsed = 0;
                _cineCams[0].SetActive(false);
                _cineCams[1].SetActive(true);
                _cineCams[1].GetComponent<CinemachineCamera>().Target.LookAtTarget = _player.gameObject.transform;

                _endTime = 1;
                break;
            case 4: // 이제 플레이어가 속도를 얻기 위해 _endTime 동안 뒤로 감. 
                _timeElapsed = 0;
                _player.SetLinearVelocity(Vector3.forward * 75);
                _endTime = 9f;
                break;
            case 5: //계속 뒤로 가다가 뒤를 보지 못한 플레이어가 떨어짐. 떨어질 때 팔로우 캠 위치에서 새 카메라로 전환.

                _cineCams[2].transform.position = _cineCams[1].transform.position;
                _cineCams[2].transform.rotation = _cineCams[1].transform.rotation;

                _cineCams[1].SetActive(false);
                _cineCams[2].SetActive(true);
                _player.SetLinearVelocity(Vector3.zero);
                break;
            case 6:
                //떨어지는 플레이어를 뒤늦게 확인. 
                CinemachineCamera tempCineCam = _cineCams[2].GetComponent<CinemachineCamera>();
                tempCineCam.Target.TrackingTarget = _player.gameObject.transform;
                tempCineCam.Lens.FieldOfView = 30;
                _timeElapsed = 0;
                Physics.gravity = new Vector3(0, -100, 0);
                _endTime = 2.0f;
                break;
            case 7: //추적 시작. 
                StartCoroutine(FadeOutCoroutine());
                _timeElapsed = 0;
                _cineCams[2].SetActive(false);
                // _cineCams[3].SetActive(true);
                _endTime = 2f;
                break;
            case 8:

                _player.gameObject.transform.position = _startPos - new Vector3(0, 400, 0);
                // _cineCams[3].SetActive(false);
                Physics.gravity = new Vector3(0, -50, 0);
                Managers.Game.MoveToNextState();
                //Fade out
                _player.SetCutSceneState(false);
                _savePoint.SetActive(true);
                SetEndingFlag();
                break;
        }
    }

    private void SetEndingFlag()
    {
        Vector3 endingPointScale = new Vector3(30, 6, 30);
        transform.localScale = endingPointScale;
    }

    private void OnEndingUIButtonClicked()
    {
        _endingUIObject.SetActive(false);
        GameInputController.Instance.SetInputMode(InputMode.Player);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!_endingUIObject.activeSelf)
            return;

        OnEndingUIButtonClicked();
    }

    private void FastenStartPoint()
    {
        _cutsceneStarted = true;
        Managers.Game.MoveToNextState();
        foreach (SphereFriend friend in _friends)
        {
            friend.SetRunUp();
        }
        _player.SetLinearVelocity(Vector3.zero);
        _player.gameObject.transform.position = _startPos;
        _cineCams[2].transform.position = _startPos + new Vector3(0, 20, 30);
        _cineCams[2].transform.rotation = Quaternion.Euler(30, 180, 0);
        _cineCams[2].SetActive(true);
        _flag = 5;
        _timeElapsed = 10;
        _timerOn = true;
        SetNextFlag();
    }

    private IEnumerator FadeOutCoroutine()
    {
        _fadeOutUiObject.SetActive(true);
        Image image = _fadeOutUiObject.GetComponentInChildren<Image>();
        Color fadeColor = image.color;
        fadeColor.a = 0;
        image.color = fadeColor;
        float fadeTimeElapsed = 0;
        while (fadeTimeElapsed < _fadeTime)
        {
            fadeTimeElapsed += Time.deltaTime;
            fadeColor.a = Mathf.Lerp(0, 1, fadeTimeElapsed / _fadeTime);
            image.color = fadeColor;
            yield return null;
        }
        yield return new WaitForSeconds(_fadeTime / 2);
        fadeTimeElapsed = 0;
        while (fadeTimeElapsed < _fadeTime)
        {
            fadeTimeElapsed += Time.deltaTime;
            fadeColor.a = Mathf.Lerp(1, 0, fadeTimeElapsed / _fadeTime);
            image.color = fadeColor;
            yield return null;

        }
        _fadeOutUiObject.SetActive(false);
    }

    void OnDisable()
    {
        Debug.Log("Dest");
    }
}