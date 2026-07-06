using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class CustomLevelLoader : LevelLoader
{
    void Start()
    {
        string levelIdentifier = GameManager.Instance.CurrentCustomLevelPath;
        Debug.Log($"CustomLevelLoader.Start: CurrentCustomLevelPath = {levelIdentifier}");
        if (string.IsNullOrEmpty(levelIdentifier))
        {
            Debug.LogError("GameManager.CurrentCustomLevelPath not set");
            SceneManager.LoadScene("CustomLevelSelect"); 
            return;
        }

        LoadAndInstantiate(levelIdentifier);

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
        string filePath = levelIdentifier; 
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("CustomLevelLoader: CurrentCustomLevelPath is null or empty");
            SceneManager.LoadScene("CustomLevelSelect");
            return null;
        }

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
            SceneManager.LoadScene("CustomLevelSelect");
            return null;
        }
    }

    protected override void InstantiateLevel(MazeData mazeData)
    {
        if (mazeData == null) return;

        ClearLevel();
        base.InstantiateLevel(mazeData);
        InstantiateElements(mazeData); 
        BakeNavMesh();
    }

    protected override void InstantiateElements(MazeData mazeData)
    {
        if (mazeData == null || mazeData.elements == null) return;
        if (elementPrefabMapping == null)
        {
            Debug.LogError("CustomLevelLoader: ElementPrefabMapping is not assigned in the inspector!");
            return;
        }

        foreach (var element in mazeData.elements)
        {
            if (element == null) continue;

            GameObject prefab = elementPrefabMapping.GetPrefabForType(element.elementType);
            if (prefab == null)
            {
                Debug.LogWarning($"CustomLevelLoader: Missing prefab configuration for element type '{element.elementType}'");
                continue;
            }

            float posX = element.position.y * cellSize;
            float posZ = (mazeData.rows - 1 - element.position.x) * cellSize;
            
            Vector3 position = new Vector3(posX, prefab.transform.position.y, posZ);

            GameObject obj = Instantiate(prefab, position, prefab.transform.rotation, transform);
            obj.name = $"{element.elementType}_{element.position.x}_{element.position.y}";
            obj.tag = "LevelObject"; 

            Debug.Log($"CustomLevelLoader: Successfully spawned element '{element.elementType}' at calculated 3D coordinates: {position}");

            if ((element.elementType == "Dog" || element.elementType == "DogNPC") && element.detection > 0f)
            {
                var dogChase = obj.GetComponent<DogNPCChase>();
                if (dogChase != null)
                {
                    dogChase.DetectionSize = element.detection / 2.0f * cellSize;
                }
            }
        }
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.Replace('/', '\\').Replace("\\\\", "\\");
    }
}