using UnityEngine;
using UnityEngine.SceneManagement;

public class ContinueChoice : MonoBehaviour
{
    public void OnClickContinue()
    {
        // 1. Set the continuous session flag to true
        GameManager.Instance.IsContinuingSession = true;

        // Note: Steps and Time were already accumulated in GameManager 
        // by LevelManager.CompleteLevel() just before this screen appeared.

        Time.timeScale = 1f;

        // Clear the custom level references in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearCurrentLevelName();       
            GameManager.Instance.ClearCurrentCustomLevelPath(); 
        }

        // Load the RandomLevel scene again
        SceneManager.LoadScene("RandomLevel");
    }

    public void OnClickStop()
    {
        // 2. Explicitly tell GameManager the session is over
        GameManager.Instance.IsContinuingSession = false;

        // 3. Clear out session configurations inside ScreenshotManager completely
        if (ScreenshotManager.Instance != null)
        {
            ScreenshotManager.Instance.ResetSessionTracking();
        }

        // Unload this prompt and show the final summary screen
        SceneManager.UnloadSceneAsync("ContinueChoiceScene");
        SceneManager.LoadScene("LevelCompleteMenu", LoadSceneMode.Additive);
    }
}