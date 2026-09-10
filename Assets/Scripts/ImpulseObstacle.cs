using UnityEngine;

public class ImpulseObstacle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float requireImpulseMagnitude = 150f;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnCollisionEnter(Collision other)
    {
        // 충돌 시 발생한 총 충격량 벡터
        if (other.gameObject.CompareTag("Player"))

        {
            Vector3 impulse = other.impulse;
            float impulseMagnitude = impulse.magnitude; // 충격량의 크기
            Debug.Log("충격량 크기: " + impulseMagnitude);
            if (impulseMagnitude > requireImpulseMagnitude)
            {
                gameObject.SetActive(false);
                Invoke("Reset", 1f);
            }
        }

    }

    private void Reset()
    {
        gameObject.SetActive(true);
    }
}
