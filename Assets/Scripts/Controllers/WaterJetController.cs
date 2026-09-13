using System.Collections;
using UnityEngine;

public class WaterJetController : MonoBehaviour
{
    private enum State
    {
        Prepare,
        Jet,
        Idle
    }

    [SerializeField] private float _prepareDuration = 3f;
    [SerializeField] private float _jetDuration = 3f;
    [SerializeField] private float _idleDuration = 3f;

    [SerializeField] private float _acceleration = 40f;
    [SerializeField] private float _maxUpVelocity = 100f;

    private State _state;

    private void Start()
    {
        StartCoroutine(StateRoutine());
    }

    private IEnumerator StateRoutine()
    {
        while (true)
        {
            _state = State.Prepare;
            yield return new WaitForSeconds(_prepareDuration);

            _state = State.Jet;
            yield return new WaitForSeconds(_jetDuration);

            _state = State.Idle;
            yield return new WaitForSeconds(_idleDuration);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_state != State.Jet)
            return;

        if (!other.CompareTag("Player"))
            return;

        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;

        float currentUpVelocity =
            Vector3.Dot(rb.linearVelocity, Vector3.up);

        if (currentUpVelocity >= _maxUpVelocity)
            return;

        float velocityIncrease =
            _acceleration * Time.fixedDeltaTime;

        velocityIncrease = Mathf.Min(
            velocityIncrease,
            _maxUpVelocity - currentUpVelocity
        );

        rb.linearVelocity +=
            Vector3.up * velocityIncrease;

        Debug.Log($"Jet acc: {Vector3.up * velocityIncrease}");
    }
}
