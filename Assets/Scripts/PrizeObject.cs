using UnityEngine;

public class PrizeObject : MonoBehaviour
{
    [SerializeField] private Material _guideMaterial;
    private bool isFisrtObject = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
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
        if (isFisrtObject)
        {
            UIManager.Instance.ShowTutorial(TutorialSequence.Guide);
            return;
        }

        if (UIManager.Instance.isGetFirstPrize)
        {
            player.AcquireFirstPrize();
            this.isFisrtObject = true;
            Renderer renderer = GetComponent<Renderer>();
            renderer.material = _guideMaterial;
            UIManager.Instance.ShowTutorial(TutorialSequence.Greeting);
            UIManager.Instance.isGetFirstPrize = false;
            return;
        }

        player.AcquirePrize();
        Destroy(this.gameObject);

        if (player.MaxJumpCount == 1)
        {
            UIManager.Instance.ShowTutorial(TutorialSequence.SecondPirzeGuide);
        }


    }
}
