using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private const int height = 1; 

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
    [SerializeField] private Material[] wallMaterials; 
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
    private Dictionary<Vector2Int, Material> wallRegionMaterials = new Dictionary<Vector2Int, Material>();
    private MazeCell[,,] maze;
    private List<GameObject> mazeObjects = new List<GameObject>();
    private GameManager.MazeShape mazeShape = GameManager.MazeShape.Square;

    private Vector3Int[] directions =
    {
        Vector3Int.forward,    
        Vector3Int.back,       
        Vector3Int.right,      
        Vector3Int.left       
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

    public int Height => height;
    #endregion

    #region Nested Types
    [System.Serializable]
    public class MazeCell
    {
        public bool[] walls = new bool[4];
        public bool visited = false;
        public Vector3Int position;
        public int distance = -1;

        public MazeCell(Vector3Int pos)
        {
            position = pos;
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
            mazeShape = GameManager.Instance.CurrentMazeShape;

            if (width <= 0 || depth <= 0)
            {
                width = 12;
                depth = 12;
                mazeShape = GameManager.MazeShape.Square;
            }
        }
    }

    private void Start()
    {
        if (MazeSaveHolder.HasLoadedData)
        {
            LoadMazeFromData(MazeSaveHolder.LoadedData);
            MazeSaveHolder.HasLoadedData = false;
        }
        else
            StartCoroutine(GenerateMazeDelayed());
    }

    private IEnumerator GenerateMazeDelayed()
    {
        yield return null; 
        GenerateMaze();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
        {
            // FIX: Disable CharacterController temporarily to force position update
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            player.position = GetCenterRoomWorldPosition();
            
            if (cc != null) cc.enabled = true;
        }
    }

    public void GenerateMaze()
    {
        if (seed == 0)
            seed = Random.Range(0, 999999);

        Random.InitState(seed);
        
        ClearMaze();
        InitializeMaze();

        Vector3Int startPos = GetStartingPosition();

        if (useIterativeGeneration)
            GenerateMazeIterative(startPos);
        else
            GenerateMazeRecursive(startPos);

        if (generateCenterRoom)
            CreateCenterRoom();

        AddExtraConnections();

        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = GetCenterRoomWorldPosition();
            if (cc != null) cc.enabled = true;
        }

        VisualizeMaze();
        UpdateMazeInfo();
        BakeNavMesh();
        PlaceEndGoalAtRandomCell();
        PlaceMazeItems();
    }

    private void LoadMazeFromData(SaveMazeData data)
    {
        ClearMaze();

        if (!string.IsNullOrEmpty(data.mazeShape) && System.Enum.TryParse(data.mazeShape, out GameManager.MazeShape parsedShape))
        {
            mazeShape = parsedShape;
        }

        // FIX: Load list of segmented floors perfectly
        if (data.floors != null && data.floors.Count > 0 && floorPrefab != null)
        {
            foreach (var f in data.floors)
            {
                SpawnFromData(floorPrefab, f);
            }
        }
        else if (data.floor != null && floorPrefab != null) // Fallback for old simple square saves
        {
            SpawnFromData(floorPrefab, data.floor);
        }

        foreach (var wall in data.walls) {
            SpawnFromData(wallPrefab, wall);
        }

        foreach (var item in data.collectibles) {
            GameObject prefab = GetPrefabByType(item.type);
            if (prefab != null) SpawnFromData(prefab, item);
        }

        foreach (var npc in data.npcs) {
            GameObject prefab = GetPrefabByType(npc.type);
            if (prefab != null) SpawnFromData(prefab, npc);
        }
        foreach (var goal in data.endGoal) {
            SpawnFromData(endGoalPrefab, goal);
        }

        if (data.playerData != null)
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (player != null)
            {
                // FIX: Disable CharacterController temporarily to force load position explicitly
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                
                player.position = data.playerData.position.ToVector3();
                Vector3 savedRotation = data.playerData.rotation.ToVector3();
                player.rotation = Quaternion.Euler(0, savedRotation.y, 0);

                if (cc != null) cc.enabled = true;
            }
        }
        else 
        {
            // Fallback placement to center room if the save file player data was somehow empty
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.position = GetCenterRoomWorldPosition();
                if (cc != null) cc.enabled = true;
            }
        }

        UpdateMazeInfo();
        BakeNavMesh();
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

    private void InitializeMaze()
    {
        maze = new MazeCell[width, height, depth];
        totalCells = 0;

        if (width <= depth)
            centerRoomSize = Mathf.Max(1, width / 4);
        else
            centerRoomSize = Mathf.Max(1, depth / 4);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);
                maze[x, 0, z] = new MazeCell(pos);
                
                if (mazeShape == GameManager.MazeShape.Circle)
                {
                    if (!IsCellInsideCircleMask(pos))
                    {
                        maze[x, 0, z].visited = true;
                        continue;
                    }
                }
                else if (mazeShape == GameManager.MazeShape.Triangle)
                {
                    if (!IsCellInsideTriangleMask(pos))
                    {
                        maze[x, 0, z].visited = true;
                        continue;
                    }
                }

                totalCells++;
            }
        }
        generationStatus = "Initialized";
    }

    private bool IsCellInsideCircleMask(Vector3Int pos)
    {
        float centerX = (width - 1) / 2f;
        float centerZ = (depth - 1) / 2f;
        float radius = Mathf.Min(width, depth) / 2f;
        float dx = pos.x - centerX;
        float dz = pos.z - centerZ;
        return (dx * dx + dz * dz) <= (radius * radius);
    }

    private bool IsCellInsideTriangleMask(Vector3Int pos)
    {
        float xNorm = (float)pos.x / (width - 1);
        float zNorm = (float)pos.z / (depth - 1);
        if (zNorm > (2f * xNorm)) return false;
        if (zNorm > (2f * (1f - xNorm))) return false;
        return true;
    }

    private Vector3Int GetStartingPosition()
    {
        if (mazeShape == GameManager.MazeShape.Triangle)
            return new Vector3Int(width / 2, 0, depth / 3);
        return new Vector3Int(width / 2, 0, depth / 2);
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

    private void GenerateMazeIterative(Vector3Int start)
    {
        Stack<Vector3Int> stack = new Stack<Vector3Int>();
        stack.Push(start);
        maze[start.x, start.y, start.z].visited = true;

        while (stack.Count > 0)
        {
            Vector3Int current = stack.Pop();
            List<Vector3Int> unvisitedNeighbors = GetUnvisitedNeighbors(current);

            if (unvisitedNeighbors.Count > 0)
            {
                stack.Push(current);
                Vector3Int chosenDirection = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
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
            if (IsInBounds(neighborPos) && !maze[neighborPos.x, neighborPos.y, neighborPos.z].visited)
            {
                neighbors.Add(direction);
            }
        }
        return neighbors;
    }

    private bool IsInCenterRoom(Vector3Int pos)
    {
        Vector3Int center = GetStartingPosition();
        int half = centerRoomSize / 2;
        return pos.x >= center.x - half && pos.x < center.x + half + (centerRoomSize % 2) &&
               pos.z >= center.z - half && pos.z < center.z + half + (centerRoomSize % 2);
    }

    private void CreateCenterRoom()
    {
        Vector3Int center = GetStartingPosition();
        int half = centerRoomSize / 2;

        for (int x = center.x - half; x < center.x + half + (centerRoomSize % 2); x++)
        {
            for (int z = center.z - half; z < center.z + half + (centerRoomSize % 2); z++)
            {
                if (!IsInBounds(new Vector3Int(x, 0, z))) continue;
                if (x > center.x - half) RemoveWalls(new Vector3Int(x, 0, z), new Vector3Int(x - 1, 0, z), Vector3Int.left);
                if (z > center.z - half) RemoveWalls(new Vector3Int(x, 0, z), new Vector3Int(x, 0, z - 1), Vector3Int.back);
            }
        }
    }

    private void VisualizeMaze()
    {
        totalWalls = 0;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int cellPos = new Vector3Int(x, 0, z);
                if (!IsInBounds(cellPos)) continue;

                MazeCell cell = maze[x, 0, z];
                Vector3 worldPos = new Vector3(x * cellSize, 0, z * cellSize);

                if (cell.walls[0]) { CreateWall(worldPos, Vector3.forward, "North Wall", x, z); totalWalls++; }
                if (cell.walls[1]) { CreateWall(worldPos, Vector3.back, "South Wall", x, z); totalWalls++; }
                if (cell.walls[2]) { CreateWall(worldPos, Vector3.right, "East Wall", x, z); totalWalls++; }
                if (cell.walls[3]) { CreateWall(worldPos, Vector3.left, "West Wall", x, z); totalWalls++; }

                if (generateFloors && floorPrefab != null)
                {
                    Vector3 floorPos = new Vector3(worldPos.x - 2f, worldPos.y, worldPos.z - 2f);
                    GameObject floorTile = Instantiate(floorPrefab, floorPos, Quaternion.identity);
                    floorTile.name = $"Floor_{x}_{z}";
                    floorTile.transform.localScale = new Vector3(cellSize, 1f, cellSize);
                    floorTile.transform.parent = transform;
                    
                    if (floorMaterial != null)
                    {
                        Renderer r = floorTile.GetComponentInChildren<Renderer>();
                        if (r != null) r.material = floorMaterial;
                    }
                    mazeObjects.Add(floorTile);
                }
            }
        }
        generationStatus = "Visualized";
    }

    private void CreateWall(Vector3 position, Vector3 direction, string name, int cellX, int cellZ)
    {
        GameObject wall = wallPrefab != null ? Instantiate(wallPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position + direction * (cellSize / 2f);
        wall.transform.localScale = GetWallScale(direction);
        wall.transform.parent = transform;

        Material regionMaterial = GetWallMaterialForCell(cellX, cellZ);
        if (regionMaterial != null)
        {
            Renderer renderer = wall.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material = regionMaterial; 
        }
        mazeObjects.Add(wall);
    }

    private Vector3 GetWallScale(Vector3 direction)
    {
        if (direction == Vector3.forward || direction == Vector3.back) 
            return new Vector3(cellSize, 1.5f, 0.5f);
        return new Vector3(0.5f, 1.5f, cellSize); 
    }

    private void RemoveWalls(Vector3Int current, Vector3Int next, Vector3Int direction)
    {
        MazeCell currentCell = maze[current.x, current.y, current.z];
        MazeCell nextCell = maze[next.x, next.y, next.z];

        if (direction == Vector3Int.forward) { currentCell.walls[0] = false; nextCell.walls[1] = false; }
        else if (direction == Vector3Int.back) { currentCell.walls[1] = false; nextCell.walls[0] = false; }
        else if (direction == Vector3Int.right) { currentCell.walls[2] = false; nextCell.walls[3] = false; }
        else if (direction == Vector3Int.left) { currentCell.walls[3] = false; nextCell.walls[2] = false; }
    }

    private bool IsInBounds(Vector3Int pos)
    {
        bool inGrid = pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height && pos.z >= 0 && pos.z < depth;
        if (!inGrid) return false;

        if (mazeShape == GameManager.MazeShape.Circle) return IsCellInsideCircleMask(pos);
        if (mazeShape == GameManager.MazeShape.Triangle) return IsCellInsideTriangleMask(pos);
        return true;
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
        if (endGoalPrefab == null) return;
        List<Vector3Int> validCells = new List<Vector3Int>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);
                if (!IsInBounds(pos)) continue;
                if (avoidCenterRoom && generateCenterRoom && IsInCenterRoom(pos)) continue;
                validCells.Add(pos);
            }
        }

        if (validCells.Count == 0) return;
        Vector3Int chosenCell = validCells[Random.Range(0, validCells.Count)];
        SpawnEndGoal(chosenCell);
    }

    private void SpawnEndGoal(Vector3Int cellPos)
    {
        Vector3 worldPos = new Vector3(cellPos.x * cellSize, cellPos.y * cellSize, cellPos.z * cellSize);
        GameObject goal = Instantiate(endGoalPrefab, worldPos, Quaternion.identity);
        goal.name = "End Goal";
        goal.transform.parent = transform;
        mazeObjects.Add(goal);
    }

    public Vector3 GetRandomCellWorldPosition(bool avoidCenterRoom = true)
    {
        List<Vector3Int> validCells = new List<Vector3Int>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);

                if (!IsInBounds(pos)) continue;
                if (avoidCenterRoom && generateCenterRoom && IsInCenterRoom(pos)) continue;

                validCells.Add(pos);
            }
        }

        if (validCells.Count == 0)
            return Vector3.zero;

        Vector3Int chosen = validCells[Random.Range(0, validCells.Count)];
        return new Vector3(chosen.x * cellSize, 0f, chosen.z * cellSize);
    }

    private void PlaceMazeItems()
    {
        List<Vector3Int> validCells = new List<Vector3Int>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);
                if (!IsInBounds(pos)) continue;
                if (generateCenterRoom && IsInCenterRoom(pos)) continue;
                validCells.Add(pos);
            }
        }

        for (int i = 0; i < validCells.Count; i++)
        {
            int r = Random.Range(i, validCells.Count);
            (validCells[i], validCells[r]) = (validCells[r], validCells[i]);
        }

        int totalItems = (width + depth) / 2;
        int autoPerType = totalItems / 6;

        MazeItem[] items =
        {
            new MazeItem { prefab = dogPrefab,    count = dogCount    > 0 ? dogCount    : autoPerType },
            new MazeItem { prefab = bonePrefab,   count = boneCount   > 0 ? boneCount   : autoPerType },
            new MazeItem { prefab = shieldPrefab, count = shieldCount > 0 ? shieldCount : autoPerType },
            new MazeItem { prefab = starPrefab,   count = starCount   > 0 ? starCount   : autoPerType },
            new MazeItem { prefab = teleportPrefab, count = teleportCount > 0 ? teleportCount : autoPerType },
            new MazeItem { prefab = slowPrefab,   count = slowCount   > 0 ? slowCount   : autoPerType }
        };

        int index = 0;
        foreach (var item in items)
        {
            if (item.prefab == null) continue;
            for (int i = 0; i < item.count && index < validCells.Count; i++)
            {
                SpawnItem(item.prefab, validCells[index++]);
            }
        }
    }

    private void SpawnItem(GameObject prefab, Vector3Int cellPos)
    {
        Vector3 worldPos;
        if (prefab == dogPrefab) worldPos = new Vector3(cellPos.x * cellSize, 0, cellPos.z * cellSize);
        else if (prefab == teleportPrefab) worldPos = new Vector3(cellPos.x * cellSize, cellPos.y + 0.2f, cellPos.z * cellSize);
        else worldPos = new Vector3(cellPos.x * cellSize, 1, cellPos.z * cellSize);            

        Quaternion rotation = Quaternion.identity;
        if (prefab == starPrefab) rotation = Quaternion.Euler(-90f, 0f, 0f);

        GameObject item = Instantiate(prefab, worldPos, rotation);
        item.transform.parent = transform;
        mazeObjects.Add(item);
    }

    protected virtual void BakeNavMesh()
    {
        if (navmeshsurface == null) return;
        navmeshsurface.BuildNavMesh();
    }

    private void AddExtraConnections()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);
                if (!IsInBounds(pos)) continue;

                MazeCell cell = maze[x, 0, z];
                foreach (Vector3Int dir in directions)
                {
                    if (Random.value > extraConnectionChance) continue;
                    Vector3Int neighborPos = pos + dir;
                    if (!IsInBounds(neighborPos)) continue;

                    int wallIndex = DirectionToWallIndex(dir);
                    if (cell.walls[wallIndex]) RemoveWalls(pos, neighborPos, dir);
                }
            }
        }
    }

    private int DirectionToWallIndex(Vector3Int dir)
    {
        if (dir == Vector3Int.forward) return 0; 
        if (dir == Vector3Int.back)    return 1; 
        if (dir == Vector3Int.right)   return 2; 
        return 3; 
    }

    private Vector3 GetCenterRoomWorldPosition()
    {
        Vector3Int center = GetStartingPosition();
        return new Vector3(center.x * cellSize, 0f, center.z * cellSize);
    }

    private void UpdateMazeInfo()
    {
        generationStatus = $"Generated ({mazeShape}) - {width}x{height}x{depth}";
    }

    private GameObject GetPrefabByType(string type)
    {
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
        instance.transform.localScale = data.scale.ToVector3();
        instance.transform.parent = transform;
        
        if (data.materialIndex >= 0 && data.materialIndex < wallMaterials.Length)
        {
            Renderer renderer = instance.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material = wallMaterials[data.materialIndex];
        }
        mazeObjects.Add(instance);
    }

    private Material GetWallMaterialForCell(int x, int z)
    {
        if (wallMaterials == null || wallMaterials.Length == 0) return null;
        int regionX = x / wallRegionSize;
        int regionZ = z / wallRegionSize;
        Vector2Int regionKey = new Vector2Int(regionX, regionZ);

        if (!wallRegionMaterials.TryGetValue(regionKey, out Material mat))
        {
            mat = wallMaterials[Random.Range(0, wallMaterials.Length)];
            wallRegionMaterials.Add(regionKey, mat);
        }
        return mat;
    }

    public SaveMazeData GetMazeSaveData()
    {
        SaveMazeData data = new SaveMazeData();
        data.sceneName = SceneManager.GetActiveScene().name; 
        data.width = this.width;
        data.depth = this.depth;
        data.mazeShape = this.mazeShape.ToString();

        CaptureObjects(data.walls, "Wall");
        CaptureObjects(data.npcs, "NPC");
        CaptureObjects(data.collectibles, "Collectibles");
        CaptureObjects(data.endGoal, "MazeGoal");

        // FIX: Extract all individual segmented floor tiles correctly and save them to the list
        foreach (GameObject obj in mazeObjects)
        {
            if (obj != null && obj.name.StartsWith("Floor_"))
            {
                data.floors.Add(new ObjectData
                {
                    type = "Floor",
                    position = new SerializableVector3(obj.transform.position),
                    rotation = new SerializableVector3(obj.transform.eulerAngles),
                    scale = new SerializableVector3(obj.transform.localScale)
                });
            }
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
            if (rend != null && wallMaterials != null)
            {
                for (int i = 0; i < wallMaterials.Length; i++)
                {
                    if (rend.sharedMaterial == wallMaterials[i]) { matIdx = i; break; }
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
}