using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a random maze using DFS (recursive backtracking).
/// Uses floor, wall, and pillar prefabs sized relative to cellSize.
/// </summary>
public class RandomMazeGenerator : MonoBehaviour
{
    [Header("Maze Size")]
    [SerializeField] private int rows = 10;
    [SerializeField] private int columns = 10;
    [SerializeField] private float cellSize = 5f;

    [Header("Prefabs")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pillarPrefab;

    [Header("Settings")]
    [SerializeField] private float wallThickness = 1f;
    [SerializeField] private bool generateOnStart = true;

    private MazeCell[,] cells;

    private void Start()
    {
        if (generateOnStart)
            GenerateMaze();
    }

    #region Maze Generation

    private void GenerateMaze()
    {
        InitializeCells();
        GeneratePathsDFS(0, 0);
        InstantiateMaze();
    }

    private void InitializeCells()
    {
        cells = new MazeCell[rows, columns];

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                cells[x, y] = new MazeCell
                {
                    visited = false,
                    wallTop = true,
                    wallBottom = true,
                    wallLeft = true,
                    wallRight = true
                };
            }
        }
    }

    private void GeneratePathsDFS(int startX, int startY)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(startX, startY);
        cells[startX, startY].visited = true;

        do
        {
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current);

            if (neighbors.Count > 0)
            {
                Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                RemoveWallBetween(current, next);

                stack.Push(current);
                current = next;
                cells[current.x, current.y].visited = true;
            }
            else if (stack.Count > 0)
            {
                current = stack.Pop();
            }
        }
        while (stack.Count > 0);
    }

    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        if (cell.x > 0 && !cells[cell.x - 1, cell.y].visited)
            neighbors.Add(new Vector2Int(cell.x - 1, cell.y));
        if (cell.x < rows - 1 && !cells[cell.x + 1, cell.y].visited)
            neighbors.Add(new Vector2Int(cell.x + 1, cell.y));
        if (cell.y > 0 && !cells[cell.x, cell.y - 1].visited)
            neighbors.Add(new Vector2Int(cell.x, cell.y - 1));
        if (cell.y < columns - 1 && !cells[cell.x, cell.y + 1].visited)
            neighbors.Add(new Vector2Int(cell.x, cell.y + 1));

        return neighbors;
    }

    private void RemoveWallBetween(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x)
        {
            if (a.y < b.y)
            {
                cells[a.x, a.y].wallRight = false;
                cells[b.x, b.y].wallLeft = false;
            }
            else
            {
                cells[a.x, a.y].wallLeft = false;
                cells[b.x, b.y].wallRight = false;
            }
        }
        else
        {
            if (a.x < b.x)
            {
                cells[a.x, a.y].wallBottom = false;
                cells[b.x, b.y].wallTop = false;
            }
            else
            {
                cells[a.x, a.y].wallTop = false;
                cells[b.x, b.y].wallBottom = false;
            }
        }
    }

    #endregion

    #region Instantiation

    private void InstantiateMaze()
    {
        SpawnFloors();
        SpawnPillars();
        SpawnWalls();
    }

    private void SpawnFloors()
    {
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                Vector3 pos = CellToWorld(x, y);
                GameObject floor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                floor.transform.localScale = new Vector3(cellSize, 1, cellSize);
                floor.tag = "LevelObject";
            }
        }
    }

    private void SpawnPillars()
    {
        for (int x = 0; x <= rows; x++)
        {
            for (int y = 0; y <= columns; y++)
            {
                Vector3 pos = new Vector3(
                    y * cellSize - cellSize / 2,
                    0,
                    (rows - x) * cellSize - cellSize / 2
                );

                GameObject pillar = Instantiate(pillarPrefab, pos, Quaternion.identity, transform);
                pillar.transform.localScale = Vector3.one;
                pillar.tag = "LevelObject";
            }
        }
    }

    private void SpawnWalls()
    {
        float wallLength = cellSize - wallThickness;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < columns; y++)
            {
                MazeCell cell = cells[x, y];
                Vector3 cellPos = CellToWorld(x, y);

                if (cell.wallRight)
                {
                    SpawnWall(cellPos + new Vector3(cellSize / 2, 0, 0), 90, wallLength);
                }

                if (cell.wallLeft)
                {
                    SpawnWall(cellPos + new Vector3(-cellSize / 2, 0, 0), 90, wallLength);
                }

                if (cell.wallTop)
                {
                    SpawnWall(cellPos + new Vector3(0, 0, -cellSize / 2), 0, wallLength);
                }

                if (cell.wallBottom)
                {
                    SpawnWall(cellPos + new Vector3(0, 0, cellSize / 2), 0, wallLength);
                }
            }
        }
    }

    private void SpawnWall(Vector3 position, float yRotation, float length)
    {
        GameObject wall = Instantiate(
            wallPrefab,
            position,
            Quaternion.Euler(0, yRotation, 0),
            transform
        );

        wall.transform.localScale = new Vector3(length, 1, wallThickness);
        wall.tag = "LevelObject";
    }

    #endregion

    #region Utilities

    private Vector3 CellToWorld(int x, int y)
    {
        float posX = y * cellSize;
        float posZ = (rows - 1 - x) * cellSize;
        return new Vector3(posX, 0, posZ);
    }

    #endregion

    #region Data Structure

    private class MazeCell
    {
        public bool visited;
        public bool wallTop;
        public bool wallBottom;
        public bool wallLeft;
        public bool wallRight;
    }

    #endregion
}
