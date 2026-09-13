using UnityEngine;

public class PrizeObjectManager : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Destroy(this.gameObject);
        }
    }
}
