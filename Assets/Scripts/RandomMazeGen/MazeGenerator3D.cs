using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MazeGenerator3D : MonoBehaviour
{
    #region Inspector Fields

    [Header("Maze Dimensions")]
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private int depth = 10;

    [Header("Cell Size")]
    [SerializeField] private float cellSize = 2f;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject endGoalPrefab;
    [SerializeField] private GameObject dogPrefab;
    [SerializeField] private GameObject bonePrefab;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private GameObject starPrefab;

    [Header("NPCs & Items Count")]
    [SerializeField] private int dogCount = 0;
    [SerializeField] private int boneCount = 0;
    [SerializeField] private int shieldCount = 0;
    [SerializeField] private int starCount = 0;

    [Header("Materials")]
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material floorMaterial;

    [Header("Generation Settings")]
    [SerializeField] private bool useIterativeGeneration = false;
    [SerializeField] private bool generateFloors = true;

    [Header("Maze Info")]
    [SerializeField, ReadOnly] private string generationStatus = "Not Generated";
    [SerializeField, ReadOnly] private int totalCells = 0;
    [SerializeField, ReadOnly] private int totalWalls = 0;

    [Header("Center Room Settings")]
    [SerializeField] private bool generateCenterRoom = true;
    [SerializeField] private int centerRoomSize = 2; // must be even or odd

    #endregion

    #region Private Fields

    private MazeCell[,,] maze;
    private List<GameObject> mazeObjects = new List<GameObject>();

    // Directions: Up, Down, North, South, East, West
    private Vector3Int[] directions =
    {
        Vector3Int.up,
        Vector3Int.down,
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

    public int Height
    {
        get => height;
        set => height = Mathf.Max(1, value);
    }

    public int Depth
    {
        get => depth;
        set => depth = Mathf.Max(1, value);
    }

    #endregion

    #region Nested Types

    [System.Serializable]
    public class MazeCell
    {
        // 0: Up, 1: Down, 2: North, 3: South, 4: East, 5: West
        public bool[] walls = new bool[6];
        public bool visited = false;
        public Vector3Int position;
        public int distance = -1;

        public MazeCell(Vector3Int pos)
        {
            position = pos;

            // Start with all walls present
            for (int i = 0; i < 6; i++)
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
        ClearMaze();
        InitializeMaze();

        if (useIterativeGeneration)
            GenerateMazeIterative();
        else
            GenerateMazeRecursive(Vector3Int.zero);

        if (generateCenterRoom)
            CreateCenterRoom();

        PlaceEndGoalAtFarthestCell();
        PlaceMazeItems();

        VisualizeMaze();
        UpdateMazeInfo();

        Debug.Log($"Maze generated! Dimensions: {width}x{height}x{depth}, Total cells: {totalCells}");
    }

    public void ClearMaze()
    {
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
            height * cellSize * 0.5f,
            depth * cellSize * 0.5f
        );

        Vector3 size = new Vector3(
            width * cellSize,
            height * cellSize,
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

    private void InitializeMaze()
    {
        maze = new MazeCell[width, height, depth];
        totalCells = width * height * depth;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    maze[x, y, z] = new MazeCell(new Vector3Int(x, y, z));
                }
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
        Vector3Int center = new Vector3Int(
            width / 2,
            height / 2,
            depth / 2
        );

        int half = centerRoomSize / 2;

        return pos.x >= center.x - half && pos.x < center.x + half + centerRoomSize % 2 &&
            pos.y >= center.y - half && pos.y < center.y + half + centerRoomSize % 2 &&
            pos.z >= center.z - half && pos.z < center.z + half + centerRoomSize % 2;
    }

    private void CreateCenterRoom()
    {
        List<MazeCell> roomCells = new List<MazeCell>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);

                    if (!IsInCenterRoom(pos))
                        continue;

                    MazeCell cell = maze[x, y, z];
                    roomCells.Add(cell);

                    for (int i = 0; i < 6; i++)
                        cell.walls[i] = false;
                }
            }
        }

        // Create a doorway from one random room cell
        MazeCell doorCell = roomCells[Random.Range(0, roomCells.Count)];

        foreach (Vector3Int dir in directions)
        {
            Vector3Int neighborPos = doorCell.position + dir;

            if (IsInBounds(neighborPos) && !IsInCenterRoom(neighborPos))
            {
                RemoveWalls(doorCell.position, neighborPos, dir);
                break;
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

    #endregion

    #region Maze Logic Helpers

    private void RemoveWalls(Vector3Int current, Vector3Int next, Vector3Int direction)
    {
        MazeCell currentCell = maze[current.x, current.y, current.z];
        MazeCell nextCell = maze[next.x, next.y, next.z];

        if (direction == Vector3Int.up)
        {
            currentCell.walls[0] = false;
            nextCell.walls[1] = false;
        }
        else if (direction == Vector3Int.down)
        {
            currentCell.walls[1] = false;
            nextCell.walls[0] = false;
        }
        else if (direction == Vector3Int.forward)
        {
            currentCell.walls[2] = false;
            nextCell.walls[3] = false;
        }
        else if (direction == Vector3Int.back)
        {
            currentCell.walls[3] = false;
            nextCell.walls[2] = false;
        }
        else if (direction == Vector3Int.right)
        {
            currentCell.walls[4] = false;
            nextCell.walls[5] = false;
        }
        else if (direction == Vector3Int.left)
        {
            currentCell.walls[5] = false;
            nextCell.walls[4] = false;
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

    private void PlaceEndGoalAtFarthestCell()
    {
        if (endGoalPrefab == null)
            return;

        Vector3Int start = Vector3Int.zero;

        Queue<MazeCell> queue = new Queue<MazeCell>();
        MazeCell startCell = maze[start.x, start.y, start.z];

        startCell.distance = 0;
        queue.Enqueue(startCell);

        MazeCell farthestCell = startCell;

        while (queue.Count > 0)
        {
            MazeCell current = queue.Dequeue();

            if (current.distance > farthestCell.distance)
                farthestCell = current;

            for (int i = 0; i < directions.Length; i++)
            {
                // Wall exists → cannot move
                if (current.walls[i])
                    continue;

                Vector3Int nextPos = current.position + directions[i];

                if (!IsInBounds(nextPos))
                    continue;

                MazeCell neighbor = maze[nextPos.x, nextPos.y, nextPos.z];

                if (neighbor.distance != -1)
                    continue;

                neighbor.distance = current.distance + 1;
                queue.Enqueue(neighbor);
            }
        }

        SpawnEndGoal(farthestCell.position);
    }

    private void PlaceMazeItems()
    {
        List<Vector3Int> validCells = new List<Vector3Int>();

        // Collect valid spawn cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);

                    // Avoid center room if you want
                    if (generateCenterRoom && IsInCenterRoom(pos))
                        continue;

                    validCells.Add(pos);
                }
            }
        }

        // Shuffle cells
        for (int i = 0; i < validCells.Count; i++)
        {
            int r = Random.Range(i, validCells.Count);
            (validCells[i], validCells[r]) = (validCells[r], validCells[i]);
        }

        int totalItems = width;          // scaling rule
        int perType = totalItems / 4;

        var items = new MazeItem[]
        {
            new MazeItem { prefab = dogPrefab,    count = perType },
            new MazeItem { prefab = bonePrefab,   count = perType },
            new MazeItem { prefab = shieldPrefab, count = perType },
            new MazeItem { prefab = starPrefab,   count = perType }
        };

        int cellIndex = 0;

        foreach (var item in items)
        {
            if (item.prefab == null)
                continue;

            for (int i = 0; i < item.count && cellIndex < validCells.Count; i++)
            {
                Vector3Int cell = validCells[cellIndex++];
                SpawnItem(item.prefab, cell);
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
                cellPos.y * cellSize,
                cellPos.z * cellSize
            );
        }
        else
        {
            worldPos = new Vector3(
                cellPos.x * cellSize,
                cellPos.y * cellSize + 1,
                cellPos.z * cellSize
            );            
        }

        Quaternion rotation = Quaternion.identity;

        // Only rotate stars
        if (prefab == starPrefab)
        {
            rotation = Quaternion.Euler(-90f, 0f, 0f);
        }

        GameObject item = Instantiate(prefab, worldPos, rotation);
        item.transform.parent = transform;
        mazeObjects.Add(item);
    }

    #endregion

    #region Visualization

    private void VisualizeMaze()
    {
        totalWalls = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    MazeCell cell = maze[x, y, z];
                    Vector3 worldPos = new Vector3(
                        x * cellSize,
                        y * cellSize,
                        z * cellSize
                    );

                    // if (cell.walls[0]) { CreateWall(worldPos, Vector3.up, "Up Wall"); totalWalls++; }
                    // if (cell.walls[1]) { CreateWall(worldPos, Vector3.down, "Down Wall"); totalWalls++; }
                    if (cell.walls[2]) { CreateWall(worldPos, Vector3.forward, "North Wall"); totalWalls++; }
                    if (cell.walls[3]) { CreateWall(worldPos, Vector3.back, "South Wall"); totalWalls++; }
                    if (cell.walls[4]) { CreateWall(worldPos, Vector3.right, "East Wall"); totalWalls++; }
                    if (cell.walls[5]) { CreateWall(worldPos, Vector3.left, "West Wall"); totalWalls++; }
                }
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

        // Rotate quad to be horizontal
        floor.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        floor.transform.parent = transform;
        mazeObjects.Add(floor);
    }

    private void CreateWall(Vector3 position, Vector3 direction, string name)
    {
        GameObject wall = wallPrefab != null
            ? Instantiate(wallPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        wall.name = name;
        wall.transform.position = position + direction * (cellSize / 2f);
        wall.transform.localScale = GetWallScale(direction);
        wall.transform.parent = transform;

        if (wallMaterial != null)
        {
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = wallMaterial;
        }

        mazeObjects.Add(wall);
    }

    private Vector3 GetWallScale(Vector3 direction)
    {
        if (direction == Vector3.up || direction == Vector3.down)
            return new Vector3(cellSize, 0.1f, cellSize);

        if (direction == Vector3.forward || direction == Vector3.back)
            return new Vector3(cellSize, 1.5f, 0.5f);

        return new Vector3(0.5f, 1.5f, cellSize);
    }

    private void UpdateMazeInfo()
    {
        generationStatus = $"Generated - {width}x{height}x{depth}";
    }

    #endregion
}