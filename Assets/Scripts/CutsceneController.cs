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
        else if (_timerOn && _timeElapsed > _endTime && _flag != 2)
        {
            SetNextFlag();
        }
        else if (_timerOn && _timeElapsed > _endTime && _flag == 2 && !_player.IsGrounded)
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
            case 1: //컷신 시작. _endTime동안 슬로프를 비춤. 
                foreach (SphereFriend friend in friends)
                {
                    friend.SetRoute();
                }
                _player.SetLinearVelocity(Vector3.zero);
                _player.transform.LookAt(_cineCams[0].transform);
                _cineCams[0].SetActive(true);
                _timerOn = true;
                _endTime = 5;
                break;
            case 2: //플레이어 팔로우 캠으로 전환 플레이어가 속도를 얻기 위해 _endTime 동안 뒤로감. 
                _timeElapsed = 0;
                _cineCams[0].SetActive(false);
                _cineCams[1].SetActive(true);
                _player.SetLinearVelocity(Vector3.forward * 75);
                _endTime = 8.5f;
                break;
            case 3: //계속 뒤로 가다가 뒤를 보지 못한 플레이어가 떨어짐. 떨어질 때 팔로우 캠 위치에서 새 카메라로 전환.
                _timeElapsed = 0;
                _cineCams[2].transform.position = _cineCams[1].transform.position;
                _cineCams[2].transform.rotation = _cineCams[1].transform.rotation;

                _cineCams[1].SetActive(false);
                _cineCams[2].SetActive(true);
                _player.SetLinearVelocity(Vector3.zero);
                _endTime = 1f;
                break;
            case 4: //떨어지는 플레이어를 뒤늦게 확인. 
                CinemachineCamera tempCineCam = _cineCams[2].GetComponent<CinemachineCamera>();
                tempCineCam.Target.TrackingTarget = _player.gameObject.transform;
                tempCineCam.Lens.FieldOfView = 30;
                _timeElapsed = 0;
                _endTime = 1.0f;
                break;
            case 5: //추적 시작. 
                _timeElapsed = 0;
                _cineCams[2].SetActive(false);
                // _cineCams[3].SetActive(true);
                _endTime = 2f;
                break;
            case 6:
                // _cineCams[3].SetActive(false);
                _player.SetCutSceneState(false);
                break;
        }
    }
}
