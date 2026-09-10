using UnityEngine;

[CreateAssetMenu(fileName = "BallStat", menuName = "Scriptable Objects/BallStat")]
public class BallStat : ScriptableObject
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float moveAcceleration;
    [SerializeField] private float jumpForce;
    [SerializeField] private float sphereRadius;
    [SerializeField] private float mass;
    [SerializeField] private float bounciness;

    public float MoveSpeed => moveSpeed;
    public float MoveAcceleration => moveAcceleration;
    public float JumpForce => jumpForce;
    public float SphereRadius => sphereRadius;
    public float Mass => mass;
    public float Bounciness => bounciness;
}
