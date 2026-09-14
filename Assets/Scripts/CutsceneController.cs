using Unity.Cinemachine;
using UnityEngine;

public class CutsceneController : MonoBehaviour
{
    [SerializeField] private GameObject[] _cineCams;
    [SerializeField] private SphereFriend[] friends;
    private PlayerController _player;
    private bool _cutsceneStarted = false;
    private bool _timerOn = false;
    private float _timeElapsed = 0;
    private float _endTime = 3;
    private int _flag = 0;

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

        if (_cutsceneStarted)
        {
            Debug.Log("cutscene" + _player.IsGrounded);
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
    }

    private void SetNextFlag()
    {
        _flag++;
        switch (_flag)
        {
            case 1: //컷신 시작. _endTime동안 친구들이 도움닫기 하는 것을 바라봄 슬로프를 비춤. 
                Managers.Game.MoveToNextState();
                foreach (SphereFriend friend in friends)
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
                _timeElapsed = 0;
                _cineCams[2].SetActive(false);
                // _cineCams[3].SetActive(true);
                _endTime = 2f;
                break;
            case 8:
                // _cineCams[3].SetActive(false);
                Physics.gravity = new Vector3(0, -50, 0);
                Managers.Game.MoveToNextState();
                _player.SetCutSceneState(false);
                break;
        }
    }
}