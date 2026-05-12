using UnityEngine;
using UnityEngine.SceneManagement;

public class DefaultLevelLoader : LevelLoader
{
    void Start()
    {
        // Get level name from GameManager
        string levelName = GameManager.Instance.CurrentLevelName;
        Debug.Log($"DefaultLevelLoader.Start: CurrentLevelName = {levelName}");
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError("GameManager.CurrentLevelName not set");
            SceneManager.LoadScene("LevelSelectMenu"); // Fallback
            return;
        }

        // Load and instantiate level
        LoadAndInstantiate(levelName);
    }

    protected override MazeData LoadLevel(string levelIdentifier)
    {
        TextAsset levelFile = Resources.Load<TextAsset>($"Levels/{levelIdentifier}");
        if (levelFile == null)
        {
            Debug.LogError($"Level file not found: Levels/{levelIdentifier}");
            return null;
        }

        MazeData mazeData = MazeDataSerializer.Deserialize(levelFile.text);
        if (mazeData == null)
        {
            Debug.LogError($"Failed to deserialize level: {levelIdentifier}");
        }

        return mazeData;
    }
}