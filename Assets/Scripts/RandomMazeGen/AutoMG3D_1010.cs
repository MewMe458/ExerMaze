using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Collections;
using UnityEngine;

public class AutoMG3D_1010 : MonoBehaviour
{
    private static AutoMG3D_1010 _instance;
    public static AutoMG3D_1010 Instance
    {
        get => _instance;
        private set => _instance = value;
    }

    #region Inspector Fields

    [Header("Maze Seed")]
    [SerializeField] private int seed;

    [Header("Maze Dimensions")]
    [SerializeField] private int width = 12;
    [SerializeField] private int depth = 12;
    private const int height = 1; // Fixed height

    [Header("Cell Size")]
    [SerializeField] private float cellSize = 4f;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject endGoalPrefab;
    [SerializeField] private GameObject dogPrefab;
    [SerializeField] private GameObject bonePrefab;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private GameObject teleportPrefab;
    [SerializeField] private GameObject slowPrefab;

    [Header("NPCs & Items Count")]
    [SerializeField] private int dogCount = 0;
    [SerializeField] private int boneCount = 0;
    [SerializeField] private int shieldCount = 0;
    [SerializeField] private int starCount = 0;
    [SerializeField] private int teleportCount = 0;
    [SerializeField] private int slowCount = 0;

    [Header("Wall Materials")]
    [SerializeField] private Material[] wallMaterials; // size = 24
    [SerializeField] private int wallRegionSize = 6;

    [Header("Materials")]
    [SerializeField] private Material floorMaterial;

    [Header("Generation Settings")]
    [SerializeField] private bool useIterativeGeneration = false;
    [SerializeField] private bool generateFloors = true;

    [Header("Maze Info")]
    [SerializeField, ReadOnly] private string generationStatus = "Not Generated";
    [SerializeField, ReadOnly] private int totalCells = 0;
    [SerializeField, ReadOnly] private int totalWalls = 0;

    [Header("Maze Complexity")]
    [SerializeField, Range(0f, 0.5f)]
    private float extraConnectionChance = 0.15f;

    [Header("Center Room Settings")]
    [SerializeField] private bool generateCenterRoom = true;
    [SerializeField, ReadOnly] private int centerRoomSize;

    [Header("Nav Mesh Surface")]
    [SerializeField] private NavMeshSurface navmeshsurface;

    #endregion

    #region Private Fields
    // Key = region coordinate (xRegion, zRegion)
    private Dictionary<Vector2Int, Material> wallRegionMaterials
        = new Dictionary<Vector2Int, Material>();

    private MazeCell[,,] maze;
    private List<GameObject> mazeObjects = new List<GameObject>();

    // Directions: North, South, East, West (removed Up/Down since height is 1)
    private Vector3Int[] directions =
    {
        Vector3Int.forward,    // North
        Vector3Int.back,       // South
        Vector3Int.right,      // East
        Vector3Int.left       // West
    };

    #endregion

    #region Public Properties

    public int Width
    {
        get => width;
        set => width = Mathf.Max(1, value);
    }

    public int Depth
    {
        get => depth;
        set => depth = Mathf.Max(1, value);
    }

    // Height is now read-only since it's fixed
    public int Height => height;

    #endregion

    #region Nested Types

    [System.Serializable]
    public class MazeCell
    {
        // 0: North, 1: South, 2: East, 3: West (removed Up/Down)
        public bool[] walls = new bool[4];
        public bool visited = false;
        public Vector3Int position;
        public int distance = -1;

        public MazeCell(Vector3Int pos)
        {
            position = pos;

            // Start with all walls present
            for (int i = 0; i < 4; i++)
            {
                walls[i] = true;
            }
        }
    }

    [System.Serializable]
    private struct MazeItem
    {
        public GameObject prefab;
        public int count;
    }

    #endregion

    #region Public API

    public void GenerateMaze()
    {
        if (seed == 0)
            seed = Random.Range(0, 999999);

        Random.InitState(seed);
        
        ClearMaze();
        InitializeMaze();

        if (useIterativeGeneration)
            GenerateMazeIterative();
        else
            GenerateMazeRecursive(Vector3Int.zero);

        if (generateCenterRoom)
            CreateCenterRoom();

        AddExtraConnections();

        if (player != null)
        {
            player.position = GetCenterRoomWorldPosition();
        }
        else
        {
            Debug.LogError(player.position);
        }

        VisualizeMaze();
        UpdateMazeInfo();
        BakeNavMesh();
        // PlaceEndGoalAtFarthestCell();
        PlaceEndGoalAtRandomCell();
        PlaceMazeItems();

        Debug.Log($"Maze generated! Dimensions: {width}x{height}x{depth}, Total cells: {totalCells}");
    }

    public void ClearMaze()
    {
        wallRegionMaterials.Clear();

        foreach (GameObject obj in mazeObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }

        mazeObjects.Clear();
        generationStatus = "Cleared";
        totalCells = 0;
        totalWalls = 0;
    }

    public Bounds GetMazeBounds()
    {
        Vector3 center = new Vector3(
            width * cellSize * 0.5f,
            0, // Height is always 1, so center Y is 0
            depth * cellSize * 0.5f
        );

        Vector3 size = new Vector3(
            width * cellSize,
            1.5f, // Wall height (from GetWallScale)
            depth * cellSize
        );

        return new Bounds(center, size);
    }

    public string GetMazeInfo()
    {
        if (maze == null)
            return "No maze generated";

        return $"Dimensions: {width}x{height}x{depth}, Cells: {totalCells}, Walls: {totalWalls}";
    }

    #endregion

    #region Maze Generation
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        if (GameManager.Instance != null)
        {
            width = GameManager.Instance.MazeWidth;
            depth = GameManager.Instance.MazeDepth;

            // Safety fallback
            if (width <= 0 || depth <= 0)
            {
                width = 12;
                depth = 12;
            }

            Debug.Log($"AutoMG3D: Using maze size {width} x {depth}");
        }
    }

    private void Start()
    {
        if (MazeSaveHolder.HasLoadedData)
        {
            LoadMazeFromData(MazeSaveHolder.LoadedData);
            // Reset the holder so it doesn't load the same maze next time you play
            MazeSaveHolder.HasLoadedData = false;
        }
        else
            StartCoroutine(GenerateMazeDelayed());
    }

    private IEnumerator GenerateMazeDelayed()
    {
        yield return null; // wait one frame

        GenerateMaze();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
            player.position = GetCenterRoomWorldPosition();
    }

    private void LoadMazeFromData(SaveMazeData data)
    {
        ClearMaze();

        // 1. Load Floor
        if (data.floor != null && floorPrefab != null)
        {
            SpawnFromData(floorPrefab, data.floor);
        }

        // 2. Load Walls
        foreach (var wall in data.walls) {
            SpawnFromData(wallPrefab, wall);
        }

        // 3. Load Collectibles
        foreach (var item in data.collectibles) {
            GameObject prefab = GetPrefabByType(item.type);
            if (prefab != null) SpawnFromData(prefab, item);
        }

        // 4. Load NPCs & Goal
        foreach (var npc in data.npcs) {
            GameObject prefab = GetPrefabByType(npc.type);
            if (prefab != null) SpawnFromData(prefab, npc);
        }
        foreach (var goal in data.endGoal) {
            SpawnFromData(endGoalPrefab, goal);
        }

        if (data.playerData != null)
        {
            // Find the player in the scene if the reference is lost
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (player != null)
            {
                // Set position
                player.position = data.playerData.position.ToVector3();
                
                // Apply only Y-Rotation to keep the capsule upright
                Vector3 savedRotation = data.playerData.rotation.ToVector3();
                player.rotation = Quaternion.Euler(0, savedRotation.y, 0);
                
                Debug.Log("Player position and rotation restored.");
            }
        }

        BakeNavMesh();
    }

    private GameObject GetPrefabByType(string type)
    {
        // Match the string name back to your inspector variables
        if (type.Contains("Dog")) return dogPrefab;
        if (type.Contains("Star")) return starPrefab;
        if (type.Contains("Bone")) return bonePrefab;
        if (type.Contains("Shield")) return shieldPrefab;
        if (type.Contains("Teleport")) return teleportPrefab;
        if (type.Contains("Slow")) return slowPrefab;
        return null;
    }

    private void SpawnFromData(GameObject prefab, ObjectData data)
    {
        GameObject instance = Instantiate(prefab, data.position.ToVector3(), Quaternion.Euler(data.rotation.ToVector3()));
        
        // Apply the saved scale
        instance.transform.localScale = data.scale.ToVector3();
        instance.transform.parent = transform;
        
        // Re-apply Material if it's a wall and has a valid index
        if (data.materialIndex >= 0 && data.materialIndex < wallMaterials.Length)
        {
            Renderer renderer = instance.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = wallMaterials[data.materialIndex];
            }
        }

        mazeObjects.Add(instance);
    }

    private void InitializeMaze()
    {
        maze = new MazeCell[width, height, depth];
        totalCells = width * height * depth;

        // Auto-calculate center room size
        if (width <= depth)
        {
            centerRoomSize = Mathf.Max(1, width / 4);
        }
        else
        {
            centerRoomSize = Mathf.Max(1, depth / 4);
        }

        // Since height is always 1, we only need to loop through y=0
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                maze[x, 0, z] = new MazeCell(new Vector3Int(x, 0, z));
            }
        }

        generationStatus = "Initialized";
    }

    private void GenerateMazeRecursive(Vector3Int currentPos)
    {
        maze[currentPos.x, currentPos.y, currentPos.z].visited = true;

        ShuffleDirections();

        foreach (Vector3Int direction in directions)
        {
            Vector3Int nextPos = currentPos + direction;

            if (IsInBounds(nextPos) && !maze[nextPos.x, nextPos.y, nextPos.z].visited)
            {
                RemoveWalls(currentPos, nextPos, direction);
                GenerateMazeRecursive(nextPos);
            }
        }
    }

    private void GenerateMazeIterative()
    {
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        Vector3Int start = Vector3Int.zero;

        stack.Push(start);
        maze[start.x, start.y, start.z].visited = true;

        while (stack.Count > 0)
        {
            Vector3Int current = stack.Pop();
            List<Vector3Int> unvisitedNeighbors = GetUnvisitedNeighbors(current);

            if (unvisitedNeighbors.Count > 0)
            {
                stack.Push(current);

                Vector3Int chosenDirection =
                    unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];

                Vector3Int nextPos = current + chosenDirection;

                RemoveWalls(current, nextPos, chosenDirection);
                maze[nextPos.x, nextPos.y, nextPos.z].visited = true;

                stack.Push(nextPos);
            }
        }

        generationStatus = "Generated (Iterative)";
    }

    private List<Vector3Int> GetUnvisitedNeighbors(Vector3Int pos)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();

        foreach (Vector3Int direction in directions)
        {
            Vector3Int neighborPos = pos + direction;

            if (IsInBounds(neighborPos) &&
                !maze[neighborPos.x, neighborPos.y, neighborPos.z].visited)
            {
                neighbors.Add(direction);
            }
        }

        return neighbors;
    }

    private bool IsInCenterRoom(Vector3Int pos)
    {
        Vector3Int center = new Vector3Int(width / 2, 0, depth / 2);
        int half = centerRoomSize / 2;

        // Check if the position falls within the calculated center bounds
        return pos.x >= center.x - half && pos.x < center.x + half + (centerRoomSize % 2) &&
               pos.z >= center.z - half && pos.z < center.z + half + (centerRoomSize % 2);
    }

    private void CreateCenterRoom()
    {
        Vector3Int center = new Vector3Int(width / 2, 0, depth / 2);
        int half = centerRoomSize / 2;

        for (int x = center.x - half; x < center.x + half + (centerRoomSize % 2); x++)
        {
            for (int z = center.z - half; z < center.z + half + (centerRoomSize % 2); z++)
            {
                if (!IsInBounds(new Vector3Int(x, 0, z))) continue;

                // Remove walls between adjacent cells inside the center room
                if (x > center.x - half) RemoveWalls(new Vector3Int(x, 0, z), new Vector3Int(x - 1, 0, z), Vector3Int.left);
                if (z > center.z - half) RemoveWalls(new Vector3Int(x, 0, z), new Vector3Int(x, 0, z - 1), Vector3Int.back);
            }
        }
    }

    private void SpawnEndGoal(Vector3Int cellPos)
    {
        Vector3 worldPos = new Vector3(
            cellPos.x * cellSize,
            cellPos.y * cellSize,
            cellPos.z * cellSize
        );

        GameObject goal = Instantiate(endGoalPrefab, worldPos, Quaternion.identity);
        goal.name = "End Goal";
        goal.transform.parent = transform;

        mazeObjects.Add(goal);
    }

    private void PlaceMazeItems()
    {
        List<Vector3Int> validCells = new List<Vector3Int>();

        // Collect valid floor cells (y always 0)
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);

                if (generateCenterRoom && IsInCenterRoom(pos))
                    continue;

                validCells.Add(pos);
            }
        }

        // Shuffle cells
        for (int i = 0; i < validCells.Count; i++)
        {
            int r = Random.Range(i, validCells.Count);
            (validCells[i], validCells[r]) = (validCells[r], validCells[i]);
        }

        // Auto-calculate counts if zero
        int totalItems = (width + depth) / 2;

        int autoPerType = totalItems / 6;

        MazeItem[] items =
        {
            new MazeItem { prefab = dogPrefab,    count = dogCount    > 0 ? dogCount    : autoPerType },
            new MazeItem { prefab = bonePrefab,   count = boneCount   > 0 ? boneCount   : autoPerType },
            new MazeItem { prefab = shieldPrefab, count = shieldCount > 0 ? shieldCount : autoPerType },
            new MazeItem { prefab = starPrefab,   count = starCount   > 0 ? starCount   : autoPerType },
            new MazeItem { prefab = teleportPrefab,   count = teleportCount   > 0 ? teleportCount   : autoPerType },
            new MazeItem { prefab = slowPrefab,   count = slowCount   > 0 ? slowCount   : autoPerType }
        };

        int index = 0;

        foreach (var item in items)
        {
            if (item.prefab == null)
                continue;

            for (int i = 0; i < item.count && index < validCells.Count; i++)
            {
                SpawnItem(item.prefab, validCells[index++]);
            }
        }
    }

    private void SpawnItem(GameObject prefab, Vector3Int cellPos)
    {
        Vector3 worldPos;
        if (prefab == dogPrefab)
        {
            worldPos = new Vector3(
                cellPos.x * cellSize,
                0,
                cellPos.z * cellSize
            );
        }
        else if (prefab == teleportPrefab)
        {
            worldPos = new Vector3(
                cellPos.x * cellSize,
                cellPos.y + 0.2f,
                cellPos.z * cellSize
            );
        }
        else
        {
            worldPos = new Vector3(
                cellPos.x * cellSize,
                1,
                cellPos.z * cellSize
            );            
        }

        Quaternion rotation = Quaternion.identity;

        // Rotate stars only
        if (prefab == starPrefab)
        {
            rotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        GameObject item = Instantiate(prefab, worldPos, rotation);
        item.transform.parent = transform;
        mazeObjects.Add(item);
    }

    protected virtual void BakeNavMesh()
    {
        if (navmeshsurface == null)
        {
            Debug.LogError("LevelLoader: NavMeshSurface not added");
            return;
        }
        navmeshsurface.BuildNavMesh();
        Debug.Log("NavMesh baked successfully");
    }

    private void AddExtraConnections()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);
                MazeCell cell = maze[x, 0, z];

                foreach (Vector3Int dir in directions)
                {
                    if (Random.value > extraConnectionChance)
                        continue;

                    Vector3Int neighborPos = pos + dir;

                    if (!IsInBounds(neighborPos))
                        continue;

                    MazeCell neighbor = maze[neighborPos.x, neighborPos.y, neighborPos.z];

                    // Only remove wall if there IS currently a wall
                    int wallIndex = DirectionToWallIndex(dir);

                    if (cell.walls[wallIndex])
                    {
                        RemoveWalls(pos, neighborPos, dir);
                    }
                }
            }
        }
    }

    #endregion

    #region Maze Logic Helpers

    private void RemoveWalls(Vector3Int current, Vector3Int next, Vector3Int direction)
    {
        MazeCell currentCell = maze[current.x, current.y, current.z];
        MazeCell nextCell = maze[next.x, next.y, next.z];

        if (direction == Vector3Int.forward) // North
        {
            currentCell.walls[0] = false; // Current's north wall
            nextCell.walls[1] = false;    // Next's south wall
        }
        else if (direction == Vector3Int.back) // South
        {
            currentCell.walls[1] = false; // Current's south wall
            nextCell.walls[0] = false;    // Next's north wall
        }
        else if (direction == Vector3Int.right) // East
        {
            currentCell.walls[2] = false; // Current's east wall
            nextCell.walls[3] = false;    // Next's west wall
        }
        else if (direction == Vector3Int.left) // West
        {
            currentCell.walls[3] = false; // Current's west wall
            nextCell.walls[2] = false;    // Next's east wall
        }
    }

    private bool IsInBounds(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < width &&
               pos.y >= 0 && pos.y < height &&
               pos.z >= 0 && pos.z < depth;
    }

    private void ShuffleDirections()
    {
        for (int i = 0; i < directions.Length; i++)
        {
            int randomIndex = Random.Range(i, directions.Length);
            Vector3Int temp = directions[i];
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }
    }

    private void PlaceEndGoalAtRandomCell(bool avoidCenterRoom = true)
    {
        if (endGoalPrefab == null)
            return;

        List<Vector3Int> validCells = new List<Vector3Int>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);

                if (avoidCenterRoom && generateCenterRoom && IsInCenterRoom(pos))
                    continue;

                validCells.Add(pos);
            }
        }

        if (validCells.Count == 0)
        {
            Debug.LogWarning("No valid cells found for end goal placement.");
            return;
        }

        Vector3Int chosenCell = validCells[Random.Range(0, validCells.Count)];
        SpawnEndGoal(chosenCell);
    }

    public Vector3 GetRandomCellWorldPosition(bool avoidCenterRoom = true)
    {
        List<Vector3Int> validCells = new List<Vector3Int>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);

                if (avoidCenterRoom && generateCenterRoom && IsInCenterRoom(pos))
                    continue;

                validCells.Add(pos);
            }
        }

        if (validCells.Count == 0)
            return Vector3.zero;

        Vector3Int chosen = validCells[Random.Range(0, validCells.Count)];

        return new Vector3(
            chosen.x * cellSize,
            0f,
            chosen.z * cellSize
        );
    }

    private int DirectionToWallIndex(Vector3Int dir)
    {
        if (dir == Vector3Int.forward) return 0; // North
        if (dir == Vector3Int.back)    return 1; // South
        if (dir == Vector3Int.right)   return 2; // East
        return 3; // West
    }

    #endregion

    #region Visualization

    private void VisualizeMaze()
    {
        totalWalls = 0;

        // Since height is always 1, we only need to loop through y=0
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                MazeCell cell = maze[x, 0, z];
                Vector3 worldPos = new Vector3(
                    x * cellSize,
                    0,
                    z * cellSize
                );

                if (cell.walls[0]) { CreateWall(worldPos, Vector3.forward, "North Wall", x, z); totalWalls++; }
                if (cell.walls[1]) { CreateWall(worldPos, Vector3.back, "South Wall", x, z); totalWalls++; }
                if (cell.walls[2]) { CreateWall(worldPos, Vector3.right, "East Wall", x, z); totalWalls++; }
                if (cell.walls[3]) { CreateWall(worldPos, Vector3.left, "West Wall", x, z); totalWalls++; }
            }
        }
        CreateSingleFloor();
        generationStatus = "Visualized";
    }

    private void CreateSingleFloor()
    {
        if (!generateFloors || floorPrefab == null)
            return;

        GameObject floor = Instantiate(floorPrefab);
        floor.name = "Maze Floor";

        // Correct center position
        floor.transform.position = new Vector3(
            -1f * (cellSize / 2),
            0f,
            -1f * (cellSize / 2)
        );

        // Correct scale to cover entire maze
        floor.transform.localScale = new Vector3(
            width * cellSize,
            1f,
            depth * cellSize
        );

        floor.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        floor.transform.parent = transform;
        mazeObjects.Add(floor);
    }

    private void CreateWall(Vector3 position, Vector3 direction, string name, int cellX, int cellZ)
    {
        GameObject wall = wallPrefab != null
            ? Instantiate(wallPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        wall.name = name;
        wall.transform.position = position + direction * (cellSize / 2f);
        wall.transform.localScale = GetWallScale(direction);
        wall.transform.parent = transform;

        Material regionMaterial = GetWallMaterialForCell(cellX, cellZ);

        if (regionMaterial != null)
        {
            Renderer renderer = wall.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material = regionMaterial; // instance material
        }

        mazeObjects.Add(wall);
    }

    private Vector3 GetWallScale(Vector3 direction)
    {
        // All walls are now vertical walls only
        if (direction == Vector3.forward || direction == Vector3.back) // North/South walls
            return new Vector3(cellSize, 1.5f, 0.5f);

        return new Vector3(0.5f, 1.5f, cellSize); // East/West walls
    }

    private Vector3 GetCenterRoomWorldPosition()
    {
        int centerX = width / 2;
        int centerZ = depth / 2;

        return new Vector3(
            centerX * cellSize,
            0f, // Player height (adjust if needed)
            centerZ * cellSize
        );
    }

    private void UpdateMazeInfo()
    {
        generationStatus = $"Generated - {width}x{height}x{depth}";
    }

    private Material GetWallMaterialForCell(int x, int z)
    {
        if (wallMaterials == null || wallMaterials.Length == 0)
            return null;

        // --- NEW: CHECK FOR PRE-LOADED DATA FROM GAMEMANAGER ---
        if (GameManager.Instance != null && GameManager.Instance.LoadedMazeData != null)
        {
            MazeData loadedData = GameManager.Instance.LoadedMazeData;
            
            // Ensure we are within the bounds of the loaded maze data
            if (x < loadedData.rows && z < loadedData.columns)
            {
                int savedIndex = loadedData.cells[x, z].MaterialIndex;
                
                // If the saved index is valid, use it immediately
                if (savedIndex >= 0 && savedIndex < wallMaterials.Length)
                {
                    return wallMaterials[savedIndex];
                }
            }
        }
        // -------------------------------------------------------

        int regionX = x / wallRegionSize;
        int regionZ = z / wallRegionSize;

        Vector2Int regionKey = new Vector2Int(regionX, regionZ);

        // If this region does not yet have a material, assign one
        if (!wallRegionMaterials.TryGetValue(regionKey, out Material mat))
        {
            mat = wallMaterials[Random.Range(0, wallMaterials.Length)];
            wallRegionMaterials.Add(regionKey, mat);
        }

        return mat;
    }

    #endregion

    #region Save Maze Details
    public SaveMazeData GetMazeSaveData()
    {
        SaveMazeData data = new SaveMazeData();
        data.width = this.width;
        data.depth = this.depth;

        CaptureObjects(data.walls, "Wall");
        CaptureObjects(data.npcs, "NPC");
        CaptureObjects(data.collectibles, "Collectibles");
        CaptureObjects(data.endGoal, "MazeGoal");

        // Capture the floor specifically
        GameObject floorObj = GameObject.Find("Maze Floor");
        if (floorObj != null)
        {
            data.floor = new ObjectData
            {
                type = "Floor",
                position = new SerializableVector3(floorObj.transform.position),
                rotation = new SerializableVector3(floorObj.transform.eulerAngles),
                scale = new SerializableVector3(floorObj.transform.localScale)
            };
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            data.playerData = new ObjectData
            {
                type = "Player",
                position = new SerializableVector3(playerObj.transform.position),
                rotation = new SerializableVector3(playerObj.transform.eulerAngles),
                scale = new SerializableVector3(playerObj.transform.localScale)
            };
        }

        return data;
    }

    private void CaptureObjects(List<ObjectData> list, string tag)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag))
        {
            int matIdx = -1;
            Renderer rend = obj.GetComponentInChildren<Renderer>();
            
            // Find which index in the wallMaterials array this object is using
            if (rend != null && wallMaterials != null)
            {
                for (int i = 0; i < wallMaterials.Length; i++)
                {
                    if (rend.sharedMaterial == wallMaterials[i])
                    {
                        matIdx = i;
                        break;
                    }
                }
            }

            list.Add(new ObjectData
            {
                type = obj.name.Replace("(Clone)", "").Trim(),
                position = new SerializableVector3(obj.transform.position),
                rotation = new SerializableVector3(obj.transform.eulerAngles),
                scale = new SerializableVector3(obj.transform.localScale),
                materialIndex = matIdx
            });
        }
    }
    #endregion
}