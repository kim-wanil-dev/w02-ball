using System.Collections;
using UnityEngine;

public class StreamController : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Rigidbody rb = other.attachedRigidbody;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = Mathf.Max(velocity.y, -0.1f);
        rb.linearVelocity = velocity;
    }
}
