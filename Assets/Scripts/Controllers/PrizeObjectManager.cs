using UnityEngine;
using UnityEngine.UI;

public class PrizeObjectManager : MonoBehaviour
{
    private GameObject _prizeUI;
    private bool _isTouched;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (gameObject.CompareTag("Prize"))
        {
            Destroy(gameObject);
            return;
        }

        if (_prizeUI == null)
            _prizeUI = Instantiate(Managers.Game.PrizeUI);

        if (!_isTouched)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.material.color = new Color(0, 0, 0);
            Button button = _prizeUI.GetComponentInChildren<Button>();
            button.onClick.AddListener(OnPrizeUIButtonClicked);
        }
        _isTouched = true;

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        _prizeUI.SetActive(true);
    }

    private void OnPrizeUIButtonClicked()
    {
        _prizeUI.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
    }
}
