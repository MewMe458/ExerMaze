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
            SceneManager.LoadScene("CustomLevelSelect");
            return null;
        }
    }

    protected override void InstantiateLevel(MazeData mazeData)
    {
        if (mazeData == null) return;

        ClearLevel();

        // 1. Structural assembly (floors, walls, goal points)
        base.InstantiateLevel(mazeData);

        // 2. Custom Element Generation Pipeline runs via our override below
        // InstantiateElements(mazeData);

        // 3. Bake the AI navigation surface layout
        BakeNavMesh();
    }

    // 🛠️ FIX: Overriding this method forces the script to read positions directly from the JSON fields.
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

            // Fetch the prefab using your string mapping logic
            GameObject prefab = elementPrefabMapping.GetPrefabForType(element.elementType);
            if (prefab == null)
            {
                Debug.LogWarning($"CustomLevelLoader: Missing prefab configuration for element type '{element.elementType}'");
                continue;
            }

            // Calculate precise 3D spatial alignment offsets directly from saved coordinates
            float posX = element.position.y * cellSize;
            float posZ = (mazeData.rows - 1 - element.position.x) * cellSize;
            
            // Retain original prefab height alignment configuration safely
            Vector3 position = new Vector3(posX, prefab.transform.position.y, posZ);

            GameObject obj = Instantiate(prefab, position, prefab.transform.rotation, transform);
            obj.name = $"{element.elementType}_{element.position.x}_{element.position.y}";
            obj.tag = "LevelObject"; // Ensure proper clean up tracking tags are applied

            Debug.Log($"CustomLevelLoader: Successfully spawned element '{element.elementType}' at calculated 3D coordinates: {position}");

            // Account for custom Dog parameter configuration variants ("Dog" and "DogNPC")
            if ((element.elementType == "Dog" || element.elementType == "DogNPC") && element.detection > 0f)
            {
                // Try fetching chase components dynamically across common variants
                var dogChase = obj.GetComponent<DogNPCChase>();
                if (dogChase != null)
                {
                    dogChase.DetectionSize = element.detection / 2.0f * cellSize;
                    Debug.Log($"CustomLevelLoader: Configured Dog detection size radius boundary to: {dogChase.DetectionSize}");
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