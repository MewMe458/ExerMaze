using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class BaseMenuManager : MonoBehaviour
{
    protected LevelManager levelManager;
    protected GPXFileSaver gpxSaver;

    protected virtual void Start()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        gpxSaver = FindAnyObjectByType<GPXFileSaver>();

        if (levelManager == null)
        {
            Debug.LogWarning($"{this.GetType().Name}: LevelManager not found, using manual restart");
        }

        if (gpxSaver == null)
        {
            Debug.LogError($"{this.GetType().Name}: GPXFileSaver not found!");
        }
    }

    public void OnRestartButtonPressed()
    {
        if (levelManager != null)
        {
            levelManager.RestartLevel();
        }
        else
        {
            RestartLevelManually();
        }
    }

    private void RestartLevelManually()
    {
        Time.timeScale = 1f; // Ensure time is running again
        SceneManager.UnloadSceneAsync(GetMenuSceneName()); // Hide menu

        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"BaseMenuManager.RestartLevelManually: CurrentScene = {currentScene}, CurrentLevelName = {GameManager.Instance.CurrentLevelName}");

        if (currentScene == "DefaultLevel")
        {
            // Preserve level name
            string levelName = GameManager.Instance.CurrentLevelName;
            if (!string.IsNullOrEmpty(levelName))
            {
                GameManager.Instance.SetCurrentLevelName(levelName);
                Debug.Log($"Restarting DefaultLevel with LevelName: {levelName}");
            }
            else
            {
                Debug.LogWarning("GameManager.CurrentLevelName is null, falling back to LevelSelectMenu");
                SceneManager.LoadScene("LevelSelectMenu");
                return;
            }
        }
        else if (currentScene == "CustomLevel")
        {
            // Preserve custom level path
            string customLevelPath = GameManager.Instance.CurrentCustomLevelPath;
            if (!string.IsNullOrEmpty(customLevelPath))
            {
                GameManager.Instance.CurrentCustomLevelPath = customLevelPath;
                Debug.Log($"Restarting CustomLevel with Path: {customLevelPath}");
            }
            else
            {
                Debug.LogWarning("GameManager.CurrentCustomLevelPath is null, falling back to CustomLevelSelect");
                SceneManager.LoadScene("CustomLevelSelect");
                return;
            }
        }

        SceneManager.LoadScene(currentScene); // Reload current level
        //BLEManager.Instance?.bleConnect?.UpdateSensorStateOnBLE("start"); // Restart sensors
        Debug.Log($"Level Restarted from {this.GetType().Name}");
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // Reset time in case game was paused
        SceneManager.UnloadSceneAsync(GetMenuSceneName()); // Unload menu

        // Clear CurrentLevelName when returning to menu
        GameManager.Instance.ClearCurrentLevelName();
        GameManager.Instance.ClearCurrentCustomLevelPath();

        SceneManager.LoadScene("Menu"); // Load main menu scene

        GameManager.Instance.SetGameState(GameManager.GameState.Menu);
        BLEManager.Instance?.bleConnect?.UpdateSensorStateOnBLE("stop");

        Debug.Log($"Returning to Main Menu from {this.GetType().Name}");
    }

    public void OnSaveGPXButtonPressed()
    {
        if (gpxSaver != null)
        {
            gpxSaver.SaveGPXFileUWP();
            Debug.Log($"Saving GPX file from {this.GetType().Name}.");
        }
        else
        {
            Debug.LogError("GPXFileSaver instance not found!");
        }
    }

    public void QuitGame()
    {
        // Clear CurrentLevelName and stop BGM before quitting
        BLEManager.Instance?.bleConnect?.Disconnect();
        GameManager.Instance.ClearCurrentLevelName();
        GameManager.Instance.ClearCurrentCustomLevelPath();
        SoundManager.Instance.StopBGM();
        Application.Quit();
    }

    protected abstract string GetMenuSceneName();
}