using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Menu,       // User is in the menu, Bluetooth data should be ignored
        InGame      // User is in a game level, Bluetooth data should control the character
    }

    public GameState CurrentState { get; private set; } = GameState.Menu;

    public string CurrentLevelName { get; private set; }
    public string CurrentCustomLevelPath { get; set; }

    // --- UPDATED: HOLD LOADED MAZE DATA ---
    public MazeData LoadedMazeData { get; set; } // Now the 3D generator can access wall indices
    // --------------------------------------

    // ------------------------------
    // NEW MAZE SIZE FIELDS
    // ------------------------------
    public int MazeWidth { get; private set; }
    public int MazeDepth { get; private set; }

    public void SetMazeSize(int width, int depth)
    {
        MazeWidth = width;
        MazeDepth = depth;
        Debug.Log($"GameManager: Maze size set to {width} x {depth}");
    }
    // ------------------------------

    // --- NEW PERSISTENT GPX DATA ---
    public bool IsContinuingSession { get; set; } = false;
    public List<(double lat, double lon, float ele, string time)> SessionCharacterPoints = new();
    public List<(double lat, double lon, float ele, string time)> SessionRealLifePoints = new();
    public int AccumulatedSteps = 0;
    public float AccumulatedTime = 0f;

    public void ClearSessionData()
    {
        IsContinuingSession = false;
        SessionCharacterPoints.Clear();
        SessionRealLifePoints.Clear();
        AccumulatedSteps = 0;
        AccumulatedTime = 0f;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (!SoundManager.Instance.IsBGMPlaying)
        {
            SoundManager.Instance.PlayBGM();
        }
    }

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"Game state changed to: {newState}");
    }

    public void SetCurrentLevelName(string levelName)
    {
        CurrentLevelName = levelName;
        Debug.Log($"GameManager: Set CurrentLevelName to {levelName}");
    }

    public void ClearCurrentLevelName()
    {
        CurrentLevelName = null;
        Debug.Log("GameManager: Cleared CurrentLevelName");
    }

    public void ClearCurrentCustomLevelPath()
    {
        CurrentCustomLevelPath = null;
        LoadedMazeData = null;
        Debug.Log("GameManager: Cleared CurrentCustomLevelPath");
    }
}
