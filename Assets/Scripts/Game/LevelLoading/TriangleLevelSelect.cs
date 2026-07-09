using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TriangleLevelSelect : MonoBehaviour
{
    [SerializeField] private Button level6BaseButton;
    [SerializeField] private Button level12BaseButton;
    [SerializeField] private Button level18BaseButton;
    [SerializeField] private Button customTriangleButton;

    public string levelName;

    void Awake()
    {
        if (level6BaseButton != null)
            level6BaseButton.onClick.AddListener(() => SelectLevel(12, 12));
        else
            Debug.LogWarning("Level 12x12 Triangle button not assigned");

        if (level12BaseButton != null)
            level12BaseButton.onClick.AddListener(() => SelectLevel(24, 24));
        else
            Debug.LogWarning("Level 24x24 Triangle button not assigned");

        if (level18BaseButton != null)
            level18BaseButton.onClick.AddListener(() => SelectLevel(36, 36));
        else
            Debug.LogWarning("Level 36x36 Triangle button not assigned");
            
        if (customTriangleButton != null)
            customTriangleButton.onClick.AddListener(() => ToCustomTriangleScene());
        else
            Debug.LogWarning("Custom Triangle Level button not assigned");
    }

    private void SelectLevel(int width, int depth)
    {
        if (ScreenshotManager.Instance != null)
        {
            ScreenshotManager.Instance.CreateSessionSubfolder();
        }

        // Pass Triangle Shape configuration
        GameManager.Instance.SetMazeSize(width, depth, GameManager.MazeShape.Triangle);
        SceneManager.LoadScene("RandomLevel");
    }

    private void ToCustomTriangleScene()
    {
        SceneManager.LoadSceneAsync("CustomTriangleMazeSize");
    }
}