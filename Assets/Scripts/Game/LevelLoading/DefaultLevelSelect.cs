using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DefaultLevelSelect : MonoBehaviour
{
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;
    [SerializeField] private Button level4Button;
    [SerializeField] private Button level5Button;

    void Awake()
    {
        if (level1Button != null)
            level1Button.onClick.AddListener(() => LoadLevel("Level1"));
        else
            Debug.LogWarning("Level 1 button not assigned");

        if (level2Button != null)
            level2Button.onClick.AddListener(() => LoadLevel("Level2"));
        else
            Debug.LogWarning("Level 2 button not assigned");

        if (level3Button != null)
            level3Button.onClick.AddListener(() => LoadLevel("Level3"));
        else
            Debug.LogWarning("Level 3 button not assigned");
        if (level4Button != null)
            level4Button.onClick.AddListener(() => LoadLevel("Level4"));
        else
            Debug.LogWarning("Level 4 button not assigned");
        if (level5Button != null)
            level5Button.onClick.AddListener(() => LoadLevel("Level5"));
        else
            Debug.LogWarning("Level 5 button not assigned");
    }

    private void LoadLevel(string levelName)
    {
        // Store level name in GameManager
        GameManager.Instance.SetCurrentLevelName(levelName);

        // Load DefaultLevel scene
        SceneManager.LoadScene("DefaultLevel");
    }
}