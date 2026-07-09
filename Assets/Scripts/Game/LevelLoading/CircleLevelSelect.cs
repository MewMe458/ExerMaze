using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CircleLevelSelect : MonoBehaviour
{
    [SerializeField] private Button level6Radius;
    [SerializeField] private Button level12Radius;
    [SerializeField] private Button level18Radius;
    [SerializeField] private Button customCircleButton;

    public string levelName;

    void Awake()
    {
        if (level6Radius != null)
            level6Radius.onClick.AddListener(() => SelectLevel(12, 12));
        else
            Debug.LogWarning("Level 12x12 button not assigned");

        if (level12Radius != null)
            level12Radius.onClick.AddListener(() => SelectLevel(24, 24));
        else
            Debug.LogWarning("Level 24x24 button not assigned");

        if (level18Radius != null)
            level18Radius.onClick.AddListener(() => SelectLevel(36, 36));
        else
            Debug.LogWarning("Level 36x36 button not assigned");
            
        if (customCircleButton != null)
            customCircleButton.onClick.AddListener(() => ToCustomCircleScene());
        else
            Debug.LogWarning("Custom Random Level button not assigned");
    }

    private void SelectLevel(int width, int depth)
    {
        if (ScreenshotManager.Instance != null)
        {
            ScreenshotManager.Instance.CreateSessionSubfolder();
        }

        // Pass Circle Shape
        GameManager.Instance.SetMazeSize(width, depth, GameManager.MazeShape.Circle);
        SceneManager.LoadScene("RandomLevel");
    }

    private void ToCustomCircleScene()
    {
        SceneManager.LoadSceneAsync("CustomCircleMazeSize");
    }
}