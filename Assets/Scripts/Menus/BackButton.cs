using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    [SerializeField] Button backButton;
    void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(NavigateScenes);
        }
        else
        {
            Debug.Log("No back button found in " + SceneManager.GetActiveScene().name);
        }
    }

    void NavigateScenes()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene.EndsWith("LevelSelect"))
        {
            SceneManager.LoadScene("LevelSelectMenu");
        }
        else
        {
            switch (currentScene)
            {
                case "LevelSelectMenu":
                case "AboutScene":
                case "SettingsScene":
                    SceneManager.LoadScene("Menu");
                    break;
                default:
                    Debug.Log("No back function for this scene");
                    break;
            }
        }
    }
}
