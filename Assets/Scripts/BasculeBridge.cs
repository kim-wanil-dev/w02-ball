using UnityEngine;

public class BasculeBridge : MonoBehaviour
{
    [SerializeField] private GameObject _connectedWheel;
    [SerializeField] private float _maxTargetRotation = 720;
    [SerializeField] private float _startBridgeAngle = 70f;
    [SerializeField] private float _endBridgeAngle = 0f;
    [SerializeField] private float _springForce = 1000f;
    [SerializeField] private float _springDamp = 50f;

    private HingeJoint _hingeJoint;
    private Rigidbody _rb;
    private float _lastYAngle;
    private float _accumulatedRotation = 0;
    private bool _Initialized = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _hingeJoint = GetComponent<HingeJoint>();
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        _lastYAngle = _connectedWheel.transform.eulerAngles.y;
        _accumulatedRotation = 0f;
        SetBridgeZRotation(_startBridgeAngle);

        _Initialized = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!_Initialized) return;

        float currentYAngle = _connectedWheel.transform.eulerAngles.y;
        float deltaAngle = Mathf.DeltaAngle(_lastYAngle, currentYAngle);
        _accumulatedRotation += deltaAngle;
        _accumulatedRotation = Mathf.Clamp(_accumulatedRotation, 0f, _maxTargetRotation);
        _lastYAngle = currentYAngle;

        float rotationRatio = _accumulatedRotation / _maxTargetRotation;
        float targetBridgeAngle = Mathf.Lerp(_startBridgeAngle, _endBridgeAngle, rotationRatio);

        SetBridgeZRotation(targetBridgeAngle);
    }

    private void SetBridgeZRotation(float targetZAngle)
    {
        if (Mathf.Approximately(targetZAngle, _endBridgeAngle))
        {
            _hingeJoint.useSpring = false;
            _rb.useGravity = true;
            // JointLimits fixLimits = _hingeJoint.limits;
            // fixLimits.min = _endBridgeAngle - 0.1f;
            // fixLimits.max = _endBridgeAngle + 0.1f;
            // _hingeJoint.limits = fixLimits;
            return;
        }

        _rb.useGravity = false;
        _hingeJoint.useSpring = true;
        JointSpring spring = _hingeJoint.spring;
        spring.targetPosition = targetZAngle;
        spring.spring = _springForce;
        spring.damper = _springDamp;
        _hingeJoint.spring = spring;
    }
}
