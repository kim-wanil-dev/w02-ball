using System;
using UnityEngine;

public class TriggerDetector : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public event Action<Rigidbody> OnTargetEntered;
    public event Action<Rigidbody> OnTargetExited;
    [SerializeField] private LayerMask targetLayer;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;
        if ((targetLayer.value & (1 << rb.gameObject.layer)) == 0)
            return;
        OnTargetEntered?.Invoke(rb);
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb == null)
            return;

        OnTargetExited?.Invoke(rb);
    }
}
