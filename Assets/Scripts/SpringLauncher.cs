using UnityEngine;

public class SpringLauncher : MonoBehaviour
{
    private enum SpringState
    {
        Idle,
        Compressing,
        Charged,
        Returning
    }

    [Header("References")]
    [SerializeField] private Rigidbody _topPlateRb;
    [SerializeField] private Transform _springPivot;
    [SerializeField] private TriggerDetector _triggerDetector;

    [Header("Compression")]
    [SerializeField] private float _springLength = 10f;

    //질량 비례 발판 압축하는 속도
    [SerializeField] private float _compressionSpeedPerMass = 1f;

    [Header("Charge")]
    [SerializeField] private float _launchWaitDuration = 1.5f;
    [SerializeField] private float _currentChargeGuage = 0f;

    [Header("Launch")]
    [SerializeField] private float _launchForce = 10f;
    [SerializeField] private ForceMode _launchForceMode = ForceMode.Impulse;

    [Header("Return")]
    //mass가 1일 때 기준 시간. 높으면 duration이 늘어남 
    [SerializeField] private float _basicReturnDuration = 0.2f;
    private float _returnDuration = 0.2f;
    private float _returnTimer;
    private float _maxChargeGuage = 100f;
    private float _launchWaitTimer;
    private SpringState _currentState;
    private Rigidbody _targetRb;
    private Vector3 _originalSpringScale;
    private Vector3 _originalTopPlateWorldPosition;
    private Vector3 _currentTopPlateWorldPosition;

    private void Awake()
    {
        _topPlateRb.isKinematic = true;
        _topPlateRb.useGravity = false;

        _originalSpringScale = _springPivot.localScale;
        _originalTopPlateWorldPosition = _topPlateRb.transform.position;

        ChangeState(SpringState.Idle);
    }

    private void OnEnable()
    {
        _triggerDetector.OnTargetEntered += HandleTargetEntered;
        _triggerDetector.OnTargetExited += HandleTargetExited;
    }
    private void OnDisable()
    {
        _triggerDetector.OnTargetEntered -= HandleTargetEntered;
        _triggerDetector.OnTargetExited -= HandleTargetExited;
    }
    private void HandleTargetEntered(Rigidbody rb)
    {
        _targetRb = rb;
    }
    private void HandleTargetExited(Rigidbody rb)
    {
        if (_targetRb == rb)
        {
            _targetRb = null;
        }
    }

    private void FixedUpdate()
    {
        switch (_currentState)
        {
            case SpringState.Idle:
                Idle();
                break;

            case SpringState.Compressing:
                Compressing();
                break;

            case SpringState.Charged:
                Charged();
                break;

            case SpringState.Returning:
                Returning();
                break;
        }
    }


    private void Idle()
    {
        if (_targetRb != null)
        {
            ChangeState(SpringState.Compressing);
        }
    }

    private void Compressing()
    {
        if (_targetRb == null)
        {
            ChangeState(SpringState.Returning);
            return;
        }

        float compressionSpeed =
            _compressionSpeedPerMass * _targetRb.mass;
        _currentChargeGuage += compressionSpeed * Time.fixedDeltaTime;
        float scaleY = (100f - _currentChargeGuage) / 100f;
        _springPivot.localScale = new Vector3(1f, scaleY, 1f);
        Vector3 targetPosition = Vector3.Lerp(_originalTopPlateWorldPosition, _originalTopPlateWorldPosition + Vector3.down * _springLength, _currentChargeGuage / 100f);
        _topPlateRb.MovePosition(targetPosition);
        if (_currentChargeGuage >= _maxChargeGuage)
        {
            ChangeState(SpringState.Charged);
        }
    }

    private void Charged()
    {
        if (_targetRb == null)
        {
            ChangeState(SpringState.Returning);
            return;
        }
        _launchWaitTimer += Time.fixedDeltaTime;
        if (_launchWaitTimer >= _launchWaitDuration)
        {
            Launch();

            ChangeState(SpringState.Returning);
        }
    }

    private void Launch()
    {
        if (_targetRb == null)
            return;

    }


    private void Returning()
    {
        float returnSpringSpeed = 1f / _returnDuration;
        _returnTimer += Time.fixedDeltaTime;

        Vector3 scale = _springPivot.localScale;
        scale.y = Mathf.MoveTowards(
            scale.y,
            _originalSpringScale.y,
            returnSpringSpeed * Time.fixedDeltaTime
        );
        _springPivot.localScale = new Vector3(1f, scale.y, 1f);
        Vector3 targetPosition = Vector3.Lerp(_currentTopPlateWorldPosition, _originalTopPlateWorldPosition, _returnTimer / _returnDuration);
        _topPlateRb.MovePosition(targetPosition);
        _topPlateRb.MovePosition(targetPosition);
        _springPivot.localScale = scale;

        if (_returnTimer >= _returnDuration)
        {
            _springPivot.localScale = _originalSpringScale;
            _topPlateRb.transform.position = _originalTopPlateWorldPosition;
            ChangeState(SpringState.Idle);
        }
    }

    private void ChangeState(SpringState newState)
    {
        _currentState = newState;

        switch (newState)
        {
            case SpringState.Idle:
                _returnTimer = 0f;
                break;

            case SpringState.Compressing:
                break;

            case SpringState.Charged:
                _launchWaitTimer = 0f;
                _currentChargeGuage = 0f;
                break;

            case SpringState.Returning:
                _launchWaitTimer = 0f;
                _currentChargeGuage = 0f;
                _currentTopPlateWorldPosition = _topPlateRb.transform.position;
                if (_targetRb == null)
                {
                    _returnDuration = _basicReturnDuration;
                    return;
                }
                _returnDuration = _basicReturnDuration * Mathf.Sqrt(_targetRb.mass);
                break;
        }
    }

}