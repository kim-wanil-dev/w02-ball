using UnityEngine;

public class CutsceneController : MonoBehaviour
{
    [SerializeField] private GameObject[] _cineCams;
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
        else if (_timerOn && _timeElapsed < _endTime)
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
            _player.cutsceneStarted = true;
            SetNextFlag();
        }
    }

    private void SetNextFlag()
    {
        _flag++;
        switch (_flag)
        {
            case 1: //컷신 시작. _endTime동안 슬로프를 비춤. 
                _cineCams[0].SetActive(true);
                _timerOn = true;
                _endTime = 5;
                break;
            case 2: //플레이어 팔로우 캠으로 전환 플레이어가 속도를 얻기 위해 _endTime 동안 뒤로감. 
                _timeElapsed = 0;
                _cineCams[0].SetActive(false);
                _player.transform.Translate(Vector3.back * 5);
                _endTime = 2;
                break;
            case 3: //돌부리 캠으로 전환. 돌부리에 걸리는 것을 목격.
                _cineCams[1].SetActive(true);
                _timeElapsed = 0;
                _endTime = 1;
                break;
            case 4: //돌부리에 걸려 뒤로 빠르게 넘어짐. _endTime 뒤에 절벽 뷰로 카메라 전환. 
                _player.transform.Translate(Vector3.back * 10);
                _timeElapsed = 0;
                _endTime = 0.5f;
                break;
            case 5: //절벽 가장자리에서 떨어지는 플레이어를 관찰. _endTime 후 추적 시작. 
                _cineCams[1].SetActive(false);
                _cineCams[2].SetActive(true);
                _endTime = 1f;
                break;
            case 6:
                _cineCams[2].SetActive(false);
                break;
        }
    }
}
