using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadLevels : MonoBehaviour
{
    private void Start()
    {
        // Find all buttons in the scene and add listener dynamically
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            // Only add listener to LevelButton tagged buttons
            if (button.CompareTag("LevelButton"))
            {
                button.onClick.AddListener(() => OnLevelButtonClicked(button));
            }
            else continue;
            
        }
    }

    private void OnLevelButtonClicked(Button clickedButton)
    {
        // Get the button text (number)
        TMP_Text buttonText = clickedButton.GetComponentInChildren<TMP_Text>();
        if (buttonText == null)
        {
            Debug.LogError("No TMP_Text found on button!");
            return;
        }

        string levelNumber = buttonText.text.Trim(); // Get the button number (1-5)

        // Get the current scene name
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Remove "Select" from the name to get the base level name
        string baseLevelName = currentSceneName.Replace("Select", "");

        // Create the target level name (e.g., RectLevel1, TriLevel3)
        string levelToLoad = $"{baseLevelName}{levelNumber}";

        // Load the scene
        Debug.Log($"Loading Scene: {levelToLoad}");
        SceneManager.LoadScene(levelToLoad);
    }
}
