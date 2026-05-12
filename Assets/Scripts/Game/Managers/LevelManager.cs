using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;
using UnityEngine.EventSystems;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public enum LevelState
    {
        Countdown,  // Countdown before the level starts
        Playing,    // Normal gameplay
        Paused,     // Game is paused
        Completed   // Level finished
    }

    public LevelState CurrentLevelState { get; private set; } = LevelState.Countdown;
    private LevelState previousLevelState;

    private string pauseMenuScene = "PauseMenu";
    private string completeMenuScene = "LevelCompleteMenu";

    private int score = 0;
    private int totalSteps = 0; // Step count for this level
    private int dogChaseCount = 0;
    private int dogBiteCount = 0;
    private int dogTamedCount = 0;
    private InventoryManager inventoryManager;
    private EventSystem levelEventSystem;
    private LevelUIManager uiManager;
    private LevelLoader levelLoader;
    private Timer timer;

    private void Awake()
    {
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("LevelManager: InventoryManager not found in scene");
        }

        levelEventSystem = FindFirstObjectByType<EventSystem>();
        if (levelEventSystem == null)
        {
            Debug.LogWarning("LevelManager: No EventSystem found in level scene");
        }

        uiManager = FindAnyObjectByType<LevelUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("LevelManager: LevelUIManager not found in scene");
        }

        levelLoader = FindAnyObjectByType<LevelLoader>();
        if (levelLoader == null)
        {
            Debug.LogError("LevelManager: LevelLoader not found in scene");
        }
        timer = FindAnyObjectByType<Timer>();
        if (timer == null)
        {
            Debug.LogError("LevelManager: Timer not found in scene");
        }
    }

    private void Start()
    {
        //StartCoroutine(BeginCountdown()); // For testing without BLE connection
        StartCoroutine(CheckBluetoothConnection());
    }

    private void OnDestroy()
    {
        if (BLEManager.Instance?.bleDataHandler != null)
        {
            BLEManager.Instance.bleDataHandler.OnStepReceived -= AddStep;
        }

        var dogNPCs = FindObjectsByType<DogNPCChase>(FindObjectsSortMode.None);
        foreach (var dog in dogNPCs)
        {
            dog.OnDogBite -= HandleDogBite;
            dog.OnDogTamed -= HandleDogTamed;
            dog.OnDogChaseStarted -= HandleDogChaseStarted;
            dog.OnDogChaseEnded -= HandleDogChaseEnded;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentLevelState == LevelState.Playing)
            {
                PauseGame();
            }
            else if (CurrentLevelState == LevelState.Paused)
            {
                ResumeGame();
            }
            else if (CurrentLevelState == LevelState.Countdown)
            {
                Debug.Log("LevelManager: Escape pressed during Countdown, returning to Menu");
                ReturnToMenu();
            }
        }
    }

    private void ReturnToMenu()
    {
        Debug.Log("LevelManager: Returning to Menu");
        uiManager.HideBluetoothConnectCheckUI(); // Hide UI via LevelUIManager
        GameManager.Instance.ClearCurrentLevelName();
        GameManager.Instance.ClearCurrentCustomLevelPath();
        GameManager.Instance.SetGameState(GameManager.GameState.Menu);
        BLEManager.Instance?.bleConnect?.UpdateSensorStateOnBLE("stop");
        SceneManager.LoadScene("Menu");
    }

    private IEnumerator CheckBluetoothConnection()
    {
        if (BLEManager.Instance == null || BLEManager.Instance.bleConnect == null || !BLEManager.Instance.bleConnect.IsDeviceConnected())
        {
            uiManager.ShowBluetoothConnectCheckUI();
            Debug.Log("LevelManager: Bluetooth not connected, showing message");
            yield return new WaitForSecondsRealtime(3f);
            Debug.Log("LevelManager: Auto-redirecting to Menu after 3 seconds");
            ReturnToMenu();
            yield break;
        }
        BLEManager.Instance.bleConnect.UpdateSensorStateOnBLE("stop"); // Ensure sensors are stopped before countdown
        Debug.Log("LevelManager: Bluetooth connected, starting countdown");
        CurrentLevelState = LevelState.Countdown;
        StartCoroutine(BeginCountdown());
    }

    private IEnumerator BeginCountdown()
    {
        yield return StartCoroutine(uiManager.StartCountdown());
        CurrentLevelState = LevelState.Playing;
        InitializeLevel();
    }

    private void InitializeLevel()
    {
        GameManager.Instance.SetGameState(GameManager.GameState.InGame);
        if (BLEManager.Instance != null && BLEManager.Instance.bleConnect != null)
        {
            BLEManager.Instance.bleConnect.UpdateSensorStateOnBLE("start"); // Enable sensors after countdown
        }

        // Reset the tracking to have first point at starting position
        GPXMovementTracker tracker = FindAnyObjectByType<GPXMovementTracker>();
        if (tracker != null)
        {
            tracker.ResetTracking();
        }

        else
        {
            Debug.LogError("LevelManager: BLEManager or BLEConnect not found, sensors will not be started");
        }

        if (inventoryManager != null)
        {
            inventoryManager.ClearItems();
        }

        uiManager.SetChallengeIconPanelVisibility(CheckIfLevelChallengeMode());

        if (timer != null)
        {
            timer.ResetTimer();
            timer.StartTimer();
        }

        if (BLEManager.Instance?.bleDataHandler != null)
        {
            BLEManager.Instance.bleDataHandler.OnStepReceived += AddStep;
        }

        var dogNPCs = FindObjectsByType<DogNPCChase>(FindObjectsSortMode.None);
        if (dogNPCs.Length == 0)
        {
            Debug.LogWarning("LevelManager: No DogNPCChase instances found in scene");
        }
        else
        {
            Debug.Log($"LevelManager: Found {dogNPCs.Length} DogNPCChase instances in scene");
        }
        foreach (var dog in dogNPCs)
        {
            dog.AllowMove = true;
            dog.OnDogBite += HandleDogBite;
            dog.OnDogTamed += HandleDogTamed;
            dog.OnDogChaseStarted += HandleDogChaseStarted;
            dog.OnDogChaseEnded += HandleDogChaseEnded;
        }

        uiManager.InitializeUI();
    }

    public void AddStep()
    {
        if (CurrentLevelState == LevelState.Playing)
        {
            totalSteps++;
            if (uiManager != null)
            {
                uiManager.UpdateStepCount(totalSteps);
            }
        }
    }

    public int GetFinalStepCount()
    {
        return GPXDataPersistence.TotalStepsAccumulated;
    }

    public bool CheckIfLevelChallengeMode()
    {
        if (levelLoader != null && levelLoader.CurrentMazeData != null)
        {
            return levelLoader.CurrentMazeData.mode == "Challenge";
        }
        return false;
    }

    public bool CheckIfLevelHasDog()
    {
        if (levelLoader != null && levelLoader.CurrentMazeData != null)
        {
            return levelLoader.CurrentMazeData.elements.Exists(e => e.type == "Dog");
        }
        return false;
    }

    public void IncrementDogBiteCount()
    {
        if (CurrentLevelState == LevelState.Playing)
        {
            dogBiteCount++;
            if (uiManager != null)
            {
                uiManager.UpdateDogBiteCount(dogBiteCount);
            }
            Debug.Log($"Dog bite count incremented to {dogBiteCount}");
        }
    }

    public void IncrementDogTamedCount()
    {
        if (CurrentLevelState == LevelState.Playing)
        {
            dogTamedCount++;
            if (uiManager != null)
            {
                uiManager.UpdateDogTamedCount(dogTamedCount);
            }
            Debug.Log($"Dog tamed count incremented to {dogTamedCount}");
        }
    }

    private void HandleDogBite()
    {
        IncrementDogBiteCount();
        uiManager?.ShowLevelMessage("You got bitten by a dog!");
    }

    private void HandleDogTamed()
    {
        IncrementDogTamedCount();
        uiManager?.ShowLevelMessage("You just tamed a dog!");
    }

    private void HandleDogChaseStarted()
    {
        if (CurrentLevelState == LevelState.Playing)
        {
            dogChaseCount++;
            if (uiManager != null)
            {
                uiManager.UpdateDogChaseCount(dogChaseCount);
                uiManager.ShowLevelMessage("A dog is chasing you!");
            }
            Debug.Log($"Dog chase count incremented to {dogChaseCount}");
        }
    }

    private void HandleDogChaseEnded()
    {
        if (CurrentLevelState == LevelState.Playing)
        {
            dogChaseCount = Mathf.Max(0, dogChaseCount - 1);
            if (uiManager != null)
            {
                uiManager.UpdateDogChaseCount(dogChaseCount);
                uiManager.ShowLevelMessage("Dog has stopped chasing you.");
            }
            Debug.Log($"Dog chase count decremented to {dogChaseCount}");
        }
    }

    public int GetDogChaseCount()
    {
        return dogChaseCount;
    }

    public void PauseGame()
    {
        if (CurrentLevelState != LevelState.Playing)
            return;

        if (levelEventSystem != null)
        {
            levelEventSystem.enabled = false; // Disable main level EventSystem
            Debug.Log("Disabled level EventSystem for PauseMenu");
        }
        SceneManager.LoadScene(pauseMenuScene, LoadSceneMode.Additive);
        previousLevelState = CurrentLevelState;
        CurrentLevelState = LevelState.Paused;
        Cursor.visible = true; // Show cursor
        Cursor.lockState = CursorLockMode.None; // Unlock cursor
        if (timer != null)
        {
            timer.StopTimer();
        }
        Time.timeScale = 0f; // Stop time when paused
        if (BLEManager.Instance != null && BLEManager.Instance.bleConnect != null)
        {
            BLEManager.Instance.bleConnect.UpdateSensorStateOnBLE("stop"); // Pause sensors
        }
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        Cursor.visible = false; // Hide cursor
        Cursor.lockState = CursorLockMode.None; // Ensure consistent state
        SceneManager.UnloadSceneAsync(pauseMenuScene);
        CurrentLevelState = previousLevelState;
        if (timer != null)
        {
            timer.StartTimer();
        }
        Time.timeScale = 1f; // Resume time
        if (BLEManager.Instance != null && BLEManager.Instance.bleConnect != null)
        {
            BLEManager.Instance.bleConnect.UpdateSensorStateOnBLE("start"); // Resume sensors
        }
        if (levelEventSystem != null)
        {
            levelEventSystem.enabled = true; // Re-enable main level EventSystem
            Debug.Log("Re-enabled main level EventSystem");
        }
        Debug.Log("Game Resumed");
    }

    public void RestartLevel()
    {
        // Preserve CurrentLevelName or CurrentCustomLevelPath
        string levelName = GameManager.Instance.CurrentLevelName;
        string customLevelPath = GameManager.Instance.CurrentCustomLevelPath;
        Debug.Log($"LevelManager.RestartLevel: CurrentLevelName = {levelName}, CurrentCustomLevelPath = {customLevelPath}");
        if (!string.IsNullOrEmpty(customLevelPath))
        {
            GameManager.Instance.CurrentCustomLevelPath = customLevelPath;
        }
        else if (!string.IsNullOrEmpty(levelName))
        {
            GameManager.Instance.SetCurrentLevelName(levelName);
        }
        else
        {
            Debug.LogWarning("LevelManager.RestartLevel: Both CurrentLevelName and CurrentCustomLevelPath are null, falling back to LevelSelectMenu");
            SceneManager.LoadScene("LevelSelectMenu");
            return;
        }
        if (timer != null)
        {
            timer.ResetTimer();
        }
        Time.timeScale = 1f; // Ensure time is running again

        // Unload pause menu if it's loaded
        if (SceneManager.GetSceneByName(pauseMenuScene).isLoaded)
        {
            SceneManager.UnloadSceneAsync(pauseMenuScene);
        }

        // Unload level complete menu if it's loaded
        if (SceneManager.GetSceneByName(completeMenuScene).isLoaded)
        {
            SceneManager.UnloadSceneAsync(completeMenuScene);
        }

        dogBiteCount = 0; // Reset bite count
        dogTamedCount = 0; // Reset tamed count
        if (uiManager != null)
        {
            uiManager.UpdateDogBiteCount(dogBiteCount);
            uiManager.UpdateDogTamedCount(dogTamedCount);
        }

        // Reload current level
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);

        // Reset GPX tracker after scene reload
        GPXMovementTracker tracker = FindAnyObjectByType<GPXMovementTracker>();
        if (tracker != null)
        {
            tracker.ResetTracking();
        }

        Debug.Log($"Level Restarted: {currentScene}");
    }

    public void CompleteLevel()
    {
        if (CurrentLevelState != LevelState.Playing)
            return;

        GPXMovementTracker tracker = FindAnyObjectByType<GPXMovementTracker>();
        if (tracker != null) tracker.SaveToPersistence();

        GameManager.Instance.AccumulatedSteps += totalSteps;
        GPXDataPersistence.TotalScoreAccumulated += score;
        GameManager.Instance.AccumulatedTime += timer.GetElapsedTime();

        CurrentLevelState = LevelState.Completed;
        Time.timeScale = 0f;

        SceneManager.LoadScene("ContinueChoiceScene", LoadSceneMode.Additive);

        // Debug.Log("Level Completed");
        // CurrentLevelState = LevelState.Completed;
        // if (BLEManager.Instance != null && BLEManager.Instance.bleConnect != null)
        // {
        //     BLEManager.Instance.bleConnect.UpdateSensorStateOnBLE("stop"); // Stop sensors
        // }
        // if (levelEventSystem != null)
        // {
        //     levelEventSystem.enabled = false; // Disable main level EventSystem
        //     Debug.Log("Disabled main level EventSystem for LevelCompleteMenu");
        // }
        // Cursor.visible = true; // Show cursor
        // Cursor.lockState = CursorLockMode.None; // Unlock cursor
        // SceneManager.LoadScene(completeMenuScene, LoadSceneMode.Additive);
        // if (timer != null)
        // {
        //     timer.StopTimer();
        // }
        // Time.timeScale = 0f; // Stop the game when completed
    }

    public float GetFinalTime()
    {
        if (timer != null)
        {
            return timer.GetElapsedTime();
        }
        Debug.LogWarning("LevelManager: Timer not found, returning 0 time");
        return 0f;
    }

    public void AddScore(int amount)
    {
        if (CurrentLevelState == LevelState.Playing)
        {
            score += amount;
            Debug.Log($"Score increased by {amount}, total: {score}");
        }
    }

    private int CalculateTimeBonus(float elapsedTime)
    {
        float maxBonus = 1500f;
        float cutoffTime = 900f; // 15 minutes

        float bonus = maxBonus * Mathf.Clamp01(1f - elapsedTime / cutoffTime);
        return Mathf.FloorToInt(bonus);
    }

    public int CalculateFinalScore()
    {
        int baseScore = 1000;
        int dogBitePenalty = dogBiteCount * 50;
        int dogTamedBonus = dogTamedCount * 100;

        float elapsedTime = timer != null ? timer.GetElapsedTime() : 0f;
        int timeBonus = CalculateTimeBonus(elapsedTime);

        int finalScore = baseScore - dogBitePenalty + dogTamedBonus + timeBonus + score;

        Debug.Log($"CalculateFinalScore: Base={baseScore}, Bites={dogBitePenalty}, Tamed={dogTamedBonus}, TimeBonus={timeBonus}, SpecialItems={score}, Final={finalScore}");
        return Mathf.Max(baseScore, finalScore); // Minimum will always be base score no matter how much the penalty is
    }

    // Replace GetFinalScore
    public int GetFinalScore()
    {
        return CalculateFinalScore();
    }

    #region To refocus cursor
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            StartCoroutine(RestoreCursorAfterFocus());
        }
    }

    private IEnumerator RestoreCursorAfterFocus()
    {
        yield return new WaitForSecondsRealtime(0.1f); // Slightly longer for stability
        
        bool isUIActive = (CurrentLevelState == LevelState.Paused || CurrentLevelState == LevelState.Completed);
        
        Cursor.visible = isUIActive;
        Cursor.lockState = isUIActive ? CursorLockMode.None : CursorLockMode.Locked;
        
        if (levelEventSystem != null)
        {
            levelEventSystem.enabled = !isUIActive;
        }
    }
    #endregion
}