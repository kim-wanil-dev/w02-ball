using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    private List<Rigidbody> _wings;

    void Awake()
    {
        _wings = new List<Rigidbody>();
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject.GetComponent<Wheel>() == null)
            {
                _wings.Add(rb);
            }
        }
    }

    public void SetRigidbodyConstraintsAllWings(bool bigScaleCollisionStay)
    {
        if (bigScaleCollisionStay)
        {
            foreach (Rigidbody rb in _wings)
            {
                rb.constraints = RigidbodyConstraints.None;
            }
        }
        else
        {
            foreach (Rigidbody rb in _wings)
            {
                rb.constraints = RigidbodyConstraints.FreezePosition;
            }
        }
    }
}
