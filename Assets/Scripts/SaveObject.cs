using UnityEngine;

public class SaveObject : MonoBehaviour
{
    [SerializeField] private Material _unregisteredMaterial;
    [SerializeField] private Material _registeredMaterial;
    public Renderer _renderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        Renderer renderer = GetComponent<Renderer>();
        renderer.material = _registeredMaterial;

        if (player.registeredSaveObject == null)
        {
            UIManager.Instance.ShowTutorial(TutorialSequence.FirstSaveGuide);
            this._renderer.material = _registeredMaterial;

        }
        else if (player.registeredSaveObject != this)
        {
            player.registeredSaveObject._renderer.material = _unregisteredMaterial;
            this._renderer.material = _registeredMaterial;
            player.registeredSaveObject = this;
        }

    }


}
