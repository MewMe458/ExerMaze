using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Menu,       
        InGame      
    }

    public enum MazeShape { Square, Circle, Triangle }
    public MazeShape CurrentMazeShape { get; private set; } = MazeShape.Square;

    public GameState CurrentState { get; private set; } = GameState.Menu;

    public string CurrentLevelName { get; private set; }
    public string CurrentCustomLevelPath { get; set; }

    public List<string> CustomLevelQueue { get; set; } = new List<string>();
    public int CurrentCustomLevelIndex { get; set; } = -1;

    public void SetupCustomLevelQueue(List<string> paths)
    {
        CustomLevelQueue = new List<string>(paths);
        CurrentCustomLevelIndex = -1; 
        Debug.Log($"GameManager: Queue initialized with {CustomLevelQueue.Count} custom levels.");
    }

    public bool AdvanceToNextCustomLevel()
    {
        if (CustomLevelQueue != null && CurrentCustomLevelIndex + 1 < CustomLevelQueue.Count)
        {
            CurrentCustomLevelIndex++;
            CurrentCustomLevelPath = CustomLevelQueue[CurrentCustomLevelIndex];
            Debug.Log($"GameManager: Advanced to next custom level: {CurrentCustomLevelPath}");
            return true;
        }
        return false;
    }

    public MazeData LoadedMazeData { get; set; } 

    public int MazeWidth { get; private set; }
    public int MazeDepth { get; private set; }

    // FIXED: Restored the 3-argument overload so all buttons compile
    public void SetMazeSize(int width, int depth, MazeShape shape = MazeShape.Square)
    {
        MazeWidth = width;
        MazeDepth = depth;
        CurrentMazeShape = shape;
        Debug.Log($"GameManager: Maze size set to {width} x {depth} ({shape})");
    }

    public void SetMazeShape(MazeShape shape)
    {
        CurrentMazeShape = shape;
        Debug.Log($"GameManager: Maze shape explicitly set to {shape}");
    }

    public void SetMazeShapeFromString(string shapeStr)
    {
        if (System.Enum.TryParse(shapeStr, out MazeShape parsedShape))
        {
            SetMazeShape(parsedShape);
        }
        else
        {
            SetMazeShape(MazeShape.Square);
        }
    }

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