using UnityEngine;

[CreateAssetMenu(fileName = "BallStat", menuName = "Scriptable Objects/BallStat")]
public class BallStat : ScriptableObject
{
    [Header("Move")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _moveAcceleration;
    [SerializeField] private float _moveResponseTime;
    [Header("Jump")]
    [SerializeField] private float _jumpForce;
    [Header("Gravity")]
    [SerializeField] private float _maxGravityVelocity;
    [Header("Property")]
    [SerializeField] private float _mass;
    [SerializeField] private float _bounciness;
    [SerializeField] private float _sphereRadius;

    public float MoveSpeed => _moveSpeed;
    public float MoveAcceleration => _moveAcceleration;
    public float MoveResponseTime => _moveResponseTime;
    public float JumpForce => _jumpForce;
    public float SphereRadius => _sphereRadius;
    public float Mass => _mass;
    public float Bounciness => _bounciness;
    public float MaxGravityVelocity => _maxGravityVelocity;
}
