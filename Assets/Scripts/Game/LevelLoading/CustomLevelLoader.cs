using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class CustomLevelLoader : LevelLoader
{
    void Start()
    {
        // Get level identifier from GameManager
        string levelIdentifier = GameManager.Instance.CurrentCustomLevelPath;
        Debug.Log($"CustomLevelLoader.Start: CurrentCustomLevelPath = {levelIdentifier}");
        if (string.IsNullOrEmpty(levelIdentifier))
        {
            Debug.LogError("GameManager.CurrentCustomLevelPath not set");
            SceneManager.LoadScene("CustomLevelSelect"); // Fallback
            return;
        }

        // Load and instantiate level
        LoadAndInstantiate(levelIdentifier);
    }

    protected override MazeData LoadLevel(string levelIdentifier)
    {
        string filePath = levelIdentifier; // Use path as identifier
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("CustomLevelLoader: CurrentCustomLevelPath is null or empty");
            GameObject.FindFirstObjectByType<CustomLevelSelect>()?.ShowErrorFromExternal("No file path provided for loading.");
            SceneManager.LoadScene("CustomLevelSelect");
            return null;
        }

        // Normalize path for consistency
        string normalizedPath = NormalizePath(filePath);
        Debug.Log($"CustomLevelLoader: Loading maze from {normalizedPath}");

        try
        {
            if (!File.Exists(normalizedPath))
            {
                throw new System.Exception("File does not exist");
            }
            string json = File.ReadAllText(normalizedPath);
            MazeData mazeData = JsonUtility.FromJson<MazeData>(json);
            if (mazeData != null)
            {
                mazeData.RestoreAfterDeserialization();

                GameManager.Instance.SetGameState(GameManager.GameState.InGame);
                return mazeData;
            }
            else
            {
                throw new System.Exception("Failed to deserialize maze file");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CustomLevelLoader: Failed to load maze file at {normalizedPath}: {ex.Message}");
            // GameObject.FindFirstObjectByType<CustomLevelSelect>()?.ShowErrorFromExternal($"Failed to load maze file at {normalizedPath}: {ex.Message}");
            SceneManager.LoadScene("CustomLevelSelect");
            return null;
        }
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        // Replace forward slashes with backslashes for Windows
        return path.Replace('/', '\\').Replace("\\\\", "\\");
    }
}