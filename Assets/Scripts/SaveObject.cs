using UnityEngine;

public class SaveObject : MonoBehaviour
{
    [SerializeField] private Material _unregisteredMaterial;
    [SerializeField] private Material _registeredMaterial;
    private MeshRenderer _renderer;
    private PlayerController _player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        if (!GameObject.Find("Player").TryGetComponent(out _player))
        {
            Debug.LogError($"{_player.name} component is missing.", this);
            enabled = false;
            return;
        }
        if (!TryGetComponent(out _renderer))
        {
            Debug.LogError($"{_renderer} : renderer component is missing.", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {

    }

    private void OnEnable()
    {
        _player.OnRegistered += HandleRegisterSaveObject;
    }
    private void OnDisable()
    {
        _player.OnRegistered -= HandleRegisterSaveObject;
    }
    // Update is called once per frame
    void Update()
    {

    }
    private void HandleRegisterSaveObject()
    {
        Debug.Log("HandleRegisterSaveObject");
        if (_player.RegisteredSaveObject == this)
        {
            _renderer.material = _registeredMaterial;
        }
        else
        {
            _renderer.material = _unregisteredMaterial;
        }
    }
}
