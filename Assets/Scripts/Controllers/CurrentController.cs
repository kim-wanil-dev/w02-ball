using UnityEngine;

public class CurrentController : MonoBehaviour
{
    [SerializeField] private float _lightAcceleration = 40f;
    [SerializeField] private float _heavyAcceleration = 20f;
    [SerializeField] private float _maxFlowSpeed = 30f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;

        Vector3 flowDirection = transform.forward;

        float currentFlowSpeed =
            Vector3.Dot(rb.linearVelocity, flowDirection);

        if (currentFlowSpeed >= _maxFlowSpeed)
            return;

        float massT = Mathf.InverseLerp(
            1f,
            3f,
            Mathf.Pow(rb.mass, 1f / 3f)
        );

        float acceleration = Mathf.Lerp(
            _lightAcceleration,
            _heavyAcceleration,
            massT
        );

        float velocityIncrease =
            acceleration * Time.fixedDeltaTime;

        velocityIncrease = Mathf.Min(
            velocityIncrease,
            _maxFlowSpeed - currentFlowSpeed
        );

        rb.linearVelocity +=
            flowDirection * velocityIncrease;
    }
}
