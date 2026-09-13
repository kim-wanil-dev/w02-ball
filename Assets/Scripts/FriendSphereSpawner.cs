using UnityEngine;

public class FriendSphereSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _friendSphere;
    [SerializeField] private float spawnDelay = 1;
    [SerializeField] private float spawnCountPerSpawnDelay;
    private float _timeElapsed = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        _timeElapsed += Time.deltaTime;
        if (_timeElapsed > spawnDelay)
        {
            _timeElapsed = 0;
            SpawnFriendSphere();
        }

    }

    private void SpawnFriendSphere()
    {
        for (int i = 0; i < spawnCountPerSpawnDelay; i++)
        {
            Vector3 spawnPos = transform.position;
            spawnPos.x = transform.position.x + Random.Range(-30, 30);
            GameObject sphere = Instantiate(_friendSphere, spawnPos, Quaternion.identity, transform);
            sphere.GetComponent<MeshRenderer>().material.color = GetRandomColor();
        }
    }

    private Color GetRandomColor()
    {
        return Random.ColorHSV();
    }
}
