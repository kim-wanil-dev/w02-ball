using UnityEngine;
using UnityEngine.UI;

public class JumpPanelController : MonoBehaviour
{
    private Image[] _currentJumps = new Image[5];
    private Image[] _maxJumps = new Image[5];

    private void Awake()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            string name = child.name;

            if (name.StartsWith("JumpOutline")
                && int.TryParse(name.Substring("JumpOutline".Length), out int i)
                && i >= 1 && i <= 5)
                _maxJumps[i - 1] = child.GetComponent<Image>();
            else if (name.StartsWith("Jump")
                && int.TryParse(name.Substring("Jump".Length), out int j)
                && j >= 1 && j <= 5)
                _currentJumps[j - 1] = child.GetComponent<Image>();
        }
    }

    public void SetCurrentJumps(int currentJumpCount)
    {
        currentJumpCount = Mathf.Clamp(currentJumpCount, 0, 5);

        for (int i = 0; i < _currentJumps.Length; i++)
        {
            if (_currentJumps[i] != null)
                _currentJumps[i].gameObject.SetActive(i < currentJumpCount);
        }
    }

    public void SetMaxJumps(int maxJumpCount)
    {
        maxJumpCount = Mathf.Clamp(maxJumpCount, 0, 5);

        for (int i = 0; i < _maxJumps.Length; i++)
        {
            if (_maxJumps[i] != null)
                _maxJumps[i].gameObject.SetActive(i < maxJumpCount);
        }
    }
}