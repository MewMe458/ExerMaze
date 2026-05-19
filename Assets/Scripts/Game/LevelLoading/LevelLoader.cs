using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using static MazeData;
using Random = UnityEngine.Random;

public abstract class LevelLoader : MonoBehaviour
{
    [SerializeField] protected GameObject floorPrefab;
    [SerializeField] protected GameObject wallPrefab;
    [SerializeField] protected GameObject pillarPrefab;
    [SerializeField] protected GameObject endGoalPrefab;
    [SerializeField] protected float cellSize = 5.0f;
    [SerializeField] protected ElementPrefabMapping elementPrefabMapping;
    // --- NEW: ADD MATERIAL ARRAY TO MATCH AutoMG3D_1010 ---
    [SerializeField] protected Material[] wallMaterials; 
    // -----------------------------------------------------
    [SerializeField] private NavMeshSurface navMeshSurface;

    public MazeData CurrentMazeData { get; private set; }

    public void LoadAndInstantiate(string levelIdentifier)
    {
        MazeData mazeData = LoadLevel(levelIdentifier);
        if (mazeData == null)
        {
            Debug.LogError($"Failed to load level: {levelIdentifier}");
            SceneManager.LoadScene("LevelSelectMenu");
            return;
        }
        CurrentMazeData = mazeData;
        InstantiateLevel(mazeData);
    }

    protected abstract MazeData LoadLevel(string levelIdentifier);

    protected virtual void InstantiateLevel(MazeData mazeData)
    {
        if (mazeData == null || floorPrefab == null || wallPrefab == null || pillarPrefab == null || endGoalPrefab == null || mazeData.cells == null)
        {
            Debug.LogError("Invalid maze data, floor prefab, wall prefab, pillar prefab, end prefab or cells array");
            SceneManager.LoadScene("LevelSelectMenu");
            return;
        }

        SpawnFloors(mazeData);
        SpawnPillars(mazeData);
        SpawnWalls(mazeData);
        PositionPlayer(mazeData);
        SpawnEndGoal(mazeData);
        BakeNavMesh();
        SpawnElements(mazeData);
    }

    protected virtual void SpawnFloors(MazeData mazeData)
    {
        if (floorPrefab == null || mazeData == null || mazeData.cells == null)
        {
            Debug.LogError("Floor prefab, maze data, or cells array is null");
            return;
        }

        for (int x = 0; x < mazeData.rows; x++)
        {
            for (int y = 0; y < mazeData.columns; y++)
            {
                float posX = y * cellSize - cellSize / 2;
                float posZ = (mazeData.rows - 1 - x) * cellSize - cellSize / 2;
                GameObject floor = Instantiate(floorPrefab, new Vector3(posX, 0, posZ), Quaternion.identity, transform);
                floor.transform.localScale = new Vector3(cellSize, 1, cellSize); // Scale to cellSize
                floor.tag = "LevelObject";
            }
        }
    }

    protected virtual void SpawnPillars(MazeData mazeData)
    {
        if (pillarPrefab == null || mazeData == null)
        {
            Debug.LogError("Pillar prefab or maze data is null");
            return;
        }

        for (int x = 0; x <= mazeData.rows; x++)
        {
            for (int y = 0; y <= mazeData.columns; y++)
            {
                float posX = y * cellSize - cellSize / 2;
                float posZ = (mazeData.rows - x) * cellSize - cellSize / 2;
                GameObject pillar = Instantiate(pillarPrefab, new Vector3(posX, 0, posZ), Quaternion.identity, transform);
                pillar.transform.localScale = new Vector3(1, 1, 1);
                pillar.tag = "LevelObject";
            }
        }
    }

    protected virtual void SpawnWalls(MazeData mazeData)
    {
        if (wallPrefab == null || mazeData == null || mazeData.cells == null)
        {
            Debug.LogError("Wall prefab, maze data, or cells array is null");
            return;
        }

        float wallScale = cellSize - 1; // Wall spans cell minus pillar thickness
        for (int x = 0; x < mazeData.rows; x++)
        {
            for (int y = 0; y < mazeData.columns; y++)
            {
                CellData cell = mazeData.cells[x, y];
                float posX = y * cellSize;
                float posZ = (mazeData.rows - 1 - x) * cellSize;
                Vector3 cellPos = new Vector3(posX, 0, posZ);

                if (cell.WallRight)
                {
                    GameObject wall = Instantiate(wallPrefab, cellPos + new Vector3(cellSize / 2, 0, 0), Quaternion.Euler(0, 90, 0), transform);
                    wall.transform.localScale = new Vector3(wallScale, 1, 1); // Scale x to cellSize - 1
                    ApplyMaterial(wall, cell.MaterialIndex);
                    wall.tag = "LevelObject";
                }

                if (cell.WallBack)
                {
                    bool shouldInstantiate = true;
                    if (x > 0 && mazeData.cells[x - 1, y].WallFront)
                    {
                        shouldInstantiate = false;
                    }
                    if (shouldInstantiate)
                    {
                        GameObject wall = Instantiate(wallPrefab, cellPos + new Vector3(0, 0, cellSize / 2), Quaternion.Euler(0, 180, 0), transform);
                        wall.transform.localScale = new Vector3(wallScale, 1, 1); // Scale z to cellSize - 1
                        ApplyMaterial(wall, cell.MaterialIndex);
                        wall.tag = "LevelObject";
                    }
                }

                if (cell.WallFront)
                {
                    GameObject wall = Instantiate(wallPrefab, cellPos + new Vector3(0, 0, -cellSize / 2), Quaternion.Euler(0, 0, 0), transform);
                    wall.transform.localScale = new Vector3(wallScale, 1, 1); // Scale z to cellSize - 1
                    ApplyMaterial(wall, cell.MaterialIndex);
                    wall.tag = "LevelObject";
                }

                if (cell.WallLeft)
                {
                    bool shouldInstantiate = true;
                    if (y > 0 && mazeData.cells[x, y - 1].WallRight)
                    {
                        shouldInstantiate = false;
                    }
                    if (shouldInstantiate)
                    {
                        GameObject wall = Instantiate(wallPrefab, cellPos + new Vector3(-cellSize / 2, 0, 0), Quaternion.Euler(0, 270, 0), transform);
                        wall.transform.localScale = new Vector3(wallScale, 1, 1); // Scale x to cellSize - 1
                        ApplyMaterial(wall, cell.MaterialIndex);
                        wall.tag = "LevelObject";
                    }
                }
            }
        }
    }

    // --- NEW HELPER METHOD ---
    protected void ApplyMaterial(GameObject wallObject, int materialIndex)
    {
        if (wallMaterials == null || wallMaterials.Length == 0 || materialIndex < 0) return;

        // Ensure index is within bounds of the materials array
        int index = Mathf.Clamp(materialIndex, 0, wallMaterials.Length - 1);
        
        Renderer renderer = wallObject.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material = wallMaterials[index];
        }
    }
    // -------------------------

    protected virtual void SpawnEndGoal(MazeData mazeData)
    {
        if (endGoalPrefab == null || mazeData == null)
        {
            Debug.LogError("End goal prefab or maze data is null");
            SceneManager.LoadScene("LevelSelectMenu");
            return;
        }

        if (mazeData.end.x >= 0 && mazeData.end.x < mazeData.rows && mazeData.end.y >= 0 && mazeData.end.y < mazeData.columns)
        {
            float endPosX = mazeData.end.y * cellSize;
            float endPosZ = (mazeData.rows - 1 - mazeData.end.x) * cellSize;
            GameObject endGoal = Instantiate(endGoalPrefab, new Vector3(endPosX, 0, endPosZ), Quaternion.identity, transform);
        }
        else
        {
            Debug.LogError($"Invalid end cell: [{mazeData.end.x},{mazeData.end.y}]");
            SceneManager.LoadScene("LevelSelectMenu");
        }
    }

    protected virtual void PositionPlayer(MazeData mazeData)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;

            if (mazeData.start.x >= 0 && mazeData.start.x < mazeData.rows && mazeData.start.y >= 0 && mazeData.start.y < mazeData.columns)
            {
                float startPosX = mazeData.start.y * cellSize;
                float startPosZ = (mazeData.rows - 1 - mazeData.start.x) * cellSize;
                player.transform.position = new Vector3(startPosX, 0, startPosZ);
            }
            else
            {
                Debug.LogError($"Invalid start cell: [{mazeData.start.x},{mazeData.start.y}]");
                SceneManager.LoadScene("LevelSelectMenu");
            }

            if (controller != null)
                controller.enabled = true;
        }
        else
        {
            Debug.LogError("Player not found");
            SceneManager.LoadScene("LevelSelectMenu");
        }
    }

    protected virtual void SpawnElements(MazeData mazeData)
    {
        //Debug.Log("Spawn elements method called");
        if (mazeData.mode == "Relax")
        {
            Debug.Log("Skipping element spawning in Relax mode");
            return;
        }
        if (elementPrefabMapping == null)
        {
            Debug.LogWarning("ElementPrefabMapping not assigned; skipping elements");
            return;
        }

        if (mazeData.elements == null || mazeData.elements.Count == 0)
        {
            Debug.Log("No elements to spawn");
            return;
        }

        int minEndDistance = Mathf.CeilToInt(mazeData.rows / 2f) - 1;
        int minStartDistance = minEndDistance - 1;
        int baseMinItemDistance = Mathf.CeilToInt(mazeData.rows / 4f);
        int maxAttempts = 20;

        List<Vector2Int> availableCells = new List<Vector2Int>(mazeData.rows * mazeData.columns);
        for (int x = 0; x < mazeData.rows; x++)
        {
            for (int y = 0; y < mazeData.columns; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!(cell.x == mazeData.start.x && cell.y == mazeData.start.y) &&
                    !(cell.x == mazeData.end.x && cell.y == mazeData.end.y))
                {
                    availableCells.Add(cell);
                }
            }
        }

        Debug.Log($"Elements to spawn: {mazeData.elements.Count}, Available cells: {availableCells.Count}");

        if (mazeData.elements.Count > availableCells.Count)
        {
            Debug.LogWarning($"Too many elements ({mazeData.elements.Count}) for available cells ({availableCells.Count})");
        }
        int elementsToSpawn = Mathf.Min(mazeData.elements.Count, availableCells.Count);

        List<Vector2Int> usedCells = new List<Vector2Int>();
        foreach (var element in mazeData.elements)
        {
            Debug.Log($"Processing element: type={element.elementType}");
            if (string.IsNullOrEmpty(element.elementType))
            {
                Debug.LogWarning("Element with null/empty type");
                continue;
            }

            GameObject prefab = elementPrefabMapping.GetPrefabForType(element.elementType);
            Debug.Log($"Prefab for type '{element.elementType}': {(prefab != null ? prefab.name : "null")}");

            if (prefab == null)
            {
                Debug.LogWarning($"No prefab for type: {element.elementType}");
                continue;
            }

            if (availableCells.Count == 0)
            {
                Debug.LogWarning($"No available cells for {element.elementType}");
                break;
            }

            Vector2Int? selectedCell = null;
            int minItemDistance = baseMinItemDistance;
            while (minItemDistance >= 1 && selectedCell == null)
            {
                bool isNPC = element.elementType.Contains("npc", StringComparison.OrdinalIgnoreCase);
                int maxNPCStartDistance = 3;
                List<Vector2Int> tempCells = new List<Vector2Int>(availableCells);
                for (int attempt = 0; attempt < maxAttempts && tempCells.Count > 0; attempt++)
                {
                    int index = Random.Range(0, tempCells.Count);
                    Vector2Int candidate = tempCells[index];

                    bool valid = true;
                    foreach (var used in usedCells)
                    {
                        int distance = Mathf.Abs(candidate.x - used.x) + Mathf.Abs(candidate.y - used.y);
                        if (distance < minItemDistance)
                        {
                            valid = false;
                            break;
                        }
                    }
                    int startDistance = Mathf.Abs(candidate.x - mazeData.start.x) + Mathf.Abs(candidate.y - mazeData.start.y);
                    if (startDistance < minStartDistance)
                    {
                        valid = false;
                    }
                    int endDistance = Mathf.Abs(candidate.x - mazeData.end.x) + Mathf.Abs(candidate.y - mazeData.end.y);
                    if (endDistance < minEndDistance)
                    {
                        valid = false;
                    }
                    if (isNPC && startDistance > maxNPCStartDistance)
                        valid = false;
                    if (valid)
                    {
                        selectedCell = candidate;
                        availableCells.Remove(candidate);
                        break;
                    }
                    tempCells.RemoveAt(index);
                }
                if (selectedCell == null)
                {
                    minItemDistance--;
                    Debug.Log($"No cell found for {element.elementType} with minItemDistance={minItemDistance + 1}; trying {minItemDistance}");
                }
            }

            if (selectedCell == null)
            {
                Debug.LogWarning($"Failed to place {element.elementType}; skipping");
                continue;
            }

            float posX = selectedCell.Value.y * cellSize;
            float posZ = (mazeData.rows - 1 - selectedCell.Value.x) * cellSize;
            Vector3 position = new Vector3(posX, prefab.transform.position.y, posZ);
            GameObject obj = Instantiate(prefab, position, prefab.transform.rotation, transform);
            Debug.Log($"Spawned {element.elementType} at [{selectedCell.Value.x},{selectedCell.Value.y}]");

            if (element.elementType == "Dog" && element.detection > 0f)
            {
                DogNPCChase dogChase = obj.GetComponent<DogNPCChase>();
                if (dogChase != null)
                {
                    dogChase.DetectionSize = element.detection / 2.0f * cellSize;
                    Debug.Log($"Set detection range to {element.detection} for Dog at {position}");
                }
                else
                {
                    Debug.LogWarning("DogNPCChase component not found on instantiated Dog prefab");
                }
            }
            //obj.tag = "LevelObject";
        }
    }

    protected virtual void BakeNavMesh()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError("LevelLoader: NavMeshSurface not added");
            return;
        }
        navMeshSurface.BuildNavMesh();
        Debug.Log("NavMesh baked successfully");
    }

    protected virtual void ClearLevel()
    {
        GameObject[] levelObjects = GameObject.FindGameObjectsWithTag("LevelObject");
        foreach (GameObject obj in levelObjects)
        {
            Destroy(obj);
        }
    }
}