using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SquareLevelSelect : MonoBehaviour
{
    [SerializeField] private Button level12x12Button;
    [SerializeField] private Button level24x24Button;
    [SerializeField] private Button level36x36Button;
    [SerializeField] private Button customSquareButton;

    public string levelName;

    void Awake()
    {
        if (level12x12Button != null)
            level12x12Button.onClick.AddListener(() => SelectLevel(12, 12));
        else
            Debug.LogWarning("Level 12x12 button not assigned");

        if (level24x24Button != null)
            level24x24Button.onClick.AddListener(() => SelectLevel(24, 24));
        else
            Debug.LogWarning("Level 24x24 button not assigned");

        if (level36x36Button != null)
            level36x36Button.onClick.AddListener(() => SelectLevel(36, 36));
        else
            Debug.LogWarning("Level 36x36 button not assigned");
            
        if (customSquareButton != null)
            customSquareButton.onClick.AddListener(() => ToCustomSquareScene());
        else
            Debug.LogWarning("Custom Random Level button not assigned");
    }

    private void SelectLevel(int width, int depth)
    {
        if (ScreenshotManager.Instance != null)
        {
            ScreenshotManager.Instance.CreateSessionSubfolder();
        }

        // Pass Square Shape
        GameManager.Instance.SetMazeSize(width, depth, GameManager.MazeShape.Square);
        SceneManager.LoadScene("RandomLevel");
    }

    private void ToCustomSquareScene()
    {
        SceneManager.LoadSceneAsync("CustomSquareMazeSize");
    }
}