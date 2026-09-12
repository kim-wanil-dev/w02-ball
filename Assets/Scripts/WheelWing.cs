using UnityEngine;

public class WheelWing : MonoBehaviour
{
    private Wheel parent;

    void Awake()
    {
        parent = GetComponentInParent<Wheel>();
    }

    private void OnCollisionStay(Collision other)
    {
        if (other.gameObject.name == "Player")
        {
            if (other.gameObject.transform.localScale.x > 12)
            {
                parent.SetRigidbodyConstraintsAllWings(true);
            }
            else
            {
                parent.SetRigidbodyConstraintsAllWings(false);
            }

        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.name == "Player")
        {
            parent.SetRigidbodyConstraintsAllWings(false);
        }
    }

}
