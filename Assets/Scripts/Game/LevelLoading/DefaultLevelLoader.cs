using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DefaultLevelLoader : LevelLoader
{
    void Start()
    {
        string levelName = GameManager.Instance.CurrentLevelName;
        Debug.Log($"DefaultLevelLoader.Start: CurrentLevelName = {levelName}");
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError("GameManager.CurrentLevelName not set");
            SceneManager.LoadScene("LevelSelectMenu"); 
            return;
        }

        LoadAndInstantiate(levelName);

        // If loading from save file, restore positions after base level completes generating
        if (MazeSaveHolder.HasLoadedData)
        {
            StartCoroutine(RestoreSavedState());
        }
    }

    private IEnumerator RestoreSavedState()
    {
        yield return new WaitForEndOfFrame(); 
        if (MazeSaveHolder.LoadedData?.playerData != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = MazeSaveHolder.LoadedData.playerData.position.ToVector3();
                player.transform.rotation = Quaternion.Euler(MazeSaveHolder.LoadedData.playerData.rotation.ToVector3());
            }
        }
        MazeSaveHolder.HasLoadedData = false; 
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